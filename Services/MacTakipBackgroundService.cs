using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;

namespace HakemYorumlari.Services
{
    public class MacTakipBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MacTakipBackgroundService> _logger;
        private readonly IBackgroundJobService _backgroundJobService;

        public MacTakipBackgroundService(IServiceProvider serviceProvider, 
            ILogger<MacTakipBackgroundService> logger,
            IBackgroundJobService backgroundJobService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _backgroundJobService = backgroundJobService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MacTakipBackgroundService başlatıldı - BackgroundJobService entegrasyonu aktif");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MacDurumlariniGuncelle(stoppingToken);
                    await YorumToplanacakMaclariKontrolEt(); // Artık job queue kullanıyor
                    await HaftalikFikstürKontrolEt();
                    
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // 10 dakikada bir kontrol
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("MacTakipBackgroundService durduruldu");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background service hatası");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
            }
        }

        private async Task MacDurumlariniGuncelle(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var skorServisi = scope.ServiceProvider.GetRequiredService<SkorCekmeServisi>();

                var simdi = DateTime.Now;
                
                // Maç saati geldiğinde durumu "Oynaniyor" yap
                var baslayacakMaclar = await context.Maclar
                    .Where(m => m.Durum == MacDurumu.Bekliyor &&
                               m.MacTarihi <= simdi &&
                               m.MacTarihi.AddMinutes(120) > simdi) // 2 saat içinde bitmemiş
                    .ToListAsync(stoppingToken);

                foreach (var mac in baslayacakMaclar)
                {
                    mac.Durum = MacDurumu.Oynaniyor;
                    _logger.LogInformation($"Maç durumu güncellendi - Oynaniyor: {mac.EvSahibi} vs {mac.Deplasman}");
                }

                // Maç bitiminden sonra durumu "Bitti" yap (maç + 2 saat)
                var bitecekMaclar = await context.Maclar
                    .Where(m => m.Durum == MacDurumu.Oynaniyor &&
                               m.MacTarihi.AddMinutes(120) <= simdi)
                    .ToListAsync(stoppingToken);

                foreach (var mac in bitecekMaclar)
                {
                    mac.Durum = MacDurumu.Bitti;
                    
                    if (string.IsNullOrEmpty(mac.Skor) || mac.Skor == "-")
                    {
                        var gercekSkor = await skorServisi.MacSkoruCek(mac.EvSahibi, mac.Deplasman, mac.MacTarihi);
                        mac.Skor = string.IsNullOrEmpty(gercekSkor) ? "-" : gercekSkor;
                        _logger.LogInformation("Skor çekildi: {EvSahibi} vs {Deplasman} -> {Skor}", mac.EvSahibi, mac.Deplasman, mac.Skor);
                    }

                    _logger.LogInformation("Maç durumu güncellendi -> Bitti: {EvSahibi} vs {Deplasman}", mac.EvSahibi, mac.Deplasman);
                }

                if (baslayacakMaclar.Any() || bitecekMaclar.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Toplam {Count} maç durumu veritabanına kaydedildi.", baslayacakMaclar.Count + bitecekMaclar.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç durumları güncellenirken hata oluştu");
            }
        }

        private async Task YorumToplanacakMaclariKontrolEt()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var simdi = DateTime.Now;
                
                // Yorum toplanacak haftaları kontrol et
                var yorumToplanacakHaftalar = await context.Maclar
                    .Where(m => m.OtomatikYorumToplamaAktif && 
                               !m.YorumlarToplandi &&
                               m.Durum == MacDurumu.Bitti &&
                               m.MacTarihi.AddMinutes(150) <= simdi // Maç bitiminden 2.5 saat sonra
                               )  
                    .GroupBy(m => m.Hafta)
                    .Select(g => new { Hafta = g.Key, MacSayisi = g.Count() })
                    .Where(g => g.MacSayisi >= 2) // En az 2 maç olan haftalar
                    .OrderBy(g => g.Hafta)
                    .ToListAsync();

                foreach (var hafta in yorumToplanacakHaftalar)
                {
                    // Bu hafta için zaten aktif job var mı kontrol et
                    var aktifJoblar = _backgroundJobService.GetAllActiveJobs();
                    var haftaJobVarMi = aktifJoblar.Any(job => 
                        job.Status == "Running" || job.Status == "Queued");

                    if (!haftaJobVarMi)
                    {
                        _logger.LogInformation($"BackgroundJobService ile hafta {hafta.Hafta} yorum toplama job'ı başlatılıyor ({hafta.MacSayisi} maç)");
                        var jobId = _backgroundJobService.EnqueueHaftaYorumToplama(hafta.Hafta);
                        _logger.LogInformation($"Job başlatıldı - ID: {jobId}, Hafta: {hafta.Hafta}");
                    }
                    else
                    {
                        _logger.LogDebug($"Hafta {hafta.Hafta} için zaten aktif job mevcut, yeni job başlatılmadı");
                    }
                }

                if (yorumToplanacakHaftalar.Any())
                {
                    _logger.LogInformation($"{yorumToplanacakHaftalar.Count} hafta için job kontrolü tamamlandı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum toplanacak maçlar kontrol edilirken hata oluştu");
            }
        }

        private async Task HaftalikFikstürKontrolEt()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var simdi = DateTime.Now;
                var buHafta = GetCurrentWeek(simdi);
                
                // Bu haftanın maçlarını kontrol et
                var buHaftaninMaclari = await context.Maclar
                    .Where(m => m.Hafta == buHafta && 
                               m.Liga == "Süper Lig" &&
                               m.MacTarihi.Date >= simdi.Date.AddDays(-7) &&
                               m.MacTarihi.Date <= simdi.Date.AddDays(7))
                    .ToListAsync();

                if (buHaftaninMaclari.Any())
                {
                    _logger.LogInformation($"Bu hafta ({buHafta}. hafta) {buHaftaninMaclari.Count} maç bulundu");
                    
                    var bitenMaclar = buHaftaninMaclari.Where(m => m.Durum == MacDurumu.Bitti).Count();
                    var oynaniyorMaclar = buHaftaninMaclari.Where(m => m.Durum == MacDurumu.Oynaniyor).Count();
                    var bekleyenMaclar = buHaftaninMaclari.Where(m => m.Durum == MacDurumu.Bekliyor).Count();
                    
                    _logger.LogInformation($"Hafta {buHafta} durum: {bitenMaclar} bitti, {oynaniyorMaclar} oynaniyor, {bekleyenMaclar} bekliyor");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftalık fikstür kontrol edilirken hata oluştu");
            }
        }

        private int GetCurrentWeek(DateTime tarih)
        {
            // TFF'deki hafta sistemi:
            // 08.08.2025 = 1. hafta
            // 15.08.2025 = 2. hafta
            // 22.08.2025 = 3. hafta
            // vs.
            
            // Süper Lig sezon başlangıcı (8 Ağustos 2025)
            var sezonBaslangic = new DateTime(2025, 8, 8);
            
            if (tarih < sezonBaslangic)
                return 1;
                
            var gecenGunler = (tarih - sezonBaslangic).Days;
            var hafta = (int)Math.Ceiling(gecenGunler / 7.0);
            
            return Math.Min(Math.Max(hafta, 1), 38); // 1-38 hafta arası
        }
    }
}
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;

namespace HakemYorumlari.Services
{
    public class MacTakipBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MacTakipBackgroundService> _logger;

        public MacTakipBackgroundService(IServiceProvider serviceProvider, 
            ILogger<MacTakipBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MacTakipBackgroundService başlatıldı");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MacDurumlariniGuncelle();
                    await YorumToplanacakMaclariKontrolEt();
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

        private async Task MacDurumlariniGuncelle()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var simdi = DateTime.Now;
                
                // Maç saati geldiğinde durumu "Oynaniyor" yap
                var baslayacakMaclar = await context.Maclar
                    .Where(m => m.Durum == MacDurumu.Bekliyor &&
                               m.MacTarihi <= simdi &&
                               m.MacTarihi.AddMinutes(120) > simdi) // 2 saat içinde bitmemiş
                    .ToListAsync();

                foreach (var mac in baslayacakMaclar)
                {
                    mac.Durum = MacDurumu.Oynaniyor;
                    _logger.LogInformation($"Maç durumu güncellendi - Oynaniyor: {mac.EvSahibi} vs {mac.Deplasman}");
                }

                // Maç bitiminden sonra durumu "Bitti" yap (maç + 2 saat)
                var bitecekMaclar = await context.Maclar
                    .Where(m => m.Durum == MacDurumu.Oynaniyor &&
                               m.MacTarihi.AddMinutes(120) <= simdi)
                    .ToListAsync();

                foreach (var mac in bitecekMaclar)
                {
                    mac.Durum = MacDurumu.Bitti;
                    
                    // Gerçek skor çekmeye çalış
                    if (string.IsNullOrEmpty(mac.Skor) || mac.Skor == "-")
                    {
                        try
                        {
                            var skorServisi = scope.ServiceProvider.GetRequiredService<SkorCekmeServisi>();
                            
                            // TFF'den öncelikli olarak skor çekmeye çalış
                            var gercekSkor = await skorServisi.MacSkoruCek(mac.EvSahibi, mac.Deplasman, mac.MacTarihi);
                            
                            if (!string.IsNullOrEmpty(gercekSkor))
                            {
                                mac.Skor = gercekSkor;
                                _logger.LogInformation($"Gerçek skor bulundu: {mac.EvSahibi} vs {mac.Deplasman} - {gercekSkor}");
                            }
                            else
                            {
                                _logger.LogWarning($"Gerçek skor bulunamadı: {mac.EvSahibi} vs {mac.Deplasman} - Skor boş bırakıldı");
                                // Skor bulunamadıysa "-" olarak işaretle
                                mac.Skor = "-";
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Skor çekme hatası: {mac.EvSahibi} vs {mac.Deplasman} - Skor boş bırakıldı");
                            mac.Skor = "-";
                        }
                    }
                    
                    _logger.LogInformation($"Maç durumu güncellendi - Bitti: {mac.EvSahibi} vs {mac.Deplasman} ({mac.Skor ?? "Skor yok"})");
                }

                if (baslayacakMaclar.Any() || bitecekMaclar.Any())
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Toplam {baslayacakMaclar.Count + bitecekMaclar.Count} maç durumu güncellendi");
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
                var yorumServisi = scope.ServiceProvider.GetRequiredService<HakemYorumuToplamaServisi>();

                var simdi = DateTime.Now;
                
                // Sadece biten ve yorum toplanmamış maçları al
                var yorumToplanacakMaclar = await context.Maclar
                    .Where(m => m.OtomatikYorumToplamaAktif && 
                               !m.YorumlarToplandi &&
                               m.Durum == MacDurumu.Bitti &&
                               m.MacTarihi.AddMinutes(150) <= simdi && // Maç bitiminden 30 dakika sonra
                               m.MacTarihi >= simdi.AddDays(-2)) // Son 2 gün içindeki maçlar
                    .OrderBy(m => m.MacTarihi)
                    .Take(5) // Aynı anda en fazla 5 maç işle
                    .ToListAsync();

                if (yorumToplanacakMaclar.Any())
                {
                    _logger.LogInformation($"{yorumToplanacakMaclar.Count} maç için otomatik yorum toplama başlatılıyor");
                }

                foreach (var mac in yorumToplanacakMaclar)
                {
                    _logger.LogInformation($"Otomatik yorum toplama başlatılıyor: {mac.EvSahibi} vs {mac.Deplasman} (Hafta {mac.Hafta})");
                    
                    try
                    {
                        var basarili = await yorumServisi.MacIcinYorumTopla(mac.Id);
                        
                        if (basarili)
                        {
                            _logger.LogInformation($"Yorum toplama başarılı: {mac.EvSahibi} vs {mac.Deplasman}");
                        }
                        else
                        {
                            _logger.LogWarning($"Yorum toplama başarısız: {mac.EvSahibi} vs {mac.Deplasman}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Yorum toplama hatası: {mac.EvSahibi} vs {mac.Deplasman}");
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(45)); // Rate limiting
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
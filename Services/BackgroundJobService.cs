using System.Collections.Concurrent;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;

namespace HakemYorumlari.Services
{
    public class BackgroundJobService : BackgroundService, IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly ConcurrentQueue<BackgroundJob> _jobQueue = new();
        private readonly Dictionary<string, JobStatus> _jobStatuses = new();
        private DateTime _lastAutoJobCheck = DateTime.MinValue;

        public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public string EnqueueHaftaYorumToplama(int hafta)
        {
            var jobId = Guid.NewGuid().ToString();
            var job = new BackgroundJob
            {
                Id = jobId,
                Type = JobType.HaftaYorumToplama,
                Parameters = new { hafta },
                CreatedAt = DateTime.Now
            };

            _jobQueue.Enqueue(job);
            _jobStatuses[jobId] = new JobStatus { Status = "Queued", Message = "İşlem kuyruğa eklendi" };
            
            _logger.LogInformation($"Hafta {hafta} yorum toplama işi kuyruğa eklendi: {jobId}");
            return jobId;
        }

        public JobStatus? GetJobStatus(string jobId)
        {
            return _jobStatuses.TryGetValue(jobId, out var status) ? status : null;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundJobService başlatıldı - Otomatik job kontrolü aktif");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Otomatik job kontrolü (her 5 dakikada bir)
                    if (DateTime.Now - _lastAutoJobCheck > TimeSpan.FromMinutes(5))
                    {
                        await CheckAndEnqueueAutoJobs();
                        _lastAutoJobCheck = DateTime.Now;
                    }

                    // Kuyruktaki job'ları işle
                    if (_jobQueue.TryDequeue(out var job))
                    {
                        await ProcessJob(job, stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background job işleme hatası");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private async Task CheckAndEnqueueAutoJobs()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var simdi = DateTime.Now;
                
                // Otomatik yorum toplanacak maçları kontrol et
                var yorumToplanacakMaclar = await context.Maclar
                    .Where(m => m.OtomatikYorumToplamaAktif && 
                               !m.YorumlarToplandi &&
                               m.Durum == MacDurumu.Bitti &&
                               m.MacTarihi.AddMinutes(150) <= simdi && // Maç bitiminden 2.5 saat sonra
                               m.MacTarihi >= simdi.AddDays(-3)) // Son 3 gün içindeki maçlar
                    .GroupBy(m => m.Hafta)
                    .Select(g => new { Hafta = g.Key, MacSayisi = g.Count() })
                    .Where(g => g.MacSayisi >= 3) // En az 3 maç olan haftalar
                    .ToListAsync();

                foreach (var hafta in yorumToplanacakMaclar)
                {
                    // Bu hafta için zaten çalışan bir job var mı kontrol et
                    var mevcutJob = _jobStatuses.Values
                        .Any(status => status.Status == "Running" || status.Status == "Queued");

                    if (!mevcutJob)
                    {
                        _logger.LogInformation($"Otomatik job başlatılıyor - Hafta {hafta.Hafta} ({hafta.MacSayisi} maç)");
                        EnqueueHaftaYorumToplama(hafta.Hafta);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otomatik job kontrolü sırasında hata oluştu");
            }
        }

        private async Task ProcessJob(BackgroundJob job, CancellationToken cancellationToken)
        {
            try
            {
                _jobStatuses[job.Id] = new JobStatus { Status = "Running", Message = "İşlem çalışıyor...", UpdatedAt = DateTime.Now };
                _logger.LogInformation($"Job işleniyor: {job.Id} - {job.Type}");

                switch (job.Type)
                {
                    case JobType.HaftaYorumToplama:
                        await ProcessHaftaYorumToplama(job, cancellationToken);
                        break;
                }

                _jobStatuses[job.Id] = new JobStatus { Status = "Completed", Message = "İşlem başarıyla tamamlandı", UpdatedAt = DateTime.Now };
                _logger.LogInformation($"Job başarıyla tamamlandı: {job.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Job {job.Id} işleme hatası");
                _jobStatuses[job.Id] = new JobStatus { Status = "Failed", Message = $"Hata: {ex.Message}", UpdatedAt = DateTime.Now };
            }
        }

        private async Task ProcessHaftaYorumToplama(BackgroundJob job, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hakemYorumuService = scope.ServiceProvider.GetRequiredService<HakemYorumuToplamaServisi>();

            dynamic parameters = job.Parameters;
            int hafta = parameters.hafta;

            _logger.LogInformation($"Hafta {hafta} için yorum toplama başlatılıyor");

            var haftaninMaclari = await context.Maclar
                .Where(m => m.Hafta == hafta && 
                           m.Liga == "Süper Lig" &&
                           m.Durum == MacDurumu.Bitti &&
                           !m.YorumlarToplandi)
                .ToListAsync(cancellationToken);

            if (!haftaninMaclari.Any())
            {
                _logger.LogInformation($"Hafta {hafta} için işlenecek maç bulunamadı");
                return;
            }

            int basariliSayisi = 0;
            int toplamMac = haftaninMaclari.Count;

            _logger.LogInformation($"Hafta {hafta} - {toplamMac} maç işlenecek");

            for (int i = 0; i < haftaninMaclari.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var mac = haftaninMaclari[i];
                
                // Progress güncelle
                var progress = (i + 1) * 100 / toplamMac;
                _jobStatuses[job.Id] = new JobStatus 
                { 
                    Status = "Running", 
                    Message = $"İşleniyor: {mac.EvSahibi} vs {mac.Deplasman} ({i + 1}/{toplamMac}) - %{progress}",
                    Progress = progress,
                    UpdatedAt = DateTime.Now
                };

                try
                {
                    _logger.LogInformation($"Maç işleniyor: {mac.EvSahibi} vs {mac.Deplasman} (ID: {mac.Id})");
                    var basarili = await hakemYorumuService.MacIcinYorumTopla(mac.Id);
                    
                    if (basarili)
                    {
                        basariliSayisi++;
                        _logger.LogInformation($"✅ Maç başarıyla işlendi: {mac.EvSahibi} vs {mac.Deplasman}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Maç işlenemedi: {mac.EvSahibi} vs {mac.Deplasman}");
                    }
                    
                    // Rate limiting - API limitlerini aşmamak için
                    await Task.Delay(3000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Maç {mac.Id} ({mac.EvSahibi} vs {mac.Deplasman}) işleme hatası");
                }
            }

            _logger.LogInformation($"🏁 Hafta {hafta} yorum toplama tamamlandı: {basariliSayisi}/{toplamMac} başarılı");
        }

        public Dictionary<string, JobStatus> GetAllJobStatuses()
        {
            try 
            {
                return _jobStatuses ?? new Dictionary<string, JobStatus>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllJobStatuses hatası");
                return new Dictionary<string, JobStatus>();
            }
        }

        public List<JobStatus> GetAllActiveJobs()
        {
            try
            {
                if (_jobQueue == null || _jobStatuses == null)
                    return new List<JobStatus>();
                    
                return _jobQueue.Where(job => _jobStatuses.ContainsKey(job.Id) && 
                                             (_jobStatuses[job.Id].Status == "Queued" || _jobStatuses[job.Id].Status == "Running"))
                           .Select(job => _jobStatuses[job.Id])
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllActiveJobs hatası");
                return new List<JobStatus>();
            }
        }

        public bool CancelJob(string jobId)
        {
            try
            {
                if (_jobStatuses.ContainsKey(jobId))
                {
                    _jobStatuses[jobId] = new JobStatus
                    {
                        Status = "Cancelled",
                        Message = "İşlem kullanıcı tarafından iptal edildi",
                        Progress = 0,
                        UpdatedAt = DateTime.Now
                    };
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        public string StartHaftaTopluYorumTopla(int hafta)
        {
            return EnqueueHaftaYorumToplama(hafta);
        }
    }

    public class BackgroundJob
    {
        public string Id { get; set; } = string.Empty;
        public JobType Type { get; set; }
        public dynamic Parameters { get; set; } = new { };
        public DateTime CreatedAt { get; set; }
    }

    public enum JobType
    {
        HaftaYorumToplama
    }

    public class JobStatus
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Progress { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
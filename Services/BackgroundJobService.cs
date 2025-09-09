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
            _logger.LogInformation("BackgroundJobService başlatıldı");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
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

        private async Task ProcessJob(BackgroundJob job, CancellationToken cancellationToken)
        {
            try
            {
                _jobStatuses[job.Id] = new JobStatus { Status = "Running", Message = "İşlem çalışıyor..." };
                _logger.LogInformation($"Job işleniyor: {job.Id} - {job.Type}");

                switch (job.Type)
                {
                    case JobType.HaftaYorumToplama:
                        await ProcessHaftaYorumToplama(job, cancellationToken);
                        break;
                }

                _jobStatuses[job.Id] = new JobStatus { Status = "Completed", Message = "İşlem başarıyla tamamlandı" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Job {job.Id} işleme hatası");
                _jobStatuses[job.Id] = new JobStatus { Status = "Failed", Message = $"Hata: {ex.Message}" };
            }
        }

        private async Task ProcessHaftaYorumToplama(BackgroundJob job, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hakemYorumuService = scope.ServiceProvider.GetRequiredService<HakemYorumuToplamaServisi>();

            dynamic parameters = job.Parameters;
            int hafta = parameters.hafta;

            var haftaninMaclari = await context.Maclar
                .Where(m => m.Hafta == hafta && 
                           m.Liga == "Süper Lig" &&
                           m.Durum == MacDurumu.Bitti &&
                           !m.YorumlarToplandi)
                .ToListAsync(cancellationToken);

            int basariliSayisi = 0;
            int toplamMac = haftaninMaclari.Count(); // Parantez eklendi

            for (int i = 0; i < haftaninMaclari.Count(); i++) // Parantez eklendi
            {
                if (cancellationToken.IsCancellationRequested) break;

                var mac = haftaninMaclari[i];
                
                // Progress güncelle
                var progress = (i + 1) * 100 / toplamMac;
                _jobStatuses[job.Id] = new JobStatus 
                { 
                    Status = "Running", 
                    Message = $"İşleniyor: {i + 1}/{toplamMac} maç (%{progress})",
                    Progress = progress
                };

                try
                {
                    var basarili = await hakemYorumuService.MacIcinYorumTopla(mac.Id);
                    if (basarili)
                    {
                        basariliSayisi++;
                    }
                    
                    _logger.LogInformation($"Maç işlendi: {mac.EvSahibi} vs {mac.Deplasman} - Başarılı: {basarili}");
                    
                    // Rate limiting
                    await Task.Delay(2000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Maç {mac.Id} işleme hatası");
                }
            }

            _logger.LogInformation($"Hafta {hafta} yorum toplama tamamlandı: {basariliSayisi}/{toplamMac} başarılı");
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
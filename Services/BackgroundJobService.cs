using System.Collections.Concurrent;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HakemYorumlari.Services
{
    public class BackgroundJobService : BackgroundService, IBackgroundJobService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly ConcurrentQueue<BackgroundJob> _jobQueue = new();
        private readonly ConcurrentDictionary<string, JobStatus> _jobStatuses = new();
        private readonly ConcurrentDictionary<string, JobMetrics> _jobMetrics = new();
        private DateTime _lastAutoJobCheck = DateTime.MinValue;
        private int _totalJobsProcessed = 0;
        private int _successfulJobs = 0;
        private int _failedJobs = 0;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellationTokens = new();

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
            // Her job için bir CTS hazırla
            _jobCancellationTokens[jobId] = new CancellationTokenSource();

            _logger.LogInformation("Hafta {Hafta} yorum toplama işi kuyruğa eklendi: {JobId}", hafta, jobId);
            return jobId;
        }

        public JobStatus? GetJobStatus(string jobId)
        {
            return _jobStatuses.TryGetValue(jobId, out var status) ? status : null;
        }

        public HealthStatus GetHealthStatus()
        {
            var activeJobs = _jobStatuses.Values.Count(j => j.Status == "Running" || j.Status == "Queued");
            var successRate = _totalJobsProcessed > 0 ? (_successfulJobs * 100 / _totalJobsProcessed) : 0;
            var recentFailures = _jobMetrics.Values
                .Where(m => m.StartTime > DateTime.Now.AddHours(-1) && m.Status == "Failed")
                .Count();

            return new HealthStatus
            {
                Status = activeJobs > 10 ? "Overloaded" : "Healthy",
                ActiveJobs = activeJobs,
                TotalProcessed = _totalJobsProcessed,
                SuccessRate = successRate,
                RecentFailures = recentFailures,
                LastCheck = DateTime.Now
            };
        }

        public JobPerformanceReport GetPerformanceReport()
        {
            var metrics = _jobMetrics.Values.ToList();
            var avgDuration = metrics.Any() ?
                TimeSpan.FromMilliseconds(metrics.Average(m => m.Duration.TotalMilliseconds)) :
                TimeSpan.Zero;

            return new JobPerformanceReport
            {
                TotalJobs = _totalJobsProcessed,
                SuccessfulJobs = _successfulJobs,
                FailedJobs = _failedJobs,
                AverageDuration = avgDuration,
                SlowestJob = metrics.OrderByDescending(m => m.Duration).FirstOrDefault(),
                FastestJob = metrics.OrderBy(m => m.Duration).FirstOrDefault()
            };
        }

        // IBackgroundJobService arayüzünden eksik olan metodu ekle
        public Dictionary<string, JobStatus> GetAllJobStatuses()
        {
            try
            {
                var result = new Dictionary<string, JobStatus>();
                
                // Son 24 saatteki job'ları al (eski job'ları temizle)
                var cutoffTime = DateTime.Now.AddHours(-24);
                
                foreach (var kvp in _jobStatuses)
                {
                    if (kvp.Value.UpdatedAt > cutoffTime)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
                
                _logger.LogInformation($"GetAllJobStatuses çağrıldı - {result.Count} aktif job durumu döndürülüyor");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllJobStatuses hatası");
                return new Dictionary<string, JobStatus>();
            }
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
                        _logger.LogInformation("Otomatik job kontrolü yapılıyor...");
                        await CheckAndEnqueueAutoJobs();
                        _lastAutoJobCheck = DateTime.Now;
                    }

                    // Kuyruktaki job'ları işle
                    if (_jobQueue.TryDequeue(out var job))
                    {
                        _logger.LogInformation($"Job işleniyor: {job.Id}");
                        // Job’a özel CTS ile host token’ını linkle
                        var jobCts = _jobCancellationTokens.GetOrAdd(job.Id, new CancellationTokenSource());
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, jobCts.Token);
                        await ProcessJob(job, linkedCts.Token);
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
                               m.MacTarihi.AddMinutes(150) <= simdi) // Maç bitiminden 2.5 saat sonra
                    .GroupBy(m => m.Hafta)
                    .Select(g => new { Hafta = g.Key, MacSayisi = g.Count() })
                    .ToListAsync(); // En az 3 maç kriterini kaldırdım

                foreach (var hafta in yorumToplanacakMaclar)
                {
                    // Bu hafta için zaten çalışan bir job var mı kontrol et
                    var mevcutJob = _jobStatuses.Values
                        .Any(status => status.Status == "Running" || status.Status == "Queued");

                    if (!mevcutJob)
                    {
                        _logger.LogInformation("Otomatik job başlatılıyor - Hafta {Hafta} ({MacSayisi} maç)", hafta.Hafta, hafta.MacSayisi);
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var jobMetrics = new JobMetrics
            {
                JobId = job.Id,
                JobType = job.Type.ToString(),
                StartTime = DateTime.Now
            };

            try
            {
                _jobStatuses[job.Id] = new JobStatus { Status = "Running", Message = "İşlem çalışıyor...", UpdatedAt = DateTime.Now };

                // Structured logging - parametreleri doğru sırada ver
                _logger.LogInformation("Job başlatıldı: {JobId} - {JobType} - {Parameters}",
                    job.Id, job.Type.ToString(), JsonSerializer.Serialize(job.Parameters));

                switch (job.Type)
                {
                    case JobType.HaftaYorumToplama:
                        await ProcessHaftaYorumToplama(job, cancellationToken);
                        break;
                }

                stopwatch.Stop();
                jobMetrics.EndTime = DateTime.Now;
                jobMetrics.Duration = stopwatch.Elapsed;
                jobMetrics.Status = "Completed";

                _jobStatuses[job.Id] = new JobStatus
                {
                    Status = "Completed",
                    Message = "İşlem başarıyla tamamlandı",
                    UpdatedAt = DateTime.Now,
                    Duration = stopwatch.Elapsed
                };

                _successfulJobs++;
                _totalJobsProcessed++;

                // Performance logging
                _logger.LogInformation("✅ Job başarıyla tamamlandı: {JobId} - Süre: {Duration}ms - Toplam İşlenen: {TotalJobs} - Başarı Oranı: {SuccessRate}%",
                    job.Id, stopwatch.ElapsedMilliseconds, _totalJobsProcessed,
                    _totalJobsProcessed > 0 ? (_successfulJobs * 100 / _totalJobsProcessed) : 0);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                jobMetrics.Status = "Cancelled";
                jobMetrics.EndTime = DateTime.Now;
                jobMetrics.Duration = stopwatch.Elapsed;

                _jobStatuses[job.Id] = new JobStatus
                {
                    Status = "Cancelled",
                    Message = "İşlem iptal edildi",
                    UpdatedAt = DateTime.Now
                };

                _logger.LogWarning("⚠️ Job iptal edildi: {JobId} - Süre: {Duration}ms", job.Id, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                jobMetrics.Status = "Failed";
                jobMetrics.EndTime = DateTime.Now;
                jobMetrics.Duration = stopwatch.Elapsed;
                jobMetrics.ErrorMessage = ex.Message;
                jobMetrics.ErrorStackTrace = ex.StackTrace;

                _failedJobs++;
                _totalJobsProcessed++;

                // Detailed error logging
                _logger.LogError(ex, "❌ Job başarısız: {JobId} - {JobType} - Hata: {ErrorMessage} - Süre: {Duration}ms - Başarısızlık Oranı: {FailureRate}%",
                    job.Id, job.Type.ToString(), ex.Message, stopwatch.ElapsedMilliseconds,
                    _totalJobsProcessed > 0 ? (_failedJobs * 100 / _totalJobsProcessed) : 0);

                _jobStatuses[job.Id] = new JobStatus
                {
                    Status = "Failed",
                    Message = $"Hata: {ex.Message}",
                    UpdatedAt = DateTime.Now,
                    ErrorDetails = new ErrorDetails
                    {
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        InnerException = ex.InnerException?.Message
                    }
                };
            }
            finally
            {
                _jobMetrics[job.Id] = jobMetrics;

                // Token temizliği
                _jobCancellationTokens.TryRemove(job.Id, out var removedCts);
                removedCts?.Dispose();

                // Eski metrikleri temizle (24 saatten eski)
                var cutoffTime = DateTime.Now.AddHours(-24);
                var keysToRemove = _jobMetrics.Where(kvp => kvp.Value.StartTime < cutoffTime).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    _jobMetrics.TryRemove(key, out _);
                }
            }
        }

        public List<JobStatus> GetAllActiveJobs()
        {
            return _jobStatuses.Values.Where(j => j.Status == "Running" || j.Status == "Queued").ToList();
        }

        public bool CancelJob(string jobId)
        {
            if (_jobStatuses.TryGetValue(jobId, out var status))
            {
                // ✅ Sadece aktif jobları iptal et
                if (status.Status == "Running" || status.Status == "Queued")
                {
                    // Job’a özel token’ı iptal et
                    if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
                    {
                        cts.Cancel();
                    }

                    status.Status = "Cancelled";
                    status.Message = "İşlem kullanıcı tarafından iptal edildi";
                    status.UpdatedAt = DateTime.Now;
                    return true;
                }
            }
            return false;
        }

        //public bool CancelJob(string jobId)
        //{
        //    try
        //    {
        //        if (_jobStatuses.ContainsKey(jobId))
        //        {
        //            _jobStatuses[jobId] = new JobStatus
        //            {
        //                Status = "Cancelled",
        //                Message = "İşlem kullanıcı tarafından iptal edildi",
        //                Progress = 0,
        //                UpdatedAt = DateTime.Now
        //            };
        //            return true;
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public string StartHaftaTopluYorumTopla(int hafta)
        {
            return EnqueueHaftaYorumToplama(hafta);
        }

        // En iyi performans için: Tek job + Asenkron + Progress tracking
        private async Task ProcessHaftaYorumToplama(BackgroundJob job, CancellationToken cancellationToken)
        {
            var parametersJson = JsonSerializer.Serialize(job.Parameters);
            var parametersDoc = JsonDocument.Parse(parametersJson);
            var hafta = parametersDoc.RootElement.GetProperty("hafta").GetInt32();

            _logger.LogInformation("Hafta {Hafta} için yorum toplama işlemi başlatılıyor.", hafta);

            // Her işlem için yeni bir scope oluşturuluyor
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var haftaninMaclari = await context.Maclar
                .Where(m => m.Hafta == hafta && m.OtomatikYorumToplamaAktif && !m.YorumlarToplandi && m.Durum == MacDurumu.Bitti)
                .OrderBy(m => m.MacTarihi) // OrderBy eklendi
                .ToListAsync(cancellationToken);

            if (!haftaninMaclari.Any())
            {
                _logger.LogWarning("Hafta {Hafta} için işlenecek maç bulunamadı.", hafta);
                _jobStatuses[job.Id].Message = $"Hafta {hafta} için işlenecek maç bulunamadı.";
                return;
            }

            var completedCount = 0;
            var totalCount = haftaninMaclari.Count;

            // Maçları sırayla işle (paralel işlem yerine)
            foreach (var mac in haftaninMaclari)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("İşlem iptal edildi.");
                    break;
                }

                // Her maç için yeni bir scope ve servis örneği oluştur
                using var macScope = _serviceProvider.CreateScope();
                var yorumToplamaServisi = macScope.ServiceProvider.GetRequiredService<HakemYorumuToplamaServisi>();

                try
                {
                    await yorumToplamaServisi.MacIcinYorumTopla(mac.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{EvSahibi} vs {Deplasman} maçı için yorum toplama başarısız.", mac.EvSahibi, mac.Deplasman);
                }

                completedCount++;
                _jobStatuses[job.Id].Progress = (completedCount * 100) / totalCount;
                _jobStatuses[job.Id].Message = $"İşlenen: {completedCount}/{totalCount} - {mac.EvSahibi} vs {mac.Deplasman}";
            }

            _logger.LogInformation("Hafta {Hafta} yorum toplama işlemi tamamlandı.", hafta);
        }


    }


    public class BackgroundJob
    {
        public string Id { get; set; } = string.Empty;
        public JobType Type { get; set; }
        public object Parameters { get; set; } = new { }; // dynamic yerine object
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
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public ErrorDetails? ErrorDetails { get; set; }
    }

    // Yeni sınıflar
    public class JobMetrics
    {
        public string JobId { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string? ErrorStackTrace { get; set; }
    }

    public class ErrorDetails
    {
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }

    public class HealthStatus
    {
        public string Status { get; set; } = string.Empty;
        public int ActiveJobs { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessRate { get; set; }
        public int RecentFailures { get; set; }
        public DateTime LastCheck { get; set; }
    }

    public class JobPerformanceReport
    {
        public int TotalJobs { get; set; }
        public int SuccessfulJobs { get; set; }
        public int FailedJobs { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public JobMetrics? SlowestJob { get; set; }
        public JobMetrics? FastestJob { get; set; }
    }

}
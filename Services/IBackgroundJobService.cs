using HakemYorumlari.Services;

namespace HakemYorumlari.Services
{
    public interface IBackgroundJobService
    {
        string EnqueueHaftaYorumToplama(int hafta);
        JobStatus? GetJobStatus(string jobId);
        Dictionary<string, JobStatus> GetAllJobStatuses();
        List<JobStatus> GetAllActiveJobs();
        bool CancelJob(string jobId);
        string StartHaftaTopluYorumTopla(int hafta);
        
        // Yeni metodlar
        HealthStatus GetHealthStatus();
        JobPerformanceReport GetPerformanceReport();
    }
}
using HakemYorumlari.Services;

namespace HakemYorumlari.Services
{
    public interface IBackgroundJobService
    {
        string EnqueueHaftaYorumToplama(int hafta);
        string StartHaftaTopluYorumTopla(int hafta);
        JobStatus? GetJobStatus(string jobId);
        Dictionary<string, JobStatus> GetAllJobStatuses(); // List yerine Dictionary
        List<JobStatus> GetAllActiveJobs();
        bool CancelJob(string jobId);
    }
}
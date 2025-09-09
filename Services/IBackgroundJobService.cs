using HakemYorumlari.Services;

namespace HakemYorumlari.Services
{
    public interface IBackgroundJobService
    {
        string EnqueueHaftaYorumToplama(int hafta);
        JobStatus? GetJobStatus(string jobId);
    }
}
using System.Globalization;
using System.Text.Json;
using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJobGrain<in TJobDetails> : IGrainWithGuidKey
    where TJobDetails : class
{
    Task SetJobDetails(TJobDetails details);
    Task Start();
    Task Cancel();
    Task<JobStatus> GetStatus();
    Task<JobErrorInformation> GetErrorInformation();
}
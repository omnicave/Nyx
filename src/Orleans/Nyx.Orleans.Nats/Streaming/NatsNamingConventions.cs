using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Nyx.Orleans.Nats.Streaming;

internal class NatsNamingConventions
{
    private readonly string _providerName;
    private readonly IOptions<ClusterOptions> _clusterOptions;

    public NatsNamingConventions(string providerName, IOptions<ClusterOptions> clusterOptions)
    {
        _providerName = providerName;
        _clusterOptions = clusterOptions;

        Prefix = $"{clusterOptions.Value.ClusterId}-{clusterOptions.Value.ServiceId}";
        SubjectPattern = $"{Prefix}-{providerName}.*.*";
        StreamName = $"orleans-streaming-{Prefix}-{providerName}";
        StreamConsumerName = $"orleans-streaming-{Prefix}-{providerName}";
    }

    public string StreamConsumerName { get; }

    public string StreamName { get; }

    public string SubjectPattern { get; }

    public string Prefix { get; }

    public string GetSubject(Guid streamGuid, string streamNamespace) => 
        $"{Prefix}-{_providerName}.{streamNamespace}.{streamGuid:N}";
    
#if NET7_0
    public string GetSubject(StreamId streamId) => 
        $"{Prefix}-{_providerName}.{streamId.ToString()}";
#endif
}
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsStreamingOptions
{
    public NatsStreamingOptions()
    {
        NatsUrl = "nats://localhost:4222";
    }

    public string NatsUrl { get; set; }
    
    public string? Prefix { get; set; }

    public Func<StreamConfig, StreamConfig>? StreamConfigurationBuilder { get; set; } = (c) => c;
}
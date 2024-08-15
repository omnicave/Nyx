using NATS.Client.JetStream;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsStreamingOptions
{
    public NatsStreamingOptions()
    {
        NatsUrl = "nats://localhost:4222";
    }

    public string NatsUrl { get; set; }
    
    public string? Prefix { get; set; }

    public Func<StreamConfiguration.StreamConfigurationBuilder, StreamConfiguration.StreamConfigurationBuilder>? StreamConfigurationBuilder { get; set; }
}
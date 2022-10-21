namespace Nyx.Orleans.Nats.Streaming;

public class NatsStreamingOptions
{
    public NatsStreamingOptions()
    {
        NatsUrl = "nats://localhost:4222";
    }

    public string NatsUrl { get; set; }
}
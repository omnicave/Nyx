namespace Nyx.Orleans.Nats.Streaming;

public class NatsClientPersistentStreamConfigurator : ClusterClientPersistentStreamConfigurator
{
    public NatsClientPersistentStreamConfigurator(
        string name, 
        IClientBuilder builder) : base(name, builder, NatsJetStreamAdapterFactory.Create)
    {
        
    }
}
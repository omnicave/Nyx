using Orleans;
using Orleans.Hosting;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsStreamClusterClientConfigurator : ClusterClientPersistentStreamConfigurator
{
    public NatsStreamClusterClientConfigurator(string name, IClientBuilder clientBuilder) : base(name, clientBuilder, NatsJetStreamAdapterFactory.Create)
    {
    }
}
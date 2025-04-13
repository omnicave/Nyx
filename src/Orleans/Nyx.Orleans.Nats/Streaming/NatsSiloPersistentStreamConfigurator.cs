using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.JetStream.Models;
using Orleans.Configuration;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsSiloPersistentStreamConfigurator : SiloPersistentStreamConfigurator
{
    public NatsSiloPersistentStreamConfigurator(
        string name,
        Action<Action<IServiceCollection>> configureServicesDelegate) : base(name, configureServicesDelegate,
        NatsJetStreamAdapterFactory.Create)
    {
        ConfigureDelegate(services =>
        {
            services
                .ConfigureNamedOptionForLogging<NatsStreamingOptions>(name)
                .ConfigureNamedOptionForLogging<SimpleQueueCacheOptions>(name)
                .ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
        });
    }

    public NatsSiloPersistentStreamConfigurator WithNatsAddress(string natsUrl)
    {
        this.Configure<NatsStreamingOptions>(builder => builder.Configure(o => o.NatsUrl = natsUrl));
        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithQueueCount(int queueCount)
    {
        this.Configure<HashRingStreamQueueMapperOptions>(builder =>
            builder.Configure(o => o.TotalQueueCount = queueCount));
        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithPrefix(string prefix)
    {
        this.Configure<NatsStreamingOptions>(builder => builder.Configure(o => o.Prefix = prefix));
        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithNatsStreamConfigBuilder(
        Func<StreamConfig, StreamConfig> streamConfigDelegate)
    {
        this.Configure<NatsStreamingOptions>(
            builder => builder.Configure(o => o.StreamConfigurationBuilder = streamConfigDelegate)
        );
        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithNatsConsumerConfigBuilder(
        Func<ConsumerConfig, ConsumerConfig> consumerConfigDelegate
    )
    {
        this.Configure<NatsStreamingOptions>(
            builder => builder.Configure(o => o.ConsumerConfigurationBuilder = consumerConfigDelegate)
        );

        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithNatsConsumerName(string consumerName)
    {
        this.Configure<NatsStreamingOptions>(
            builder => builder.Configure(o => o.ConsumerName = consumerName)
        );
        return this;
    }

    public NatsSiloPersistentStreamConfigurator WithEntryAssemblyAsConsumerName()
    {
        var name = Assembly.GetEntryAssembly()?.GetName()?.Name?.ToLower().Replace('.', '-');
        this.Configure<NatsStreamingOptions>(
            builder => builder.Configure(
                o => o.ConsumerName = name
                                      ?? throw new InvalidOperationException(
                                          "Cannot determine Assembly name of entry assembly.")
            )
        );
        return this;
    }
}
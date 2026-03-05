namespace Nyx.Orleans.Host;

public interface IExtraSiloConfiguration
{
    void Apply(HostBuilderContext context, ISiloBuilder builder);
}

public class ExtraSiloConfiguration(
    Action<HostBuilderContext, ISiloBuilder> configureDelegate,
    IExtraSiloConfiguration? previous = null
    )
    : IExtraSiloConfiguration
{
    public void Apply(HostBuilderContext context, ISiloBuilder builder)
    {
        previous?.Apply(context, builder);
        configureDelegate(context, builder);
    }
}

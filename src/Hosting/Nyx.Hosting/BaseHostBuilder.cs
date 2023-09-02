using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Hosting;

public abstract class BaseHostBuilder : IHostBuilder
{
    private readonly List<Action<IHostBuilder>> _operations = new();

    public abstract IHost Build();

    /// <inheritdoc />
    public virtual IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    /// <inheritdoc />
    public IHostBuilder ConfigureAppConfiguration(
        Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        _operations.Add( b => b.ConfigureAppConfiguration(configureDelegate));
        return this;
    }

    /// <inheritdoc />
    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
    {
        _operations.Add(b => b.ConfigureContainer(configureDelegate));
        return this;
    }

    /// <inheritdoc />
    public IHostBuilder ConfigureHostConfiguration(
        Action<IConfigurationBuilder> configureDelegate)
    {
        _operations.Add(b => b.ConfigureHostConfiguration(configureDelegate));
        return this;
    }

    /// <inheritdoc />
    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        _operations.Add( b => b.ConfigureServices(configureDelegate));
        return this;
    }

    /// <inheritdoc />
    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        if (factory == null)
            throw new ArgumentNullException(nameof (factory));
        _operations.Add(b => b.UseServiceProviderFactory(factory));
        return this;
    }

    /// <inheritdoc />
    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull
    {
        if (factory == null)
            throw new ArgumentNullException(nameof (factory));
        _operations.Add(b => b.UseServiceProviderFactory(factory));
        return this;
    }

    protected internal void ApplyHostBuilderOperations(IHostBuilder hostBuilder)
    {
        foreach (var operation in _operations)
            operation(hostBuilder);
    }
}
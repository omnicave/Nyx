namespace Nyx.Hosting.DependencyInjection;


public abstract class BaseServiceDiscoveryAttribute : Attribute { }
public abstract class BaseServiceDiscoveryAttribute<T>(Type? serviceType) : BaseServiceDiscoveryAttribute
{
    public Type? ServiceType { get; } = serviceType;

    public string Key { get; set; } = string.Empty;

    internal Type GetServiceType(Type implementationType, IServiceTypeDiscoveryConvention convention)
    {
        // return the explicitly defined service type if provided
        if (ServiceType is not null)
            return ServiceType;

        return convention.TryGetServiceType(implementationType, out var serviceType)
            ? serviceType
            : throw new InvalidOperationException($"Cannot determine service type for {implementationType.FullName}. Please specify the service type explicitly in the {nameof(T)}.");
    }

    internal bool IsKeyed => !string.IsNullOrWhiteSpace(Key);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class SingletonServiceAttribute : BaseServiceDiscoveryAttribute<SingletonServiceAttribute>
{
    public SingletonServiceAttribute(Type serviceType) : base(serviceType)
    {
    }

    public SingletonServiceAttribute() : base( null )
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ScopedServiceAttribute : BaseServiceDiscoveryAttribute<ScopedServiceAttribute>
{
    public ScopedServiceAttribute(Type serviceType) : base(serviceType)
    {
    }

    public ScopedServiceAttribute() : base( null )
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class TransientServiceAttribute : BaseServiceDiscoveryAttribute<TransientServiceAttribute>
{
    public TransientServiceAttribute(Type serviceType) : base(serviceType)
    {
    }

    public TransientServiceAttribute() : base( null )
    {
    }
}

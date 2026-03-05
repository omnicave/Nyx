namespace Nyx.Hosting.DependencyInjection;

public class DefaultServiceTypeDiscovery : IServiceTypeDiscoveryConvention
{
    public Type GetServiceType(Type implementationType)
    {
        var interfaces = implementationType
            .GetInterfaces();

        // return the interface that matches the implementation type name
        var serviceTypeMatchingImplementationTypeName = interfaces
            .FirstOrDefault(i => i.Name == $"I{implementationType.Name}");

        if (serviceTypeMatchingImplementationTypeName is not null)
            return serviceTypeMatchingImplementationTypeName;

        // if we reached this point, there was no matching interface by name, so we check
        // if there's exactly one interface implemented and return that
        if (interfaces.Length == 1)
            return interfaces[0];

        throw new InvalidOperationException($"Cannot determine service type for {implementationType.FullName}.");
    }
}

public interface IServiceTypeDiscoveryConvention
{
    Type GetServiceType(Type implementationType);

    bool TryGetServiceType(Type implementationType, out Type serviceType)
    {
        try
        {
            serviceType = GetServiceType(implementationType);
            return true;
        }
        catch
        {
            serviceType = typeof(object);
            return false;
        }
    }

}

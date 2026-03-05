using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nyx.Hosting.DependencyInjection;

public static class Extensions
{
    public static IServiceCollection AutoRegisterServicesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor? BuildServiceDescriptorUsingConventions(Type type, IServiceTypeDiscoveryConvention convention)
        {
            if (type is { IsAbstract: true })
            {
                return null;
            }

            if (type is { IsInterface: true })
            {
                return null;
            }

            var attribute = type.GetCustomAttribute<BaseServiceDiscoveryAttribute>();

            var sd = attribute switch
            {
                ScopedServiceAttribute scopedServiceAttribute => scopedServiceAttribute.IsKeyed
                    ? ServiceDescriptor.KeyedScoped(scopedServiceAttribute.GetServiceType(type, convention), scopedServiceAttribute.Key, type)
                    : ServiceDescriptor.Scoped(scopedServiceAttribute.GetServiceType(type, convention), type),
                SingletonServiceAttribute singletonServiceAttribute => singletonServiceAttribute.IsKeyed
                    ? ServiceDescriptor.KeyedSingleton(singletonServiceAttribute.GetServiceType(type, convention), singletonServiceAttribute.Key, type)
                    : ServiceDescriptor.Singleton(singletonServiceAttribute.GetServiceType(type, convention), type),
                TransientServiceAttribute transientServiceAttribute => transientServiceAttribute.IsKeyed
                    ? ServiceDescriptor.KeyedTransient(transientServiceAttribute.GetServiceType(type, convention), transientServiceAttribute.Key, type)
                    : ServiceDescriptor.Transient(transientServiceAttribute.GetServiceType(type, convention), type),
                _ => null
            };
            return sd;
        }

        var convention = new DefaultServiceTypeDiscovery();

        var descriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Select(type => BuildServiceDescriptorUsingConventions(type, convention))
            .Where(sd => sd != null)
            .Cast<ServiceDescriptor>()
            .ToList();
        
        descriptors
            .ForEach(sd =>
            {
                if (sd != null) services.Add(sd);
            });

        return services;
    }

    public static IServiceCollection AutoRegisterServicesFromAssemblyImplementingType<T>(this IServiceCollection services,
        Assembly assembly,
        Func<Type, Type, ServiceDescriptor>? serviceDescriptorFactory = null)
    {
        if (serviceDescriptorFactory == null)
            serviceDescriptorFactory = ServiceDescriptor.Transient;

        assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(T)))
            .Select(type => serviceDescriptorFactory(typeof(T), type))
            .ToList()
            .ForEach( services.Add);

        return services;
    }

}

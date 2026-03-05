using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Hosting.Modules;

public interface INyxModule
{
    
}

public interface IServiceRegistrationsBuilder
{
    IServiceCollection ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services);
}

public interface IConfigurationValidationProvider
{
    void ValidateConfiguration(IConfiguration configuration);
}


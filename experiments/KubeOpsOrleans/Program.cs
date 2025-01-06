using KubeOps.Operator;
using KubeOpsOrleans.Services;
using Nyx.Orleans.Host;
using Orleans.Serialization;

var builder = OrleansSiloHostBuilder.CreateBuilder(args);
builder
    .ConfigureClustering(siloBuilder => siloBuilder.UseLocalhostClustering())
    .ConfigureServices(collection => collection
        .AddTransient<IHomerConfigFileGenerator, HomerConfigFileGenerator>()
        .AddSerializer(serializerBuilder => 
            serializerBuilder.AddNewtonsoftJsonSerializer(
                type => new[] {"KubeOpsOrleans", "k8s"}.Any(p => type.FullName?.StartsWith(p) ?? false)
                )
            )
        .AddKubernetesOperator(settings =>
        {
            settings.EnableLeaderElection = false;
        })
        
    );
builder.ConfigureApplicationBuilder(applicationBuilder => applicationBuilder.UseKubernetesOperator());

var app = builder.Build();
await app.RunOperatorAsync(args);

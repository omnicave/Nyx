using KubeOps.Operator;
using KubeOpsOrleans.Operator;
using KubeOpsOrleans.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Nyx.Orleans.Host;
using Orleans;
using Orleans.Hosting;
using Orleans.Serialization;

var builder = OrleansSiloHostBuilder.CreateBuilder(args);
builder
    .ConfigureClustering(siloBuilder => siloBuilder.UseLocalhostClustering())
    .ConfigureServices(collection => collection
        .AddTransient<IHomerConfigFileGenerator, HomerConfigFileGenerator>()
        .AddSerializer(serializerBuilder => 
            serializerBuilder.AddNewtonsoftJsonSerializer(
                type => new[] {"KubeOpsOrleans", "k8s"}.Any(p => type.FullName.StartsWith(p))
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

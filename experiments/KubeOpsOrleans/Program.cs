using KubeOps.Operator;
using KubeOpsOrleans.Operator;
using KubeOpsOrleans.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Nyx.Orleans.Host;
using Orleans;
using Orleans.Hosting;

var builder = OrleansSiloHostBuilder.CreateBuilder(args);
builder
    .ConfigureClustering(siloBuilder => siloBuilder.UseLocalhostClustering())
    .ConfigureServices(collection => collection
    .AddTransient<IHomerConfigFileGenerator, HomerConfigFileGenerator>()
    .AddKubernetesOperator(settings =>
    {
        settings.EnableLeaderElection = false;
    })
);
builder.ConfigureApplicationBuilder(applicationBuilder => applicationBuilder.UseKubernetesOperator());

var app = builder.Build();
await app.RunOperatorAsync(args);

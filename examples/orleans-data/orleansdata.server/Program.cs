// See https://aka.ms/new-console-template for more information

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Orleans.Host;
using Orleans.Serialization;
using orleansdata.models.dao;



var host = OrleansSiloHostBuilder.CreateSiloHost("orleans-data", "orleans-data", "OrleansData Experiment", args)
    .ConfigureClustering(builder => builder.UseDevelopmentClustering((IPEndPoint)null))
    .ConfigureServices((context, collection) =>
    {
        collection.RegisterDbContext(
                "Host=localhost; Port=54321; Database=ex_orleansdata;Username=postgres;Password=password123",
                typeof(Program).Assembly
            )
            .RegisterDataMigrationStartupService()
            .AddDbContextConfigurator<DbContextConfigurator>()
            .AddSerializer(builder => builder.AddNewtonsoftJsonSerializer( t => t.FullName?.StartsWith("orleansdata") ?? false))
            ;
    })
    .Build();

await host.RunAsync();

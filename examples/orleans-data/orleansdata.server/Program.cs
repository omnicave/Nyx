// See https://aka.ms/new-console-template for more information

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli;
using Nyx.Orleans.Host;
using Orleans.Serialization;
using orleansdata.models.dao;


var host = CommandLineHostBuilder.Create(args)
    .UseHostBuilderFactory(ctx =>
    {
        if (ctx.CommandName.Equals("start-silo", StringComparison.OrdinalIgnoreCase))
        {
            var rng = new Random();
            var gatewayPort = ctx.GetSingleOptionValue<int>("gatewayPort", 12000 + rng.Next(999));
            var siloPort = ctx.GetSingleOptionValue<int>("siloPort", 13000 + rng.Next(999));
            var dashboardPort = ctx.GetSingleOptionValue<int>("dashboardPort", 5002);
            var apiPort = ctx.GetSingleOptionValue<int>("apiPort", 5001);
            var healthCheckPort = ctx.GetSingleOptionValue<int>("healthCheckPort", 5081);
            
            return OrleansSiloHostBuilder.CreateSiloHost("orleans-data", "orleans-data", "OrleansData Experiment", args, gatewayPort, siloPort, dashboardPort, apiPort, healthCheckPort )
                .ConfigureClustering(builder => builder.UseDevelopmentClustering((IPEndPoint?)null))
                .ConfigureServices((context, collection) =>
                {
                    collection.RegisterDbContext(
                            "Host=localhost; Port=54321; Database=ex_orleansdata;Username=postgres;Password=password123",
                            typeof(Program).Assembly
                        )
                        .RegisterDataMigrationStartupService()
                        .AddDbContextConfigurator<DbContextConfigurator>()
                        .AddSerializer(builder =>
                            builder.AddNewtonsoftJsonSerializer(t => t.FullName?.StartsWith("orleansdata") ?? false))
                        ;
                });
        }

        return CommandLineHostBuilder.DefaultHostBuilderFactory(ctx);

    })
    .ConfigureLoggingDefaults()
    .RegisterCommand<StartSiloComand>()
    // .WithRootCommandHandler(async (IHost host) =>
    // {
    //     var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    //
    //     var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
    //     applicationLifetime.ApplicationStopping
    //         .Register(obj =>
    //             {
    //                 var tcs = (TaskCompletionSource<object>)obj;
    //                 tcs.TrySetResult(null);
    //             },
    //             waitForStop);
    //     //
    //     await waitForStop.Task;
    // })
    .Build();

await host.RunAsync();

using System.Reflection;
using Nyx.Cli;
using Nyx.Orleans;
using Nyx.Orleans.Host;
using Nyx.Orleans.Nats;
using Orleans;

var hostBuilder = WebApplication.CreateBuilder(args)
    .ConfigureSiloHost("ExampleOrleansCluster", "Ex1");

// var builder = OrleansSiloHostBuilder.CreateSiloHost("ExampleOrleansCluster", "Ex1", args: args);
//
// builder
//     .ConfigureClustering(sb => sb.AddNatsClustering())
//     .ConfigureOrleansSilo(
//         (context, siloBuilder) =>
//         {
//             siloBuilder
//                 .AddNatsStreams(orleans.shared.Constants.NatsStreamProviderName);
//         }
//         )
//     // .ConfigureForPostgresClustering(
//     //     "Host=localhost; Port=54321; Database=postgres;Username=postgres;Password=password123")
//     .UsePostgresPubSubStore(
//         "Host=localhost; Port=54321; Database=postgres;Username=postgres;Password=password123")
//     .UsePostgresInternalGrainStorage(
//         "Host=localhost; Port=54321; Database=postgres;Username=postgres;Password=password123")
//     ;

var host = hostBuilder.Build();

await host.RunAsync();

using System.Reflection;
using Nyx.Orleans.Host;

var builder = new OrleansSiloHostBuilder(
    "ExampleOrleansCluster",
    "Ex1",
    args: args
);

builder.ConfigureForPostgresClustering("Host=localhost; Port=54321; Database=postgres;Username=postgres;Password=password123")
    .UsePostgresPubSubStore("Host=localhost; Port=54321; Database=postgres;Username=postgres;Password=password123");

var app = builder.Build();

await app.RunAsync();

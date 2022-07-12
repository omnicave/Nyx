using System.Reflection;
using Nyx.Orleans.Host;

var builder = new OrleansSiloHostBuilder(
    "ExampleOrleansCluster",
    "Ex1",
    args: args
);

builder.ConfigureForDevelopment();

var app = builder.Build();

await app.RunAsync();

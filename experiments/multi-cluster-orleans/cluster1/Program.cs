using System.Net;
using Nyx.Orleans.Host;
using Orleans;using Orleans.Hosting;

var builder = OrleansSiloHostBuilder.CreateSiloHost("MultiClusterOrleans1", "Cluster1", args: args);

builder
    .ConfigureForDevelopment()
    .RegisterExternalOrleansCluster("cluster2",
        clientBuilder => clientBuilder.UseStaticClustering(
            new IPEndPoint(IPAddress.Loopback, 12002),
            "MultiClusterOrleans2",
            "Cluster2")
    )
    ;

var host = builder.Build();

await host.RunAsync();
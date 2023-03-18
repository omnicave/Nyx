using System.Net;
using Nyx.Orleans.Host;

var builder = OrleansSiloHostBuilder.CreateSiloHost("MultiClusterOrleans2", "Cluster2", args: args);

builder
    .ConfigureForDevelopment()
    .RegisterExternalOrleansCluster("cluster1",
        clientBuilder =>
            clientBuilder.UseStaticClustering(new IPEndPoint(IPAddress.Loopback, 12001), "MultiClusterOrleans1",
                "Cluster1")
    )
    ;

var host = builder.Build();

await host.RunAsync();
// See https://aka.ms/new-console-template for more information

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli;
using Nyx.Orleans.Data;
using Nyx.Orleans.Host;
using Orleans.Streams;
using orleansdata.shared;

async Task Main(IHost host)
{
    var client = host.Services.GetRequiredService<IClusterClient>();

    Console.WriteLine("Populating data ...");
    var prepareDataGrain = client.GetGrain<IPrepareDataGrain>(Guid.Empty);
    await prepareDataGrain.PopulateDb();

    Console.WriteLine("Getting all products");

    {
        var q = client.GetGrain<IQueryGrain<Product>>(Guid.NewGuid());

        var c = await q.Count();
        Console.WriteLine($"Number of products {c}");

        var result = await q.FetchAll();
        foreach (var item in result)
        {
            Console.WriteLine(item);
        }
    }

    Console.WriteLine("======");
    {
        var q = client.GetQueryGrain<Product>();

        await q.Configure(QueryParameters.Default with
        {
            SearchString = "works"
        });

        var result = await q.FetchAll();
        
        foreach (var item in result)
        {
            Console.WriteLine(item);
        }
    }
    
    
    
    Console.WriteLine("Press any key to exit ... ");
    Console.ReadKey();

    Console.WriteLine("Exiting ... ");
}

var host = CommandLineHostBuilder.Create("orleansdata.client", args)
    .UseHostBuilderFactory(ctx =>
        new OrleansClientHostBuilder("orleansdata.client", "orleans-data", "orleans-data", args)
            .ConfigureClient(builder => builder.UseStaticClustering(new IPEndPoint(IPAddress.Loopback, 12000)))
    )
    .WithRootCommandHandler(
        async (IHost host) => { await Main(host); }
    )
    .Build();



await host.RunAsync();


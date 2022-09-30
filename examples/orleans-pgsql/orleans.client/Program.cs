// var builder = WebApplication.CreateBuilder(args);
//
// // Add services to the container.
//
// builder.Services.AddControllers();
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();
//
// var app = builder.Build();
//
// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
//
// app.UseHttpsRedirection();
//
// app.UseAuthorization();
//
// app.MapControllers();
//
// app.Run();

using System.Net;
using Nyx.Orleans.Host;
using Nyx.Orleans.Jobs;
using Nyx.Orleans.Serialization;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using orleans.shared;

var client = new ClientBuilder()
    .UseSimplifiedClustering(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12000), "ExampleOrleansCluster", "Ex1")
    // .UseStaticClustering(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12000))
    // // // Clustering information
    // .Configure<ClusterOptions>(options =>
    // {
    //     options.ClusterId = "ExampleOrleansCluster";
    //     options.ServiceId = "Ex1";
    // })
    // Application parts: just reference one of the grain interfaces that we use
    .ConfigureApplicationParts(parts => 
        parts
            .AddApplicationPart(typeof(IHelloWorldGrain).Assembly)
            .AddApplicationPart(typeof(IBackgroundJobGrain<>).Assembly)
    )
    .Configure<SerializationProviderOptions>(options =>
    {
        options.FallbackSerializationProvider = typeof(NewtonsoftJsonSerializer);
    })
    
    .Build();

await client.Connect();

var g = await client.StartJob(new TestJob());

var exit = false;
int i = 0;
while (!exit)
{
    await Task.Delay(TimeSpan.FromSeconds(1));
    var status = await g.GetStatus();
    Console.WriteLine($"Status: { await g.GetStatus() } [{i}]");
    switch (status)
    {
        case JobStatus.Finished:
            exit = true;
            break;
        case JobStatus.Failed:
            exit = true;
            break;
        default:
            break;
    }

    i++;
    
    if (i == 5)
        await g.Cancel();
}

Console.WriteLine("Exiting ... ");
await client.Close();
await client.DisposeAsync();
client.Dispose();

// var motd = await g.GetHosts();
// motd.Select(x=> $"{x.Key.Endpoint}[{x.Key.Generation}][{x.Value}][{x.Key.IsClient}]").ToList().ForEach( Console.WriteLine);
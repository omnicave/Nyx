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
using Orleans;
using Orleans.Configuration;
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
    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IHelloWorldGrain).Assembly))
    .Build();

await client.Connect();

var g = client.GetGrain<IManagementGrain>(0);

var motd = await g.GetHosts();
motd.Select(x=> $"{x.Key.Endpoint}[{x.Key.Generation}][{x.Value}][{x.Key.IsClient}]").ToList().ForEach( Console.WriteLine);
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
using Nyx.Cli;
using Nyx.Orleans;
using Nyx.Orleans.Host;
using Nyx.Orleans.Jobs;
using Nyx.Orleans.Nats;
using Nyx.Orleans.Nats.Clustering;
using Nyx.Orleans.Serialization;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using orleans.shared;
using Orleans.Streams;

var host = CommandLineHostBuilder.Create("orleans.client", args)
    .UseHostBuilderFactory(ctx =>
        new OrleansClientHostBuilder("orleans.client", "ExampleOrleansCluster", "Ex1")
            .ConfigureClient(builder =>
            {
                builder.AddNatsClustering()
                    .AddNatsStreams(orleans.shared.Constants.NatsStreamProviderName);
            })
    )
    .WithRootCommandHandler(
        async (IHost host) =>
        {
            var client = host.Services.GetRequiredService<IClusterClient>();
            var testStreamListener = client.GetGrain<ITestStreamListenerGrain>(Guid.Empty);
            await testStreamListener.Start();

            var stream = client.GetStreamProvider(orleans.shared.Constants.NatsStreamProviderName)
                .GetStream<TestStreamMessage>(StreamConstants.StreamNamespace, StreamConstants.StreamId);

            int i = 0;
            while (i < 20)
            {
                await stream.OnNextAsync(new TestStreamMessage($"Iteration from client {i++}", i));
            }

            Console.WriteLine("Press any key to exit ... ");
            Console.ReadKey();

            Console.WriteLine("Exiting ... ");
        }
    )
    .Build();

await host.RunAsync();

// var g = await client.StartJob(new TestJob());


// Func<TestStreamMessage,StreamSequenceToken,Task> processMessages = (msg, sequence) =>
// {
//     Console.WriteLine($"{msg.Message} [{msg.Number}]");
//     return Task.CompletedTask;
// };
// var result = await stream.SubscribeAsync(processMessages);



// var exit = false;
// int i = 0;
// while (!exit)
// {
//     await Task.Delay(TimeSpan.FromSeconds(1));
//     var status = await g.GetStatus();
//     Console.WriteLine($"Status: { await g.GetStatus() } [{i}]");
//     switch (status)
//     {
//         case JobStatus.Finished:
//             exit = true;
//             break;
//         case JobStatus.Failed:
//             exit = true;
//             break;
//         default:
//             break;
//     }
//
//     i++;
//     
//     if (i == 5)
//         await g.Cancel();
// }



// var motd = await g.GetHosts();
// motd.Select(x=> $"{x.Key.Endpoint}[{x.Key.Generation}][{x.Value}][{x.Key.IsClient}]").ToList().ForEach( Console.WriteLine);


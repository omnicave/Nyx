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

using Orleans;
using Orleans.Configuration;
using orleans.shared;

var client = new ClientBuilder()
    .UseLocalhostClustering(12000)
    // Clustering information
    .Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "ExampleOrleansCluster";
        options.ServiceId = "Ex1";
    })
    // Application parts: just reference one of the grain interfaces that we use
    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IHelloWorldGrain).Assembly))
    .Build();

await client.Connect();

var g = client.GetGrain<IHelloWorldGrain>(Guid.NewGuid());

var motd = await g.GetMotd();
Console.WriteLine($"Motd: {motd ?? "<null>"}");
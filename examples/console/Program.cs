// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli;
using Nyx.Examples.Console;

// var result = await CommandLineHostBuilder.Create(args)
//     .RegisterCommandsFromThisAssembly()
//     .AddOutputFormatGlobalFlag()
//     .AddGlobalOption<GlobalOption>("globalone", "g")
//     .AddGlobalOption<string>("token")
//     .RunAsync();
//
// return result;
//

await CommandLineHostBuilder.Create(args)
    .ConfigureServices((context, collection) =>
    {
        collection.AddScoped<IRandomTextService, RandomTextService>();
    })
    .WithRootCommandHandler((IRandomTextService randomTextService, int max) =>
    {
        Console.WriteLine(randomTextService.GetRandomSentence(max));
    })
    .RegisterCommandsFromThisAssembly()
    .AddOutputFormatGlobalFlag()
    .AddGlobalOption<GlobalOption>("globalone", "g")
    .AddGlobalOption<string>("token")
    .Build()
    .RunAsync();

public enum GlobalOption
{
    Unset,
    Option1,
    Option2,
    Option3
}
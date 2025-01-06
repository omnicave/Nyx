// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli;
using Nyx.Examples.Console;
using Nyx.Utils.Collections;

// var result = await CommandLineHostBuilder.Create(args)
//     .RegisterCommandsFromThisAssembly()
//     .AddOutputFormatGlobalFlag()
//     .AddGlobalOption<GlobalOption>("globalone", "g")
//     .AddGlobalOption<string>("token")
//     .RunAsync();
//
// return result;
//

// await CommandLineHostBuilder.Create(args)
//     .ConfigureServices((context, collection) =>
//     {
//         collection.AddScoped<IRandomTextService, RandomTextService>();
//     })
//     // .WithRootCommandHandler((IRandomTextService randomTextService, int max) =>
//     // {
//     //     Console.WriteLine(randomTextService.GetRandomSentence(max));
//     // })
//     .ConfigureLoggingDefaults()
//     .RegisterCommandsFromThisAssembly()
//     .AddOutputFormatGlobalFlag()
//     .AddGlobalOption<GlobalOption>("globalone", "g")
//     .AddGlobalOption<string>("token")
//     .Build()
//     .RunAsync();
//



var opt = new JsonSerializerOptions()
{

};

var s = JsonSerializer.Serialize(new ValueCollection<GlobalOption>(new[] { GlobalOption.Option1 }), opt );

Console.WriteLine(s);

JsonSerializer.Deserialize<ValueCollection<GlobalOption>>(s, opt);



public enum GlobalOption
{
    Unset,
    Option1,
    Option2,
    Option3
}
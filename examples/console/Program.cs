// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Hosting;
using Nyx.Cli;

var result = await CommandLineHostBuilder.Create(args)
    .RegisterCommandsFromThisAssembly()
    .AddOutputFormatGlobalFlag()
    .AddGlobalOption<GlobalOption>("globalone", "g")
    .AddGlobalOption<string>("token")
    .RunAsync();

return result;

public enum GlobalOption
{
    Unset,
    Option1,
    Option2,
    Option3
}

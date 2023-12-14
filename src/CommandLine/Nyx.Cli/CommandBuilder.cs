using System;
using System.CommandLine;
using System.Threading;

namespace Nyx.Cli;

internal interface ICommandBuilder
{
    Command Build(CliHostBuilderContext cliHostBuilderContext);
}

internal interface IRootCommandBuilder
{
    Command Build();
}
using System;
using System.CommandLine;
using System.Threading;

namespace Nyx.Cli;

internal interface ICommandBuilder
{
    Command Build();
}

internal interface IRootCommandBuilder
{
    Command Build();
}
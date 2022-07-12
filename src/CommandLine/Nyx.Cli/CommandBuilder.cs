using System.CommandLine;
using System.Threading;

namespace Nyx.Cli;

internal interface ICommandBuilder
{
    Command Build();
}
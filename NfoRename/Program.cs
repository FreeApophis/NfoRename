using System.CommandLine;
using NfoRename.Cli;

namespace NfoRename;

internal static class Program
{
    public static async Task<int> Main(string[] programArguments)
        => await new RootCommandBuilder()
            .AddCommandToRoot(new CheckCommand())
            .AddCommandToRoot(new RepairCommand())
            .Build()
            .InvokeAsync(programArguments);
}
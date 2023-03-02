using System.CommandLine;

namespace NfoRename.Cli;

internal class RootCommandBuilder
{
    private readonly RootCommand _root = new("nfo-rename");

    public RootCommand Build()
        => _root;

    public RootCommandBuilder AddCommandToRoot(Command command)
    {
        _root.AddCommand(command);

        return this;
    }
}
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Spectre.Console;
using NfoRename.Engine;

namespace NfoRename.Cli;

internal class RepairCommand : Command
{
    public RepairCommand() : base("repair", "Fixes file names which are consistent with format and the information in the .nfo files.")
    {
        Handler = CommandHandler.Create<IConsole, DirectoryInfo, bool>(HandleCommand);

        Add(new Option<bool>(new[] { "-f", "--force" }, "Rename everything without interactively asking for each change."));
        Add(new Argument<DirectoryInfo> { Name = "searchPath", Arity = ArgumentArity.ExactlyOne, Description = "Path to the .nfo and media files." });

    }

    private static int HandleCommand(IConsole console, DirectoryInfo searchPath, bool force)
    {
        try
        {
            var formatterRules = FormatterRules.Default;
            var repairRoot = new RepairRoot(new MediaFormatter(formatterRules, new FilenameFormatter(formatterRules)));

            return repairRoot.Run(searchPath, force);
        }
        catch (Exception exception)
        {
            AnsiConsole.Write(new Markup(exception.Message, new Style(foreground: Color.Maroon)));
            return ProgramResult.FileRepairFailed;
        }
    }


}
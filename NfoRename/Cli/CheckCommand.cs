using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using NfoRename.Engine;
using Spectre.Console;

namespace NfoRename.Cli;

internal class CheckCommand : Command
{
    public CheckCommand() : base("check", "Checks if the file names are consistent with format and the information in the .nfo files.")
    {
        Handler = CommandHandler.Create<IConsole, DirectoryInfo, bool>(HandleCommand);

        Add(new Option<bool>(new[] { "-e", "--errors-only" }, "Only show the errors."));
        Add(new Argument<DirectoryInfo> { Name = "searchPath", Arity = ArgumentArity.ExactlyOne, Description = "Path to the .nfo and media files." });
    }

    private static int HandleCommand(IConsole console, DirectoryInfo searchPath, bool errorsOnly)
    {
        try
        {
            var formatterRules = FormatterRules.Default;
            var checkService = new CheckRoot(new MediaFormatter(formatterRules, new FilenameFormatter(formatterRules)));

            return checkService.Run(searchPath, errorsOnly);
        }
        catch (Exception exception)
        {
            AnsiConsole.Write(new Markup(exception.Message, new Style(foreground: Color.Maroon)));
            return ProgramResult.FileCheckFailed;
        }
    }
}
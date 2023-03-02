using Funcky.Extensions;
using Funcky.Monads;
using NfoRename.Engine;
using Spectre.Console;

namespace NfoRename.Cli;

internal class CheckRoot
{
    private readonly MediaFormatter _mediaFormatter;

    public CheckRoot(MediaFormatter mediaFormatter)
    {
        _mediaFormatter = mediaFormatter;
    }

    public int Run(DirectoryInfo searchPath, bool errorsOnly)
    {
        var result = NfoFile.Find(searchPath.FullName)
            .Select(_mediaFormatter.CheckName)
            .Inspect(ReportCheck(ReportLine(errorsOnly)))
            .Aggregate(new ProgramResult(ProgramResult.Ok, 0, 0), CheckForFail);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[blue]Totals:[/]").LeftAligned());
        AnsiConsole.MarkupLine($" Files: [green]{result.CheckedFiles}[/]");
        AnsiConsole.MarkupLine($"Errors: [red]{result.Errors}[/]");

        return result.Result;
    }

    private static Action<Either<Error, Medium>> ReportCheck(Func<Either<Error, Medium>, Markup> reportLine)
        => nfo
            => AnsiConsole.Write(reportLine(nfo));

    private static Func<Either<Error, Medium>, Markup> ReportLine(bool errorsOnly)
        => nfo
            => errorsOnly
                ? nfo.Match(ErrorExtensions.ToMarkup, _ => new Markup(string.Empty))
                : nfo.Match(ErrorExtensions.ToMarkup, MediumExtensions.ToMarkup);


    private static ProgramResult CheckForFail(ProgramResult result, Either<Error, Medium> either)
        => either.Match(left: _ => CheckForFailLeft(result), right: _ => CheckForFailRight(result));

    private static ProgramResult CheckForFailRight(ProgramResult result)
        => result with { CheckedFiles = result.CheckedFiles + 1 };

    private static ProgramResult CheckForFailLeft(ProgramResult result)
        => new(Result: ProgramResult.FileCheckFailed, Errors: result.Errors + 1, CheckedFiles: result.CheckedFiles + 1);
}
using Funcky;
using Funcky.Extensions;
using Funcky.Monads;
using NfoRename.Engine;
using Spectre.Console;

namespace NfoRename.Cli;

internal class RepairRoot
{
    private readonly MediaFormatter _mediaFormatter;

    public RepairRoot(MediaFormatter mediaFormatter)
    {
        _mediaFormatter = mediaFormatter;
    }

    public int Run(DirectoryInfo searchPath, bool force)
    {
        NfoFile.Find(searchPath.FullName)
            .Select(_mediaFormatter.CheckName)
            .ForEach(FixName(force));

        return ProgramResult.Ok;
    }

    private static Action<Either<Error, Medium>> FixName(bool force)
        => either
            => either.Switch(left: RepairName(force), right: Functional.NoOperation);

    private static Action<Error> RepairName(bool force)
        => error
            =>
        {
            if (error is not Error.WrongName wrongName)
            {
                return;
            }

            AnsiConsole.Write(wrongName.ToMarkup());

            if (force || AnsiConsole.Confirm("Do you really want to rename?", false))
            {
                Rename(wrongName);
            }
        };

    private static void Rename(Error.WrongName wrongName)
    {
        foreach (var source in Directory.EnumerateFiles(wrongName.Location.FullName, $"{wrongName.Actual}*.*"))
        {
            File.Move(source, RepairedDestination(wrongName, source));
            AnsiConsole.Write(new Markup($"Move: [blue]{Path.GetFileName(source).EscapeMarkup()}[/] to [green]{Path.GetFileName(RepairedDestination(wrongName, source)).EscapeMarkup()}[/]\n"));
        }
    }

    private static string RepairedDestination(Error.WrongName wrongName, string filename)
        => Path.Combine(wrongName.Location.FullName, $"{wrongName.Expected}{ExtractExtension(wrongName, filename)}");

    private static string ExtractExtension(Error.WrongName wrongName, string filename)
        => Path.GetFileName(filename)[wrongName.Actual.Length..];
}
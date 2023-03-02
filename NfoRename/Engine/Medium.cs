using Funcky;
using Funcky.Monads;
using Spectre.Console;

namespace NfoRename.Engine;

[DiscriminatedUnion]

internal abstract partial record Medium(string Filename)
{
    internal partial record Movie(string Filename, string Title, int Year) : Medium(Filename);
    internal partial record Show(string Filename, string Title, int Year) : Medium(Filename);
    internal partial record Season(string Filename, Show PartOfShow, int SeasonNumber) : Medium(Filename);
    internal partial record Episode(string Filename, Season PartOfSeason, string Title, int EpisodeNumber, Option<int> EpisodeNumberTo) : Medium(Filename);
}

internal static class MediumExtensions
{
    public static Markup ToMarkup(this Medium medium)
        => medium.Match(
            movie => new Markup($"[[[green]OK[/]]]    {Path.GetFileNameWithoutExtension((string?)movie.Filename)}\n"),
            show => new Markup($"[[[green]OK[/]]]    {Directory.GetParent(show.Filename)?.Name}\n"),
            season => new Markup($"[[[green]OK[/]]]    {Directory.GetParent(season.Filename)?.Name}\n"),
            episode => new Markup($"[[[green]OK[/]]]    {Path.GetFileNameWithoutExtension((string?)episode.Filename)}\n"));
}

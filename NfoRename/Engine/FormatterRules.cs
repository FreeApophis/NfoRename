using System.Collections.Immutable;

namespace NfoRename.Engine;

internal record FormatterRules(
    FormatterRule MovieFolderFormat,
    FormatterRule ShowFolderFormat,
    string SpecialSeasonFolder,
    FormatterRule SeasonFolderFormat,
    FormatterRule EpisodeNumberFormat,
    FormatterRule EpisodeNumberFormatFromTo,
    FormatterRule EpisodeTitleFormat,
    IReadOnlyDictionary<string, string> StringReplacements)
{
    public static FormatterRules Default
        => new(
            MovieFolderFormat: new FormatterRule("{Title} ({Year})"),
            ShowFolderFormat: new FormatterRule("{ShowTitle}"),
            SpecialSeasonFolder: "Specials",
            SeasonFolderFormat: new FormatterRule("Season {SeasonNumber}"),
            EpisodeNumberFormat: new FormatterRule("{EpisodeNumber:D2}"),
            EpisodeNumberFormatFromTo: new FormatterRule("{EpisodeNumber:D2}-{EpisodeNumberTo:D2}"),
            EpisodeTitleFormat: new FormatterRule("{ShowTitle} - s{SeasonNumber:D2}e{FormattedEpisodeNumber} - {Title}"),
            StringReplacements: DefaultStringReplacements());

    private static ImmutableDictionary<string, string> DefaultStringReplacements()
        => ImmutableDictionary<string, string>.Empty
            .Add(":", " -")
            .Add("\\\\", " - ")
            .Add("/", " - ");
}
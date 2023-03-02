using Funcky.Extensions;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Funcky.Monads;

namespace NfoRename.Engine;

internal class MediaFormatter
{
    private readonly FormatterRules _formatterRules;
    private readonly FilenameFormatter _filenameFormatter;

    private static readonly Regex VariableRegex = new("{(\\w*)(:(\\S+?))?}");
    private static readonly Regex PartRegex = new("(.*)\\s\\((\\d*)\\)$");

    private readonly IReadOnlyDictionary<string, Func<Medium, string, string>> _knownVariables;

    public MediaFormatter(FormatterRules formatterRules, FilenameFormatter filenameFormatter)
    {
        _formatterRules = formatterRules;
        _filenameFormatter = filenameFormatter;
        _knownVariables = new Dictionary<string, Func<Medium, string, string>>
        {
            { Variable.SeasonNumber, ApplySeasonNumber },
            { Variable.FormattedEpisodeNumber, ApplyFormattedEpisodeNumber },
            { Variable.EpisodeNumber, ApplyEpisodeNumber },
            { Variable.EpisodeNumberTo, ApplyEpisodeNumberTo },
            { Variable.ShowTitle, ApplyShowTitle },
            { Variable.Title, ApplyTitle },
            { Variable.Year, ApplyYear },
        };
    }
    public Either<Error, Medium> CheckName(Either<Error, Medium> either)
        => from media in either
           from @checked in media.Match(CheckMovie, CheckShow, CheckSeason, CheckEpisode)
           select @checked;

    private Either<Error, Medium> CheckMovie(Medium.Movie movie)
        => Path.GetFileNameWithoutExtension(movie.Filename) == ExpectedMovieName(movie)
            ? Either<Error, Medium>.Right(movie)
            : Either<Error, Medium>.Left(new Error.WrongName(Directory.GetParent(movie.Filename)!, ExpectedMovieName(movie), Path.GetFileNameWithoutExtension(movie.Filename), false));

    private string ExpectedMovieName(Medium medium)
        => Apply(_formatterRules.MovieFolderFormat, medium);

    private Either<Error, Medium> CheckShow(Medium.Show show)
        => Path.GetFileName(Path.GetDirectoryName(show.Filename)) == ExpectedShowName(show)
            ? Either<Error, Medium>.Right(show)
            : Either<Error, Medium>.Left(new Error.WrongName(Directory.GetParent(show.Filename)!, ExpectedShowName(show), Path.GetFileName(Path.GetDirectoryName(show.Filename))!, false));

    private string ExpectedShowName(Medium medium)
        => Apply(_formatterRules.ShowFolderFormat, medium);
    private Either<Error, Medium> CheckSeason(Medium.Season season)
        => Path.GetFileName(Path.GetDirectoryName(season.Filename)) == ExpectedSeasonName(season)
            ? Either<Error, Medium>.Right(season)
            : Either<Error, Medium>.Left(new Error.WrongName(Directory.GetParent(season.Filename)!, ExpectedSeasonName(season), Path.GetFileName(Path.GetDirectoryName(season.Filename))!, false));

    private string ExpectedSeasonName(Medium.Season season)
        => season.SeasonNumber == 0
            ? _formatterRules.SpecialSeasonFolder
            : Apply(_formatterRules.SeasonFolderFormat, season);

    private Either<Error, Medium> CheckEpisode(Medium.Episode episode)
        => Path.GetFileNameWithoutExtension(episode.Filename) == ExpectedEpisodeName(episode)
            ? Either<Error, Medium>.Right(episode)
            : Either<Error, Medium>.Left(new Error.WrongName(Directory.GetParent(episode.Filename)!, ExpectedEpisodeName(episode), Path.GetFileNameWithoutExtension(episode.Filename), false));

    private string ExpectedEpisodeName(Medium medium)
        => Apply(_formatterRules.EpisodeTitleFormat, medium);

    private string Apply(FormatterRule formatterRule, Medium medium)
        => _filenameFormatter.ToLegalFilename(VariableRegex.Matches(formatterRule.Rule).Aggregate(formatterRule.Rule, Rename(medium)));

    private Func<string, Match, string> Rename(Medium medium)
        => (current, match)
            => match switch
            {
                { Groups: [_, { Value: var variable }, _, { Success: false }] } => FormatWithVariable(medium, current, variable),
                { Groups: [_, { Value: var variable }, _, { Value: var format, Success: true }] } => FormatWithVariable(medium, current, variable, format),
                _ => throw new UnreachableException("no match matches!"),
            };

    private string FormatWithVariable(Medium medium, string current, string variable, string format) =>
        _knownVariables.GetValueOrNone(variable)
            .Match(
                none: () => throw new FormatException($"{{{variable}}} is an unknown variable name.?"),
                some: apply => current.Replace($"{{{variable}:{format}}}", apply(medium, format)));

    private string FormatWithVariable(Medium medium, string current, string variable)
        => _knownVariables.GetValueOrNone(variable)
            .Match(
                none: () => throw new FormatException($"{{{variable}}} is an unknown variable name.?"),
                some: apply => current.Replace($"{{{variable}}}", apply(medium, "")));

    private static string ApplySeasonNumber(Medium medium, string format)
        => medium.Match(
            movie: _ => throw new FormatException("cannot match SeasonNumber for movie."),
            show: _ => throw new FormatException("cannot match SeasonNumber for show."),
            season: season => season.SeasonNumber.ToString(format),
            episode: episode => episode.PartOfSeason.SeasonNumber.ToString(format));

    private string ApplyFormattedEpisodeNumber(Medium medium, string format)
        => medium.Match(
            movie: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            show: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            season: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            episode: FormattedEpisodeNumber);

    private static string ApplyEpisodeNumber(Medium medium, string format)
        => medium.Match(
            movie: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            show: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            season: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            episode: episode => episode.EpisodeNumber.ToString(format));

    private static string ApplyEpisodeNumberTo(Medium medium, string format)
        => medium.Match(
            movie: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            show: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            season: _ => throw new FormatException("cannot match EpisodeNumber for movie."),
            episode: episode => episode.EpisodeNumberTo.Match(string.Empty, to => to.ToString(format)));


    private static string ApplyShowTitle(Medium medium, string format)
        => medium.Match(
            movie: _ => throw new FormatException("cannot match ShowTitle for movie, did you mean {Title}?"),
            show: show => show.Title,
            season: _ => throw new FormatException("cannot match ShowTitle for season."),
            episode: episode => episode.PartOfSeason.PartOfShow.Title);

    private static string ApplyTitle(Medium medium, string format)
        => medium.Match(
            movie: movie => movie.Title,
            show: _ => throw new FormatException("cannot match Title for show, did you mean {ShowTitle}?"),
            season: _ => throw new FormatException("cannot match Title for season."),
            episode: GetEpisodeTitle);

    private static string ApplyYear(Medium medium, string format)
        => medium.Match(
            movie: movie => movie.Year.ToString(format),
            show: show => show.Year.ToString(format),
            season: _ => throw new FormatException("cannot match Year for season."),
            episode: _ => throw new FormatException("cannot match Year for episode."));


    private static string GetEpisodeTitle(Medium.Episode episode)
        => episode.EpisodeNumberTo.Match(
            none: () => episode.Title,
            some: to => MergeTitle(episode, to - episode.EpisodeNumber + 1));

    private static string MergeTitle(Medium.Episode episode, int episodeCount)
    {
        var titles = episode.Title.Split(new[] { ',' });

        return titles.Length == episodeCount
            ? titles.Select(s => TitleOnly(ToParsedTitle(s.Trim()))).Distinct().JoinToString(", ")
            : episode.Title;
    }

    private static Title ToParsedTitle(string episodeTitle)
        => ToParsedTitle(episodeTitle, PartRegex.Matches(episodeTitle));

    private static Title ToParsedTitle(string episodeTitle, MatchCollection matches)
        => matches is [{ Groups: [_, { Value: var title }, { Value: var part }] }]
            ? new Title.EpisodeWithPart(title, int.Parse(part))
            : new Title.SingleEpisode(episodeTitle);


    private static string TitleOnly(Title title)
        => title.Match(episode => episode.Title, episode => episode.Title);

    private string FormattedEpisodeNumber(Medium.Episode episode)
        => episode.EpisodeNumberTo.Match(
            none: () => Apply(_formatterRules.EpisodeNumberFormat, episode),
            some: _ => Apply(_formatterRules.EpisodeNumberFormatFromTo, episode));
}
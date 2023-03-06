using System.Xml;
using Funcky.Extensions;
using Funcky.Monads;

namespace NfoRename.Engine;

internal static class NfoFile
{
    private const string Movie = "movie";
    private const string TvShow = "tvshow";
    private const string Season = "season";
    private const string Episode = "episodedetails";

    private const string TitleTag = "title";
    private const string YearTag = "year";
    private const string SeasonNumberTag = "seasonnumber";
    private const string EpisodeTag = "episode";
    private const string EpisodeNumberEnd = "episodenumberend";

    private const string IgnoreFile = ".ignore";
    private const string AnyFile = "*";
    private const string AnyNfoFile = "*.nfo\"";
    private const string SeasonNfoFile = "season.nfo";
    private const string TvShowNfoFile = "tvshow.nfo";

    public static IEnumerable<Either<Error, Medium>> Find(string searchPath, Option<Medium.Show> currentShow = default, Option<Medium.Season> currentSeason = default)
        => File.Exists(Path.Combine(searchPath, IgnoreFile))
            ? Enumerable.Empty<Either<Error, Medium>>()
            : FindInternal(searchPath, currentShow.OrElse(ShowOrNone(searchPath, currentShow, currentSeason)), currentSeason.OrElse(SeasonOrNone(searchPath, currentShow, currentSeason)));


    private static IEnumerable<Either<Error, Medium>> FindInternal(string searchPath, Option<Medium.Show> currentShow, Option<Medium.Season> currentSeason)
        => Enumerable.Empty<Either<Error, Medium>>()
            .Concat(FindNfoFile(searchPath, currentShow, currentSeason))
            .Concat(FindFolder(searchPath, currentShow, currentSeason));

    private static IEnumerable<Either<Error, Medium>> FindFolder(string searchPath, Option<Medium.Show> currentShow, Option<Medium.Season> currentSeason)
        => Directory.EnumerateDirectories(searchPath, AnyFile, SearchOption.TopDirectoryOnly)
            .SelectMany(directory => Find(directory, currentShow, currentSeason));

    private static IEnumerable<Either<Error, Medium>> FindNfoFile(string searchPath, Option<Medium.Show> currentShow, Option<Medium.Season> currentSeason)
        => Directory.EnumerateFiles(searchPath, AnyNfoFile, SearchOption.TopDirectoryOnly)
            .Select(nfoPath => ReadNfoFile(nfoPath, currentShow, currentSeason));

    private static Func<Option<Medium.Season>> SeasonOrNone(string directory, Option<Medium.Show> currentShow, Option<Medium.Season> currentSeason)
        => ()
            => ReadNfoFile(Path.Combine(directory, SeasonNfoFile), currentShow, currentSeason).RightOrNone().SelectMany(medium => medium as Medium.Season ?? Option<Medium.Season>.None);

    private static Func<Option<Medium.Show>> ShowOrNone(string directory, Option<Medium.Show> currentShow, Option<Medium.Season> currentSeason)
        => ()
            => ReadNfoFile(Path.Combine(directory, TvShowNfoFile), currentShow, currentSeason).RightOrNone().SelectMany(medium => medium as Medium.Show ?? Option<Medium.Show>.None);

    private static Either<Error, Medium> ReadNfoFile(string nfoPath, Option<Medium.Show> show, Option<Medium.Season> season)
        => from xml in ToXmlDocument(nfoPath)
           from nfoFile in ToNfoFile(nfoPath, xml, season, show)
           select nfoFile;

    private static Either<Error, XmlDocument> ToXmlDocument(string filename)
    {
        try
        {
            var result = new XmlDocument();

            result.Load(filename);

            return Either<Error, XmlDocument>.Right(result);
        }
        catch (Exception exception)
        {
            return Either<Error, XmlDocument>.Left(new Error.XmlReadFailed(exception.Message, filename));
        }
    }
    private static Either<Error, Medium> ToNfoFile(string filename, XmlDocument xml, Option<Medium.Season> season, Option<Medium.Show> show)
        => GetRootNodeName(xml) switch
        {
            Movie => ReadMovie(filename, xml),
            TvShow => ReadShow(filename, xml),
            Season => ReadSeason(filename, xml, show),
            Episode => ReadEpisode(filename, xml, season),
            _ => Either<Error, Medium>.Left(new Error.UnknownRootNode(GetRootNodeName(xml))),
        };

    private static string GetRootNodeName(XmlNode xml)
        => xml.ChildNodes.Cast<XmlNode>().Last().Name;

    private static Either<Error, Medium> ReadMovie(string filename, XmlDocument xml)
        => from title in GetTitle(filename, xml)
           from year in GetYear(filename, xml)
           select new Medium.Movie(filename, title, year) as Medium;

    private static Either<Error, Medium> ReadShow(string filename, XmlDocument xml)
        => from title in GetTitle(filename, xml)
           from year in GetYear(filename, xml)
           select new Medium.Show(filename, title, year) as Medium;

    private static Either<Error, Medium> ReadSeason(string filename, XmlDocument xml, Option<Medium.Show> maybeShow)
        => from show in maybeShow.ToEither(() => new Error.ShowMissing(filename) as Error)
           from season in GetSeasonNumber(filename, xml)
           select new Medium.Season(filename, show, season) as Medium;


    private static Either<Error, Medium> ReadEpisode(string filename, XmlDocument xml, Option<Medium.Season> maybeSeason)
        => from season in maybeSeason.ToEither(() => new Error.SeasonMissing(filename) as Error)
           from title in GetTitle(filename, xml)
           from episode in GetEpisodeNumber(filename, xml)
           select new Medium.Episode(filename, season, title, episode, GetEpisodeNumberEnd(xml)) as Medium;

    private static Option<int> GetEpisodeNumberEnd(XmlDocument xml)
        => from episodeNumberEndString in GetNameOrNoneByTagName(xml, EpisodeNumberEnd)
           from episodeNumberEnd in episodeNumberEndString.ParseInt32OrNone()
           select episodeNumberEnd;

    private static Either<Error, int> GetSeasonNumber(string filename, XmlDocument xml)
        => from seasonString in GetNameOrMissingByTagName(filename, xml, SeasonNumberTag)
           from seasonNumber in seasonString.ParseInt32OrNone().ToEither(() => new Error.ParseError($"{SeasonNumberTag} is '{seasonString}' which is not an int.") as Error)
           select seasonNumber;

    private static Either<Error, string> GetTitle(string filename, XmlDocument xml)
        => GetNameOrMissingByTagName(filename, xml, TitleTag);

    private static Either<Error, int> GetYear(string filename, XmlDocument xml)
        => from yearString in GetNameOrMissingByTagName(filename, xml, YearTag)
           from year in yearString.ParseInt32OrNone().ToEither(() => new Error.ParseError($"{YearTag} is '{yearString}' which is not an int.") as Error)
           select year;

    private static Either<Error, int> GetEpisodeNumber(string filename, XmlDocument xml)
        => from episodeString in GetNameOrMissingByTagName(filename, xml, EpisodeTag)
           from episodeNumber in episodeString.ParseInt32OrNone().ToEither(() => new Error.ParseError($"{EpisodeTag} is '{episodeString}' which is not an int.") as Error)
           select episodeNumber;

    private static Either<Error, string> GetNameOrMissingByTagName(string filename, XmlDocument xml, string tag)
        => GetNameOrNoneByTagName(xml, tag)
            .ToEither<Error, string>(() => new Error.MissingXmlTag(filename, TitleTag));

    private static Option<string> GetNameOrNoneByTagName(XmlDocument xml, string tag) =>
        xml
            .GetElementsByTagName(tag)
            .Cast<XmlNode>()
            .SingleOrNone()
            .AndThen(node => node.InnerText);
}

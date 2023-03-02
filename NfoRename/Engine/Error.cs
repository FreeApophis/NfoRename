using Funcky;
using Spectre.Console;

namespace NfoRename.Engine;

[DiscriminatedUnion]
internal abstract partial record Error
{
    public partial record XmlReadFailed(string Message, string Path) : Error;
    public partial record UnknownRootNode(string RootNode) : Error;
    public partial record CannotFindNfoFiles(string Message) : Error;
    public partial record WrongName(DirectoryInfo Location, string Expected, string Actual, bool IsFolder) : Error;
    public partial record InformationInNfoMissing(string Parameter) : Error;
    public partial record MissingXmlTag(string File, string Tag) : Error;
    public partial record ParseError(string Message) : Error;
    public partial record ShowMissing(string Filename) : Error;
    public partial record SeasonMissing(string Filename) : Error;
}

internal static class ErrorExtensions
{
    public static Markup ToMarkup(this Error error)
        => error.Match(
            xmlReadFailed => new Markup($"Reading XML at '{StringExtensions.EscapeMarkup(xmlReadFailed.Path)} failed with '{StringExtensions.EscapeMarkup(xmlReadFailed.Message)}'", new Style(Color.Red)),
            unknownRootNode => new Markup($"XML has unknown root node '{StringExtensions.EscapeMarkup(unknownRootNode.RootNode)}'", new Style(Color.Red)),
            cannotFindNfoFiles => new Markup($"Cannot find .nfo file '{StringExtensions.EscapeMarkup(cannotFindNfoFiles.Message)}'", new Style(Color.Red)),
            wrongName => new Markup($"[[[red]ERROR[/]]] The {Target(wrongName.IsFolder)} is not named correctly:\n[[[blue]ACT[/]]]   {StringExtensions.EscapeMarkup(wrongName.Actual)}\n[[[blue]EXP[/]]]   {wrongName.Expected}\n"),
            informationInNfoMissing => new Markup($"The information '{StringExtensions.EscapeMarkup(informationInNfoMissing.Parameter)}' is missing.", new Style(Color.Red)),
            missingXmlTag => new Markup($"The xml tag '{StringExtensions.EscapeMarkup(missingXmlTag.Tag)}' is missing in '{missingXmlTag.File}'", new Style(Color.Red)),
            parseError => new Markup(StringExtensions.EscapeMarkup(parseError.Message), new Style(Color.Red)),
            showMissing => new Markup($"[red][blue]tvshow.nfo[/] is missing for {showMissing.Filename}.[/]\n"),
            seasonMissing => new Markup($"[red][blue]season.nfo[/] is missing for {seasonMissing.Filename}.[/]\n"));

    private static string Target(bool isFolder)
        => isFolder
            ? "folder"
            : "file";
}
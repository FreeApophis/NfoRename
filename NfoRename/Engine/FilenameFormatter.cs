using System.Collections.Immutable;
using System.Linq;
using Spectre.Console;

namespace NfoRename.Engine;

internal class FilenameFormatter
{
    private readonly IReadOnlyDictionary<string, string> _stringReplacements;

    public FilenameFormatter(FormatterRules formatterRules)
    {
        _stringReplacements =
            DefaultStringReplacements()
                .Aggregate(formatterRules.StringReplacements.ToImmutableDictionary(), AppendNecessaryDefault);
    }

    private static ImmutableDictionary<string, string> AppendNecessaryDefault(ImmutableDictionary<string, string> replacements, KeyValuePair<string, string> defaultReplacement)
        => replacements.ContainsKey(defaultReplacement.Key)
            ? replacements
            : replacements.Add(defaultReplacement.Key, defaultReplacement.Value);

    private static ImmutableDictionary<string, string> DefaultStringReplacements()
        => Path.GetInvalidFileNameChars()
            .ToImmutableDictionary(char.ToString, _ => string.Empty);

    public string ToLegalFilename(string filename)
    {
        if (filename != ToLegalFilenameInternal(filename))
        {
            var highlighted = _stringReplacements
                .Aggregate(filename, ReplaceAndHighlight)
                .Replace("end-of-highlight", "/");

            AnsiConsole.MarkupLine($"Changes were applied to the filename: [gray]{filename}[/] to [blue]{highlighted}[/]");
        }

        return ToLegalFilenameInternal(filename);
    }
    public string ToLegalFilenameInternal(string filename) =>
        _stringReplacements
            .Aggregate(filename, Replace);


    private static string Replace(string name, KeyValuePair<string, string> replacement)
        => name.Replace(replacement.Key, replacement.Value);

    private static string ReplaceAndHighlight(string name, KeyValuePair<string, string> replacement)
        => replacement.Value is ""
            ? name.Replace(replacement.Key, $"[red]{replacement.Key}[end-of-highlight]")
            : name.Replace(replacement.Key, $"[red]{replacement.Key}[end-of-highlight][green]{replacement.Value}[end-of-highlight]");
}
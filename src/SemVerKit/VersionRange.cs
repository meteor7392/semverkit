using System.Text.RegularExpressions;

namespace SemVerKit;

public sealed class VersionRange
{
    private readonly Func<SemanticVersion, bool> _predicate;

    public VersionRange(string? range)
    {
        _predicate = Parse(range);
    }

    public bool IsSatisfiedBy(SemanticVersion version) => _predicate(version);

    public static bool IsSatisfiedBy(string range, SemanticVersion version) =>
        new VersionRange(range).IsSatisfiedBy(version);

    private static Func<SemanticVersion, bool> Parse(string? range)
    {
        var text = range?.Trim() ?? string.Empty;
        if (text == string.Empty || text == "*")
        {
            return _ => true;
        }

        var alternatives = text.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
        var predicates = new List<Func<SemanticVersion, bool>>();

        foreach (var alternative in alternatives)
        {
            var trimmed = alternative.Trim();
            if (trimmed.Length > 0)
            {
                predicates.Add(ParseAlternative(trimmed));
            }
        }

        if (predicates.Count == 0)
        {
            return _ => true;
        }

        return version => predicates.Any(predicate => predicate(version));
    }

    private static Func<SemanticVersion, bool> ParseAlternative(string alternative)
    {
        var hyphen = Regex.Match(alternative, @"^(?<low>.+?)\s+-\s+(?<high>.+?)$");
        if (hyphen.Success)
        {
            var low = ParsePartial(hyphen.Groups["low"].Value.Trim());
            var high = ParsePartial(hyphen.Groups["high"].Value.Trim());
            return version => version >= low && version <= high;
        }

        var tokens = alternative.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return _ => true;
        }

        var tokenPredicates = tokens.Select(ParseToken).ToArray();
        return version => tokenPredicates.All(predicate => predicate(version));
    }

    private static Func<SemanticVersion, bool> ParseToken(string token)
    {
        token = token.Trim();

        if (token.StartsWith(">="))
        {
            var version = ParsePartial(token.Substring(2));
            return candidate => candidate >= version;
        }

        if (token.StartsWith("<="))
        {
            var version = ParsePartial(token.Substring(2));
            return candidate => candidate <= version;
        }

        if (token.StartsWith(">"))
        {
            var version = ParsePartial(token.Substring(1));
            return candidate => candidate > version;
        }

        if (token.StartsWith("<"))
        {
            var version = ParsePartial(token.Substring(1));
            return candidate => candidate < version;
        }

        if (token.StartsWith("="))
        {
            var version = ParsePartial(token.Substring(1));
            return candidate => candidate.CompareTo(version) == 0;
        }

        if (token.StartsWith("^"))
        {
            return Caret(token.Substring(1));
        }

        if (token.StartsWith("~"))
        {
            return Tilde(token.Substring(1));
        }

        if (TryParsePartial(token, out var version, out var components))
        {
            if (components == 3)
            {
                return candidate => candidate.CompareTo(version!) == 0;
            }

            return Caret(token);
        }

        throw new FormatException($"Invalid version range token: '{token}'.");
    }

    private static SemanticVersion ParsePartial(string input)
    {
        if (!TryParsePartial(input, out var version, out _) || version is null)
        {
            throw new FormatException($"Invalid semantic version: '{input}'.");
        }

        return version;
    }

    private static bool TryParsePartial(string input, out SemanticVersion? version, out int components)
    {
        version = null;
        components = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();
        var buildMetadata = string.Empty;
        var plusIndex = text.IndexOf('+');
        if (plusIndex >= 0)
        {
            buildMetadata = text.Substring(plusIndex + 1);
            text = text.Substring(0, plusIndex);
        }

        var preRelease = string.Empty;
        var dashIndex = text.IndexOf('-');
        if (dashIndex >= 0)
        {
            preRelease = text.Substring(dashIndex + 1);
            text = text.Substring(0, dashIndex);
        }

        var core = text.Split('.');
        if (core.Length < 1 || core.Length > 3)
        {
            return false;
        }

        foreach (var part in core)
        {
            if (!SemanticVersion.IsNonNegativeInteger(part))
            {
                return false;
            }
        }

        components = core.Length;
        var padded = core.Concat(Enumerable.Repeat("0", 3 - components)).ToArray();
        var fullVersion = string.Join(".", padded);

        if (!string.IsNullOrEmpty(preRelease))
        {
            fullVersion += "-" + preRelease;
        }

        if (!string.IsNullOrEmpty(buildMetadata))
        {
            fullVersion += "+" + buildMetadata;
        }

        return SemanticVersion.TryParse(fullVersion, out version);
    }

    private static (SemanticVersion Version, int Components) ParsePartialComponents(string input)
    {
        if (!TryParsePartial(input, out var version, out var components) || version is null)
        {
            throw new FormatException($"Invalid semantic version: '{input}'.");
        }

        return (version, components);
    }

    private static Func<SemanticVersion, bool> Caret(string input)
    {
        var (version, components) = ParsePartialComponents(input);

        SemanticVersion upper;
        if (components == 1 || version.Major > 0)
        {
            upper = new SemanticVersion(version.Major + 1, 0, 0);
        }
        else if (components == 2 || version.Minor > 0)
        {
            upper = new SemanticVersion(0, version.Minor + 1, 0);
        }
        else
        {
            upper = new SemanticVersion(0, 0, version.Patch + 1);
        }

        return candidate => candidate >= version && candidate < upper;
    }

    private static Func<SemanticVersion, bool> Tilde(string input)
    {
        var (version, components) = ParsePartialComponents(input);

        SemanticVersion upper;
        if (components <= 1)
        {
            upper = new SemanticVersion(version.Major + 1, 0, 0);
        }
        else
        {
            upper = new SemanticVersion(version.Major, version.Minor + 1, 0);
        }

        return candidate => candidate >= version && candidate < upper;
    }
}

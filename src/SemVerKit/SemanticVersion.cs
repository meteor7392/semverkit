using System.Numerics;

namespace SemVerKit;

public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> PreRelease { get; }
    public string BuildMetadata { get; }

    public SemanticVersion(
        int major,
        int minor,
        int patch,
        IEnumerable<string>? preRelease = null,
        string? buildMetadata = null)
    {
        if (major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), "Version components must be non-negative.");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease is null ? Array.Empty<string>() : preRelease.ToArray();
        BuildMetadata = buildMetadata ?? string.Empty;
    }

    public static bool TryParse(string? input, out SemanticVersion? version)
    {
        version = null;
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
            if (string.IsNullOrEmpty(buildMetadata) || !IsValidBuildMetadata(buildMetadata))
            {
                return false;
            }
        }

        var preReleaseText = string.Empty;
        var dashIndex = text.IndexOf('-');
        if (dashIndex >= 0)
        {
            preReleaseText = text.Substring(dashIndex + 1);
            text = text.Substring(0, dashIndex);
            if (string.IsNullOrEmpty(preReleaseText))
            {
                return false;
            }
        }

        var core = text.Split('.');
        if (core.Length != 3)
        {
            return false;
        }

        if (!IsNonNegativeInteger(core[0]) || !IsNonNegativeInteger(core[1]) || !IsNonNegativeInteger(core[2]))
        {
            return false;
        }

        if (!int.TryParse(core[0], out var major) ||
            !int.TryParse(core[1], out var minor) ||
            !int.TryParse(core[2], out var patch))
        {
            return false;
        }

        var preRelease = new List<string>();
        if (!string.IsNullOrEmpty(preReleaseText))
        {
            foreach (var identifier in preReleaseText.Split('.'))
            {
                if (string.IsNullOrEmpty(identifier) || !IsValidPreReleaseIdentifier(identifier))
                {
                    return false;
                }

                preRelease.Add(identifier);
            }
        }

        version = new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
        return true;
    }

    public static SemanticVersion Parse(string input)
    {
        if (!TryParse(input, out var version) || version is null)
        {
            throw new FormatException($"Invalid semantic version: '{input}'.");
        }

        return version;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;

        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;

        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        var thisHasPreRelease = PreRelease.Count > 0;
        var otherHasPreRelease = other.PreRelease.Count > 0;

        if (!thisHasPreRelease && otherHasPreRelease)
        {
            return 1;
        }

        if (thisHasPreRelease && !otherHasPreRelease)
        {
            return -1;
        }

        if (!thisHasPreRelease)
        {
            return 0;
        }

        var count = Math.Max(PreRelease.Count, other.PreRelease.Count);
        for (var i = 0; i < count; i++)
        {
            var thisHas = i < PreRelease.Count;
            var otherHas = i < other.PreRelease.Count;

            if (!thisHas && otherHas)
            {
                return -1;
            }

            if (thisHas && !otherHas)
            {
                return 1;
            }

            if (!thisHas)
            {
                break;
            }

            var left = PreRelease[i];
            var right = other.PreRelease[i];
            var leftNumeric = left.All(c => c >= '0' && c <= '9');
            var rightNumeric = right.All(c => c >= '0' && c <= '9');

            if (leftNumeric && rightNumeric)
            {
                if (BigInteger.TryParse(left, out var leftNumber) && BigInteger.TryParse(right, out var rightNumber))
                {
                    result = leftNumber.CompareTo(rightNumber);
                    if (result != 0) return result;
                }
                else
                {
                    result = string.CompareOrdinal(left, right);
                    if (result != 0) return result;
                }
            }
            else if (leftNumeric != rightNumeric)
            {
                return leftNumeric ? -1 : 1;
            }
            else
            {
                result = string.CompareOrdinal(left, right);
                if (result != 0) return result;
            }
        }

        return 0;
    }

    public bool Equals(SemanticVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, string.Join(".", PreRelease));

    public override string ToString()
    {
        var value = $"{Major}.{Minor}.{Patch}";
        if (PreRelease.Count > 0)
        {
            value += "-" + string.Join(".", PreRelease);
        }

        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            value += "+" + BuildMetadata;
        }

        return value;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => !(left == right);

    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;

    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;

    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    internal static bool IsNonNegativeInteger(string value) =>
        !string.IsNullOrEmpty(value) && System.Text.RegularExpressions.Regex.IsMatch(value, "^(0|[1-9][0-9]*)$");

    private static bool IsValidPreReleaseIdentifier(string value) =>
        !string.IsNullOrEmpty(value) &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') &&
        (!value.All(c => c >= '0' && c <= '9') || value == "0" || value[0] != '0');

    private static bool IsValidBuildMetadata(string value) =>
        !string.IsNullOrEmpty(value) &&
        value.Split('.').All(part =>
            !string.IsNullOrEmpty(part) && part.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
}

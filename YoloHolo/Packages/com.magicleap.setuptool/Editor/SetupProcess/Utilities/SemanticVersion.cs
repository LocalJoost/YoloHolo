namespace MagicLeap.SetupTool.Editor.Utilities
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class SemanticVersion : IComparable<SemanticVersion>
{
    private static readonly Regex SemVerRegex = new Regex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.Compiled);

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string PreRelease { get; }
    public string BuildMetadata { get; }

    public SemanticVersion(int major, int minor, int patch, string preRelease = null, string buildMetadata = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
    }

    public static SemanticVersion Parse(string version)
    {
        var match = SemVerRegex.Match(version);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid semantic version string", nameof(version));
        }

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var patch = int.Parse(match.Groups[3].Value);
        var preRelease = match.Groups[4].Value;
        var buildMetadata = match.Groups[5].Value;

        return new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
    }

    public int CompareTo(SemanticVersion other)
    {
        if (other == null) return 1;

        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;

        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;

        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        result = ComparePreRelease(PreRelease, other.PreRelease);
        return result;
    }

    private static int ComparePreRelease(string preRelease1, string preRelease2)
    {
        if (string.IsNullOrEmpty(preRelease1) && string.IsNullOrEmpty(preRelease2)) return 0;
        if (string.IsNullOrEmpty(preRelease1)) return 1;
        if (string.IsNullOrEmpty(preRelease2)) return -1;

        var identifiers1 = preRelease1.Split('.');
        var identifiers2 = preRelease2.Split('.');

        var length = Math.Min(identifiers1.Length, identifiers2.Length);
        for (var i = 0; i < length; i++)
        {
            var result = CompareIdentifiers(identifiers1[i], identifiers2[i]);
            if (result != 0) return result;
        }

        return identifiers1.Length.CompareTo(identifiers2.Length);
    }

    private static int CompareIdentifiers(string id1, string id2)
    {
        var numeric1 = int.TryParse(id1, out var num1);
        var numeric2 = int.TryParse(id2, out var num2);

        if (numeric1 && numeric2) return num1.CompareTo(num2);
        if (numeric1) return -1;
        if (numeric2) return 1;

        return string.Compare(id1, id2, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease)) version += $"-{PreRelease}";
        if (!string.IsNullOrEmpty(BuildMetadata)) version += $"+{BuildMetadata}";
        return version;
    }
}
}
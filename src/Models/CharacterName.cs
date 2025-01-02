using Scop;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace Scop.Models;

public class CharacterName : IEquatable<CharacterName>
{
    [JsonIgnore]
    public string? GivenName => GivenNames is null ? null : string.Join(' ', GivenNames);

    public List<string>? GivenNames { get; set; }

    public bool HasDoubleSurname { get; set; }

    public bool HasHyphenatedSurname { get; set; }

    public bool HasMatronymicSurname { get; set; }

    [JsonIgnore]
    public bool IsEmpty => (GivenNames is null || GivenNames.Count == 0)
        && (MiddleNames is null || MiddleNames.Count == 0)
        && (Suffixes is null || Suffixes.Count == 0)
        && (Surnames is null || Surnames.Count == 0)
        && string.IsNullOrWhiteSpace(Title);

    [JsonIgnore]
    public bool IsDefault => !HasDoubleSurname
        && !HasHyphenatedSurname
        && !HasMatronymicSurname
        && IsEmpty;

    [JsonIgnore]
    public string? MiddleName => MiddleNames is null ? null : string.Join(' ', MiddleNames);

    public List<string>? MiddleNames { get; set; }

    [JsonIgnore]
    public int PartCount => (GivenNames?.Count ?? 0)
        + (MiddleNames?.Count ?? 0)
        + (Surnames?.Count ?? 0)
        + (Suffixes?.Count ?? 0)
        + (string.IsNullOrWhiteSpace(Title) ? 0 : 1);

    [JsonIgnore]
    public string? Suffix => Suffixes is null ? null : string.Join(' ', Suffixes);

    public List<string>? Suffixes { get; set; }

    public List<Surname>? Surnames { get; set; }

    public string? Title { get; set; }

    /// <inheritdoc />
    public bool Equals(CharacterName? other)
    {
        if (other is null
            || HasDoubleSurname != other.HasDoubleSurname
            || HasHyphenatedSurname != other.HasHyphenatedSurname
            || HasMatronymicSurname != other.HasMatronymicSurname)
        {
            return false;
        }

        return IsMatchingName(other);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CharacterName);

    public override int GetHashCode() => HashCode.Combine(
        GivenNames,
        HasDoubleSurname,
        HasHyphenatedSurname,
        HasMatronymicSurname,
        MiddleNames,
        Suffixes,
        Surnames,
        Title);

    public float GetMatchScore(CharacterName? other)
    {
        if (other is null)
        {
            return 0f;
        }

        if (IsMatchingName(other))
        {
            return HasDoubleSurname == other.HasDoubleSurname
                && HasHyphenatedSurname == other.HasHyphenatedSurname
                && HasMatronymicSurname == other.HasMatronymicSurname
                ? float.PositiveInfinity
                : float.MaxValue;
        }

        var length = other.PartCount;
        var matches = 0.0f;
        var titleMatch = false;

        if (!string.IsNullOrWhiteSpace(Title)
            && string.Equals(other.Title, Title, StringComparison.OrdinalIgnoreCase))
        {
            matches = 1.0f;
            titleMatch = true;
        }

        if (GivenNames?.Count > 0)
        {
            foreach (var name in GivenNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                if (other.GivenNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches++;
                }
                else if (other.MiddleNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.5f;
                }
                else if (other.Surnames?.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    matches += 0.5f;
                }
                else if (other.Suffixes?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.25f;
                }
                else if (!titleMatch
                    && string.Equals(other.Title, name, StringComparison.OrdinalIgnoreCase))
                {
                    matches += 0.25f;
                    titleMatch = true;
                }
            }
        }

        if (MiddleNames?.Count > 0)
        {
            foreach (var name in MiddleNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                if (other.MiddleNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches++;
                }
                else if (other.GivenNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.5f;
                }
                else if (other.Surnames?.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    matches += 0.5f;
                }
                else if (other.Suffixes?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.25f;
                }
                else if (!titleMatch
                    && string.Equals(other.Title, name, StringComparison.OrdinalIgnoreCase))
                {
                    matches += 0.25f;
                    titleMatch = true;
                }
            }
        }

        if (Surnames?.Count > 0)
        {
            foreach (var name in Surnames)
            {
                if (string.IsNullOrWhiteSpace(name.Name))
                {
                    continue;
                }
                if (other.Surnames?.Any(x => string.Equals(x.Name, name.Name, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    matches++;
                }
                else if (other.GivenNames?.Contains(name.Name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.5f;
                }
                else if (other.MiddleNames?.Contains(name.Name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.5f;
                }
                else if (other.Suffixes?.Contains(name.Name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.25f;
                }
                else if (!titleMatch
                    && string.Equals(other.Title, name.Name, StringComparison.OrdinalIgnoreCase))
                {
                    matches += 0.25f;
                    titleMatch = true;
                }
            }
        }

        if (Suffixes?.Count > 0)
        {
            foreach (var name in Suffixes)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                if (other.Suffixes?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches++;
                }
                else if (other.GivenNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.25f;
                }
                else if (other.MiddleNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true)
                {
                    matches += 0.25f;
                }
                else if (other.Surnames?.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    matches += 0.25f;
                }
                else if (!titleMatch
                    && string.Equals(other.Title, name, StringComparison.OrdinalIgnoreCase))
                {
                    matches += 0.25f;
                    titleMatch = true;
                }
            }
        }

        return Math.Min(matches, length) / length;
    }

    public bool IsMatchingName(CharacterName other)
    {
        if (!string.Equals(Title, other.Title, StringComparison.Ordinal)
            || GivenNames?.Count != other.GivenNames?.Count
            || MiddleNames?.Count != other.MiddleNames?.Count
            || Suffixes?.Count != other.Suffixes?.Count
            || Surnames?.Count != other.Surnames?.Count)
        {
            return false;
        }

        if (GivenNames?.Count > 0)
        {
            var lookup = GivenNames.ToLookup(x => x);
            var otherLookup = other.GivenNames!.ToLookup(x => x);
            if (lookup.Count != otherLookup.Count
                || lookup.Any(x => x.Count() != otherLookup[x.Key].Count()))
            {
                return false;
            }
        }

        if (MiddleNames?.Count > 0)
        {
            var lookup = MiddleNames.ToLookup(x => x);
            var otherLookup = other.MiddleNames!.ToLookup(x => x);
            if (lookup.Count != otherLookup.Count
                || lookup.Any(x => x.Count() != otherLookup[x.Key].Count()))
            {
                return false;
            }
        }

        if (Suffixes?.Count > 0)
        {
            var lookup = Suffixes.ToLookup(x => x);
            var otherLookup = other.Suffixes!.ToLookup(x => x);
            if (lookup.Count != otherLookup.Count
                || lookup.Any(x => x.Count() != otherLookup[x.Key].Count()))
            {
                return false;
            }
        }

        if (Surnames?.Count > 0)
        {
            var lookup = Surnames.ToLookup(x => x);
            var otherLookup = other.Surnames!.ToLookup(x => x);
            if (lookup.Count != otherLookup.Count
                || lookup.Any(x => x.Count() != otherLookup[x.Key].Count()))
            {
                return false;
            }
        }

        return true;
    }

    public string ToShortName()
    {
        var sb = new StringBuilder();

        if (GivenNames?.Count > 0)
        {
            sb.Append(GivenNames.First());
        }

        AppendSurname(sb);

        if (Suffixes?.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.Append(Suffixes.First());
        }

        return sb.ToString();
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out CharacterName? name)
    {
        name = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        name = new();

        var tokens = value.Split(' ');

        var index = 0;
        var title = tokens[0].Trim(',');
        if (Strings.Titles.Contains(title, StringComparer.OrdinalIgnoreCase))
        {
            name.Title = title;
            index++;
        }

        var lastIndex = tokens.Length - 1;
        for (; lastIndex >= index; lastIndex--)
        {
            var suffix = tokens[lastIndex].Trim(',');
            if (!Strings.Suffixes.Contains(suffix, StringComparer.OrdinalIgnoreCase))
            {
                break;
            }
            (name.Suffixes ??= []).Insert(0, suffix);
        }

        if (!tokens[index].Contains('-'))
        {
            (name.GivenNames ??= []).Add(tokens[index].Trim(','));
            index++;
        }

        for (; index < lastIndex; index++)
        {
            if (tokens[index].Contains('-'))
            {
                break;
            }
            (name.MiddleNames ??= []).Add(tokens[index].Trim(','));
        }

        for (; index <= lastIndex; index++)
        {
            var nameTokens = tokens[index].Split('-');
            for (var i = 0; i < nameTokens.Length; i++)
            {
                (name.Surnames ??= []).Add(new(
                    nameTokens[i].Trim(','),
                    (i > 0 && i < nameTokens.Length - 1)
                        || (nameTokens.Length == 1 && name.Surnames?.Count > 0 && index < lastIndex),
                    (i > 0 && i == nameTokens.Length - 1)
                        || (nameTokens.Length == 1 && name.Surnames?.Count > 0 && index == lastIndex)));
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(Title);

        if (GivenNames?.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.AppendJoin(' ', GivenNames);
        }

        if (MiddleNames?.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.AppendJoin(' ', MiddleNames);
        }

        AppendSurname(sb);

        if (Suffixes?.Count > 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            sb.AppendJoin(' ', Suffixes);
        }

        return sb.ToString();
    }

    private void AppendSurname(StringBuilder sb)
    {
        if (Surnames is null || Surnames.Count == 0)
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        sb.AppendJoin(
            HasHyphenatedSurname ? '-' : ' ',
            (Surnames.Any(x => x.IsSpousal)
                ? Surnames.Where(x => x.IsSpousal || ((HasDoubleSurname || HasHyphenatedSurname) && x.IsMatronymic == HasMatronymicSurname))
                : Surnames.Where(x => HasDoubleSurname || HasHyphenatedSurname || x.IsMatronymic == HasMatronymicSurname))
                .OrderByDescending(x => x.IsSpousal)
                .Select(x => x.Name));
    }

    public static bool operator ==(CharacterName? left, CharacterName? right) => EqualityComparer<CharacterName>.Default.Equals(left, right);
    public static bool operator !=(CharacterName? left, CharacterName? right) => !(left == right);
}

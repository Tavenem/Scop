using Scop.Enums;
using Tavenem.Mathematics;

namespace Scop.Models;

/// <summary>
/// A type of relationship.
/// </summary>
/// <param name="Name">
/// <para>
/// The name for the relative's relationship to the associated character, if it is not gendered.
/// </para>
/// <para>
/// Also used as the display name for the type itself.
/// </para>
/// <para>
/// "{Relative} is this character's &lt;x>"
/// </para>
/// </param>
/// <param name="InverseName">
/// <para>
/// The name for the associated character's relationship to the relative, if it is not gendered.
/// </para>
/// <para>
/// Should match the <see cref="Name"/> of the inverse type, if defined.
/// </para>
/// <para>
/// "This character is {relative}'s &lt;x>"
/// </para>
/// <para>
/// May be omitted if it is the same as <see cref="Name"/>.
/// </para>
/// </param>
/// <param name="Names">
/// The names for the relative's relationship to the associated character, when the term varies by
/// gender.
/// </param>
/// <param name="AgeGap">
/// <para>
/// The typical age difference between the relative and the associated character, as a range of
/// minimum and maximum differences, along with the mean difference.
/// </para>
/// <para>
/// Any value may be negative, to indicate that the relative is younger.
/// </para>
/// <para>
/// Any value may be <see langword="null"/>.
/// </para>
/// <para>
/// If no set of values is found for a given <see cref="NameGender"/>, the values for <see
/// cref="NameGender.None"/> will be used (if present).
/// </para>
/// <para>
/// If no values are provided at all, an <see cref="AgeGap.Mean"/> of zero is presumed, with no <see
/// cref="AgeGap.Min"/> or <see cref="AgeGap.Max"/>.
/// </para>
/// </param>
public record RelationshipType(
    string? Name,
    string? InverseName = null,
    Dictionary<NameGender, string>? Names = null,
    Dictionary<NameGender, AgeGap>? AgeGap = null)
{
    private const string ExPrefix = "ex-";

    public static RelationshipType? GetRelationshipType(ScopData data, string? value)
    {
        if (string.IsNullOrEmpty(value)
            || data.RelationshipTypes is null)
        {
            return null;
        }

        if (data
            .RelationshipTypes
            .FirstOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase))
            is RelationshipType matchingType)
        {
            return matchingType;
        }

        foreach (var type in data.RelationshipTypes)
        {
            foreach (var gender in Enum.GetValues<NameGender>())
            {
                if ((type.Names?.TryGetValue(gender, out var name) == true
                    ? name
                    : type.Name)
                    is string variation
                    && string.Equals(variation, value, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }
        }

        const string TrimChars = "- ";
        var generations = 0;
        var valueSpan = value.AsSpan();
        while (valueSpan.StartsWith("great", StringComparison.OrdinalIgnoreCase)
            && valueSpan.Length > 5)
        {
            valueSpan = valueSpan[5..].TrimStart(TrimChars);
            generations++;
        }
        if (valueSpan.StartsWith("grand", StringComparison.OrdinalIgnoreCase)
            && valueSpan.Length > 5)
        {
            valueSpan = valueSpan[5..].TrimStart(TrimChars);
            generations++;
        }

        var hasPrefix = false;
        while ((valueSpan.StartsWith("half", StringComparison.OrdinalIgnoreCase)
            || valueSpan.StartsWith("step", StringComparison.OrdinalIgnoreCase))
            && valueSpan.Length > 4)
        {
            valueSpan = valueSpan[4..].TrimStart(TrimChars);
            hasPrefix = true;
        }
        if (valueSpan.StartsWith("ex", StringComparison.OrdinalIgnoreCase)
            && valueSpan.Length > 2)
        {
            valueSpan = valueSpan[2..].TrimStart(TrimChars);
            hasPrefix = true;
        }

        var prefix = generations > 0 || hasPrefix
            ? value[..(value.Length - valueSpan.Length)]
            : null;

        var length = valueSpan.Length;
        var inlaw = false;
        if (valueSpan.EndsWith("in-law", StringComparison.OrdinalIgnoreCase)
            || valueSpan.EndsWith("in law", StringComparison.OrdinalIgnoreCase))
        {
            valueSpan = valueSpan[6..].TrimEnd(TrimChars);
            inlaw = true;
        }
        else if (valueSpan.EndsWith("inlaw", StringComparison.OrdinalIgnoreCase))
        {
            valueSpan = valueSpan[5..].TrimEnd(TrimChars);
            inlaw = true;
        }
        if (generations == 0
            && !hasPrefix
            && !inlaw)
        {
            return null;
        }

        var suffix = inlaw
            ? value[^(value.Length - valueSpan.Length)..]
            : null;

        RelationshipType? baseType = null;
        foreach (var type in data.RelationshipTypes)
        {
            if (valueSpan.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
            {
                baseType = type;
                break;
            }

            foreach (var gender in Enum.GetValues<NameGender>())
            {
                if ((type.Names?.TryGetValue(gender, out var name) == true
                    ? name
                    : type.Name)
                    is string variation
                    && valueSpan.Equals(variation, StringComparison.OrdinalIgnoreCase))
                {
                    baseType = type;
                    break;
                }
            }
            if (baseType is not null)
            {
                break;
            }
        }
        if (baseType is null)
        {
            return null;
        }

        return new RelationshipType(
            baseType.Name is null ? null : prefix + baseType.Name + suffix,
            baseType.InverseName is null ? null : prefix + baseType.InverseName + suffix,
            baseType.Names?.ToDictionary(x => x.Key, x => prefix + x.Value + suffix),
            baseType.AgeGap?.ToDictionary(
                x => x.Key,
                x => new AgeGap(
                    x.Value.Min.HasValue ? (generations * 16).CopySign(x.Value.Min.Value) + x.Value.Min.Value : null,
                    x.Value.Mean.HasValue ? (generations * 22).CopySign(x.Value.Mean.Value) + x.Value.Mean.Value : null,
                    x.Value.Max.HasValue ? (generations * 42).CopySign(x.Value.Max.Value) + x.Value.Max.Value : null)));
    }

    /// <inheritdoc />
    public virtual bool Equals(RelationshipType? other)
        => other is RelationshipType value
        && string.Equals(value.Name, Name, StringComparison.Ordinal);

    /// <summary>
    /// Gets a type equal to this one, but with "ex-" prefixed to all names.
    /// </summary>
    public RelationshipType GetExType() => new(
        ExPrefix + Name,
        InverseName is null ? null : ExPrefix + InverseName,
        Names?.ToDictionary(
            x => x.Key,
            x => ExPrefix + x.Value),
        AgeGap?.ToDictionary(
            x => x.Key,
            x => x.Value));

    /// <inheritdoc />
    public override int GetHashCode() => Name?.GetHashCode() ?? 0;

    /// <inheritdoc />
    public override string? ToString() => Name?.ToString();
}

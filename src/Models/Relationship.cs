using Scop.Enums;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Scop.Models;

public partial class Relationship : IEquatable<Relationship>, IJsonOnDeserialized
{
    [JsonIgnore] public string? EditedInverseType { get; set; }

    [JsonIgnore] public Character? EditedRelative { get; set; }

    [JsonIgnore] public NameGender EditedRelativeGender { get; set; }

    [JsonIgnore] public RelationshipType? EditedRelationshipType { get; set; }

    [JsonIgnore] public string? EditedRelativeName { get; set; }

    [JsonIgnore] public string? EditedType { get; set; }

    [Obsolete("Use RelativeId")]
    public string? Id { get; set; }

    /// <summary>
    /// The inverse of this relationship.
    /// </summary>
    [JsonIgnore] public Relationship? Inverse { get; set; }

    /// <summary>
    /// The name for the associated character's relationship to the relative, if it is not modeled
    /// as a <see cref="Models.RelationshipType"/>.
    /// </summary>
    /// <remarks>
    /// "This character is {relative}'s &lt;x>"
    /// </remarks>
    public string? InverseType { get; set; }

    [JsonIgnore] public bool IsEditing { get; set; }

    [Obsolete("Use Type")]
    public string? RelationshipName { get; set; }

    /// <summary>
    /// The type of relationship the relative has with the associated character.
    /// </summary>
    /// <remarks>
    /// "{Relative} is this character's &lt;x>"
    /// </remarks>
    [JsonIgnore] public RelationshipType? RelationshipType { get; set; }

    /// <summary>
    /// The relative.
    /// </summary>
    [JsonIgnore] public Character? Relative { get; set; }

    /// <summary>
    /// The <see cref="Character.Id"/> of the relative, or a hand-written relative name when the
    /// relative is not modeled.
    /// </summary>
    public string? RelativeId { get; set; }

    [Obsolete("Use RelativeId")]
    public string? RelativeName { get; set; }

    /// <summary>
    /// The gender of the relative, when it is not modeled.
    /// </summary>
    /// <remarks>
    /// The value should be considered non-authoritative when <see cref="Relative"/> is non-null.
    /// </remarks>
    public NameGender RelativeGender { get; set; }

    [JsonIgnore] public bool Synthetic { get; set; }

    /// <summary>
    /// The name for the relative's relationship to the associated character, if it is not modeled
    /// as a <see cref="Models.RelationshipType"/>.
    /// </summary>
    /// <remarks>
    /// "{Relative} is this character's &lt;x>"
    /// </remarks>
    public string? Type { get; set; }

    public static Relationship FromType(
        ScopData data,
        RelationshipType type,
        Character relative,
        Character? character = null,
        bool synthetic = false)
    {
        var result = new Relationship
        {
            EditedRelationshipType = type,
            EditedRelative = relative,
            RelationshipType = type,
            Relative = relative,
            RelativeId = relative.Id,
            RelativeGender = relative.GetNameGender(),
            Synthetic = synthetic,
            Type = type.Name,
        };
        result.EditedRelativeGender = result.RelativeGender;
        result.EditedRelativeName = relative.CharacterShortName;
        result.EditedType = result.GetRelationshipTypeName();
        result.Inverse = result.GetInverseRelationship(data, character);
        result.InverseType = result.Inverse.Type;
        result.EditedInverseType = result.Inverse.GetRelationshipTypeName();
        return result;
    }

    public static IEnumerable<Relationship> GetAdjustedRelationships(ScopData data, Character character, Relationship relationship, List<Relationship>? relationships)
    {
        if (relationships is null)
        {
            yield break;
        }

        foreach (var relativeRelationship in relationships)
        {
            if (string.Equals(relativeRelationship.RelativeId, character.Id, StringComparison.Ordinal))
            {
                continue;
            }
            var newRelationship = relationship.Type switch
            {
                null => null,
                "child" => relativeRelationship.GetForChild(data, character),
                "parent" => relativeRelationship.GetForParent(data, character),
                "pibling" => relativeRelationship.GetForPibling(data, character),
                "sibling" => relativeRelationship.GetForSibling(data, character),
                "spouse" => relativeRelationship.GetForSpouse(data, character),
                var x when x.EndsWith("child", StringComparison.OrdinalIgnoreCase)
                    => relativeRelationship.GetForGrandchild(data, character),
                var x when x.EndsWith("parent", StringComparison.OrdinalIgnoreCase)
                    => relativeRelationship.GetForGrandparent(data, character),
                _ => null,
            };

            if (newRelationship is not null)
            {
                yield return newRelationship;
            }
        }
    }

    public Relationship Clone() => new()
    {
        EditedInverseType = EditedInverseType,
        EditedRelativeGender = EditedRelativeGender,
        EditedRelationshipType = EditedRelationshipType,
        EditedRelative = EditedRelative,
        EditedType = EditedType,
        EditedRelativeName = EditedRelativeName,
        Inverse = Inverse,
        InverseType = InverseType,
        RelationshipType = RelationshipType,
        Relative = Relative,
        RelativeGender = RelativeGender,
        RelativeId = RelativeId,
        Synthetic = Synthetic,
        Type = Type,
    };

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Relationship);

    /// <inheritdoc />
    public bool Equals(Relationship? other) => other is not null
        && string.Equals(other.RelativeId, RelativeId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(other.Type, Type, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(RelativeId, Type);

    public Relationship GetInverseRelationship(ScopData data, Character? character)
    {
        if (Inverse is not null)
        {
            return Inverse;
        }

        var inverseTypeName = EditedInverseType
            ?? RelationshipType?.InverseName
            ?? InverseType
            ?? EditedType
            ?? RelationshipType?.Name
            ?? Type;

        var inverseType = RelationshipType.GetRelationshipType(data, inverseTypeName);
        var gender = character?.GetNameGender() ?? NameGender.None;

        var inverse = new Relationship
        {
            EditedRelationshipType = inverseType,
            EditedRelative = character,
            EditedRelativeGender = gender,
            EditedRelativeName = character?.DisplayName,
            Inverse = this,
            InverseType = EditedType
                ?? RelationshipType?.Name
                ?? Type,
            RelationshipType = inverseType,
            Relative = character,
            RelativeId = character?.Id,
            RelativeGender = gender,
            Synthetic = true,
            Type = inverseType?.Name ?? inverseTypeName,
        };
        inverse.EditedInverseType = inverse.InverseType;
        inverse.EditedType = inverse.GetRelationshipTypeName();

        return inverse;
    }

    public string? GetRelationshipTypeName(RelationshipType? type = null, Character? relative = null)
    {
        if ((type ?? EditedRelationshipType ?? RelationshipType) is not RelationshipType relationshipType)
        {
            return Type;
        }

        return relationshipType.Names?.TryGetValue(
            (relative ?? EditedRelative ?? Relative)?.GetNameGender() ?? EditedRelativeGender,
            out var name) == true
            ? name
            : relationshipType.Name;
    }

    public void OnDeserialized()
    {
        EditedInverseType = InverseType;
        EditedRelativeGender = RelativeGender;
        EditedRelativeName = RelativeId;
        EditedType = Type;
    }

    public override string ToString()
    {
        var sb = new StringBuilder(Relative?.DisplayName ?? RelativeId);

        if (GetRelationshipTypeName() is string { Length: > 0 } name)
        {
            if (sb.Length > 0)
            {
                sb.Append(": ");
            }
            sb.Append(name);
        }

        return sb.Length > 0
            ? sb.ToString()
            : "unknown";
    }

    private Relationship? GetForChild(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (Type?.EndsWith("child", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-"
                : "grand";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(null, x.Value.Mean - 25, x.Value.Max - 16))),
                Relative,
                character,
                true);
        }
        else if (string.Equals(Type, "sibling", StringComparison.OrdinalIgnoreCase))
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "child");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }
        else if (string.Equals(Type, "spouse", StringComparison.OrdinalIgnoreCase))
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "child");
            if (relationshipType is not null)
            {
                const string InLawSuffix = "-in-law";
                return FromType(
                    data,
                    new(
                        relationshipType.Name + InLawSuffix,
                        relationshipType.InverseName is null ? null : relationshipType.InverseName + InLawSuffix,
                        relationshipType.Names?.ToDictionary(
                            x => x.Key,
                            x => x.Value + InLawSuffix),
                        relationshipType?.AgeGap?.ToDictionary(
                            x => x.Key,
                            x => new AgeGap(null, x.Value.Mean - 25, null))),
                    Relative,
                    character,
                    true);
            }
        }

        return null;
    }

    private Relationship? GetForGrandchild(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (Type?.EndsWith("child", StringComparison.OrdinalIgnoreCase) == true)
        {
            const string Prefix = "great-";
            var levels = (Type.Contains("grand", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                + GreatRegex().Matches(Type).Count;
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    Prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : Prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => Prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(
                            null,
                            x.Value.Mean - (levels * 25),
                            x.Value.Max - (levels * 16)))),
                Relative,
                character,
                true);
        }
        else if (string.Equals(Type, "sibling", StringComparison.OrdinalIgnoreCase))
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "child");
            if (relationshipType is not null)
            {
                var levels = (Type!.Contains("grand", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    + GreatRegex().Matches(Type).Count;
                if (levels > 0)
                {
                    var prefix = string.Concat([.. Enumerable.Repeat("great-", levels - 1), "grand-"]);
                    var inverse = RelationshipType?.InverseName ?? InverseType;
                    return FromType(
                        data,
                        new(
                            prefix + (RelationshipType?.Name ?? Type),
                            inverse is null ? null : prefix + inverse,
                            RelationshipType?.Names?.ToDictionary(
                                x => x.Key,
                                x => prefix + x.Value),
                            RelationshipType?.AgeGap?.ToDictionary(
                                x => x.Key,
                                x => new AgeGap(
                                    null,
                                    x.Value.Mean - (levels * 25),
                                    x.Value.Max - (levels * 16)))),
                        Relative,
                        character,
                        true);
                }
            }
        }

        return null;
    }

    private Relationship? GetForGrandparent(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (string.Equals(Type, "cousin", StringComparison.OrdinalIgnoreCase))
        {
            var newRelationship = Clone();
            newRelationship.Synthetic = true;
            return newRelationship;
        }

        if (Type?.EndsWith("parent", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-great-"
                : "great-grand";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(
                            x.Value.Min + 32,
                            x.Value.Mean + 50,
                            x.Value.Max.HasValue
                                ? x.Value.Max + 84
                                : null))),
                Relative,
                character,
                true);
        }
        else if (Type?.EndsWith("pibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-"
                : "grand-";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type).Replace("grand", null).TrimStart('-'),
                    inverse is null ? null : prefix + inverse.Replace("grand", null).TrimStart('-'),
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value.Replace("grand", null).TrimStart('-')),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(
                            x.Value.Min + 6,
                            x.Value.Mean + 50,
                            null))),
                Relative,
                character,
                true);
        }
        else if (Type?.Equals("sibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "pibling");
            if (relationshipType is not null)
            {
                var levels = (Type.Contains("grand", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    + GreatRegex().Matches(Type).Count;
                if (levels > 0)
                {
                    var prefix = levels == 1
                        ? "grand-"
                        : string.Concat(Enumerable.Repeat("great-", levels - 1));
                    var inverse = RelationshipType?.InverseName ?? InverseType;
                    return FromType(
                        data,
                        new(
                            prefix + (RelationshipType?.Name ?? Type),
                            inverse is null ? null : prefix + inverse,
                            RelationshipType?.Names?.ToDictionary(
                                x => x.Key,
                                x => prefix + x.Value),
                            RelationshipType?.AgeGap?.ToDictionary(
                                x => x.Key,
                                x => new AgeGap(
                                    null,
                                    x.Value.Mean + (25 * levels),
                                    x.Value.Max + (68 * levels)))),
                        Relative,
                        character,
                        true);
                }
            }
        }
        else if (Type?.Equals("spouse", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "parent");
            if (relationshipType is not null)
            {
                var levels = (Type.Contains("grand", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    + GreatRegex().Matches(Type).Count;
                if (levels > 0)
                {
                    var prefix = string.Concat([.. Enumerable.Repeat("great-", levels - 1), "grand-"]);
                    var inverse = RelationshipType?.InverseName ?? InverseType;
                    return FromType(
                        data,
                        new(
                            prefix + (RelationshipType?.Name ?? Type),
                            inverse is null ? null : prefix + inverse,
                            RelationshipType?.Names?.ToDictionary(
                                x => x.Key,
                                x => prefix + x.Value),
                            RelationshipType?.AgeGap?.ToDictionary(
                                x => x.Key,
                                x => new AgeGap(
                                    null,
                                    x.Value.Mean + (25 * levels),
                                    x.Value.Max + (68 * levels)))),
                        Relative,
                        character,
                        true);
                }
            }
        }

        return null;
    }

    private Relationship? GetForParent(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (string.Equals(Type, "cousin", StringComparison.OrdinalIgnoreCase))
        {
            var newRelationship = Clone();
            newRelationship.Synthetic = true;
            return newRelationship;
        }

        if (Type?.EndsWith("parent", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-"
                : "grand";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(
                            x.Value.Min + 16,
                            x.Value.Mean + 25,
                            x.Value.Max.HasValue
                                ? x.Value.Max + 42
                                : null))),
                Relative,
                character,
                true);
        }
        else if (Type?.EndsWith("pibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-"
                : "grand-";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type).Replace("grand", null).TrimStart('-'),
                    inverse is null ? null : prefix + inverse.Replace("grand", null).TrimStart('-'),
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value.Replace("grand", null).TrimStart('-')),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(x.Value.Min - 10, x.Value.Mean + 25, null))),
                Relative,
                character,
                true);
        }
        else if (Type?.Contains("child", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "sibling");
            if (relationshipType is not null)
            {
                if (Type?.EndsWith("in-law", StringComparison.OrdinalIgnoreCase) == true
                    || Type?.EndsWith("inlaw", StringComparison.OrdinalIgnoreCase) == true)
                {
                    const string InLawSuffix = "-in-law";
                    return FromType(
                        data,
                        new(
                            relationshipType.Name + InLawSuffix,
                            relationshipType.InverseName is null ? null : relationshipType.InverseName + InLawSuffix,
                            relationshipType.Names?.ToDictionary(
                                x => x.Key,
                                x => x.Value + InLawSuffix),
                            relationshipType?.AgeGap?.ToDictionary(
                                x => x.Key,
                                x => new AgeGap(null, x.Value.Mean, null))),
                        Relative,
                        character,
                        true);
                }

                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }
        else if (Type?.Equals("sibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "pibling");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }
        else if (Type?.Equals("spouse", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "parent");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }

        return null;
    }

    private Relationship? GetForPibling(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (string.Equals(Type, "cousin", StringComparison.OrdinalIgnoreCase))
        {
            var newRelationship = Clone();
            newRelationship.Synthetic = true;
            return newRelationship;
        }

        if (Type?.Contains("parent", StringComparison.OrdinalIgnoreCase) == true)
        {
            var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                ? "great-"
                : "grand";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(
                            x.Value.Min + 16,
                            x.Value.Mean + 25,
                            x.Value.Max.HasValue
                                ? x.Value.Max + 42
                                : null))),
                Relative,
                character,
                true);
        }
        else if (Type?.Contains("pibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            const string Prefix = "great-";
            var inverse = RelationshipType?.InverseName ?? InverseType;
            return FromType(
                data,
                new(
                    Prefix + (RelationshipType?.Name ?? Type),
                    inverse is null ? null : Prefix + inverse,
                    RelationshipType?.Names?.ToDictionary(
                        x => x.Key,
                        x => Prefix + x.Value),
                    RelationshipType?.AgeGap?.ToDictionary(
                        x => x.Key,
                        x => new AgeGap(x.Value.Min - 10, x.Value.Mean + 25, null))),
                Relative,
                character,
                true);
        }
        else if (string.Equals(Type, "child", StringComparison.OrdinalIgnoreCase))
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "cousin");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }
        else if (Type?.Equals("spouse", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "pibling");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }

        return null;
    }

    private Relationship? GetForSibling(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (string.Equals(Type, "cousin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Type, "sibling", StringComparison.OrdinalIgnoreCase)
            || Type.EndsWith("parent")
            || Type.EndsWith("pibling"))
        {
            var newRelationship = Clone();
            newRelationship.Synthetic = true;
            return newRelationship;
        }

        if (Type?.Equals("child", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "nibling");
            if (relationshipType is not null)
            {
                return FromType(
                    data,
                    relationshipType,
                    Relative,
                    character,
                    true);
            }
        }
        else if (Type?.EndsWith("child", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "nibling");
            if (relationshipType is not null)
            {
                var prefix = Type.Contains("grand", StringComparison.OrdinalIgnoreCase)
                    ? "great-"
                    : "grand-";
                var inverse = RelationshipType?.InverseName ?? InverseType;
                return FromType(
                    data,
                    new(
                        prefix + (RelationshipType?.Name ?? Type).Replace("grand", null).TrimStart('-'),
                        inverse is null ? null : prefix + inverse.Replace("grand", null).TrimStart('-'),
                        RelationshipType?.Names?.ToDictionary(
                            x => x.Key,
                            x => prefix + x.Value.Replace("grand", null).TrimStart('-')),
                        RelationshipType?.AgeGap?.ToDictionary(
                            x => x.Key,
                            x => new AgeGap(x.Value.Min - 10, x.Value.Mean + 25, null))),
                    Relative,
                    character,
                    true);
            }
        }
        else if (Type?.Contains("spouse", StringComparison.OrdinalIgnoreCase) == true)
        {
            var relationshipType = RelationshipType.GetRelationshipType(data, "sibling");
            if (relationshipType is not null)
            {
                const string InLawSuffix = "-in-law";
                return FromType(
                    data,
                    new(
                        relationshipType.Name + InLawSuffix,
                        relationshipType.InverseName is null ? null : relationshipType.InverseName + InLawSuffix,
                        relationshipType.Names?.ToDictionary(
                            x => x.Key,
                            x => x.Value + InLawSuffix),
                        relationshipType?.AgeGap?.ToDictionary(
                            x => x.Key,
                            x => new AgeGap(null, x.Value.Mean, null))),
                    Relative,
                    character,
                    true);
            }
        }

        return null;
    }

    private Relationship? GetForSpouse(ScopData data, Character character)
    {
        if (Type is null || Relative is null)
        {
            return null;
        }

        if (string.Equals(Type, "spouse", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Type, "child-in-law", StringComparison.OrdinalIgnoreCase))
        {
            var newRelationship = Clone();
            newRelationship.Synthetic = true;
            return newRelationship;
        }

        if (string.Equals(Type, "child", StringComparison.OrdinalIgnoreCase))
        {
            // child already has two parents (not including this character)
            if (Relative
                .RelationshipMap?
                .Where(x => x.RelativeId != character.Id)
                .Count(x => string.Equals(x.Type, "parent", StringComparison.OrdinalIgnoreCase)) > 1)
            {
                if (RelationshipType is not null)
                {
                    const string StepPrefix = "step-";
                    return FromType(
                        data,
                        new(
                            StepPrefix + RelationshipType.Name,
                            RelationshipType.InverseName is null ? null : StepPrefix + RelationshipType.InverseName,
                            RelationshipType.Names?.ToDictionary(
                                x => x.Key,
                                x => StepPrefix + x.Value),
                            RelationshipType?.AgeGap?.ToDictionary(
                                x => x.Key,
                                x => new AgeGap(null, x.Value.Mean, null))),
                        Relative,
                        character,
                        true);
                }
            }
            else
            {
                var newRelationship = Clone();
                newRelationship.Synthetic = true;
                return newRelationship;
            }
        }

        if (Type?.Equals("parent", StringComparison.OrdinalIgnoreCase) == true
            || Type?.Equals("sibling", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (RelationshipType is not null)
            {
                const string InLawSuffix = "-in-law";
                return FromType(
                    data,
                    new(
                        RelationshipType.Name + InLawSuffix,
                        RelationshipType.InverseName is null ? null : RelationshipType.InverseName + InLawSuffix,
                        RelationshipType.Names?.ToDictionary(
                            x => x.Key,
                            x => x.Value + InLawSuffix),
                        RelationshipType?.AgeGap?.ToDictionary(
                            x => x.Key,
                            x => new AgeGap(null, x.Value.Mean, null))),
                    Relative,
                    character,
                    true);
            }
        }

        return null;
    }

    public static bool operator ==(Relationship? left, Relationship? right) => EqualityComparer<Relationship>.Default.Equals(left, right);
    public static bool operator !=(Relationship? left, Relationship? right) => !(left == right);

    [GeneratedRegex("great", RegexOptions.IgnoreCase)]
    private static partial Regex GreatRegex();
}

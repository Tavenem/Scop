﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.Blazor.Framework;

namespace Scop;

public class Character : TraitContainer, INote, IEquatable<Character>
{
    public int? AgeYears { get; set; }

    public int? AgeMonths { get; set; }

    public int? AgeDays { get; set; }

    public DateTimeOffset? Birthdate { get; set; }

    [JsonIgnore] public string? CharacterFullName => GetName(true);

    [JsonIgnore] public string? CharacterName => GetName();

    public string? Content { get; set; }

    [JsonIgnore]
    public string? DisplayAge
    {
        get
        {
            if (DisplayAgeYears.HasValue)
            {
                return $": {DisplayAgeYears.Value}";
            }
            if (DisplayAgeMonths.HasValue)
            {
                return $": {DisplayAgeMonths.Value} mo";
            }
            if (DisplayAgeDays.HasValue)
            {
                return $": {DisplayAgeDays.Value} d";
            }
            return null;
        }
    }

    [JsonIgnore] public int? DisplayAgeYears { get; set; }

    [JsonIgnore] public int? DisplayAgeMonths { get; set; }

    [JsonIgnore] public int? DisplayAgeDays { get; set; }

    [JsonIgnore] public string? DisplayEthnicity { get; set; }

    [JsonIgnore] public string? DisplayGender => string.IsNullOrEmpty(Gender) ? null : $": {Gender}";

    [JsonIgnore] public string DisplayName => CharacterName ?? Name ?? Type;

    public List<string[]>? EthnicityPaths { get; set; }

    public string? Gender { get; set; }

    public bool HyphenatedSurname { get; set; }

    [JsonIgnore] public int IconIndex => 1;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore]
    public bool IsUnnamed => string.IsNullOrWhiteSpace(Name)
        && (Names is null
        || Names.Count == 0)
        && (Surnames is null
        || Surnames.Count == 0);

    [JsonIgnore] public ElementList<INote>? List { get; set; }

    public string? Name { get; set; }

    public List<string>? Names { get; set; }

    public List<INote>? Notes { get; set; }

    [JsonIgnore] public INote? Parent { get; set; }

    public Pronouns Pronouns { get; set; }

    [JsonIgnore] public HashSet<string>? RelationshipIds { get; set; }

    [JsonIgnore] public List<Relationship>? RelationshipMap { get; set; }

    public List<Relationship>? Relationships { get; set; }

    public string? Suffix { get; set; }

    public List<string>? Surnames { get; set; }

    public string? Title { get; set; }

    [JsonIgnore] public string Type => "Character";

    public static string? GetRelationshipName(string? type, NameGender gender)
    {
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }

        if (type.EndsWith("grandchild"))
        {
            if (gender == NameGender.Female)
            {
                return type[..^5] + "daughter";
            }
            if (gender == NameGender.Male)
            {
                return type[..^5] + "son";
            }
            return type;
        }

        if (type.EndsWith("grandparent"))
        {
            if (gender == NameGender.Female)
            {
                return type[..^6] + "daughter";
            }
            if (gender == NameGender.Male)
            {
                return type[..^6] + "son";
            }
            return type;
        }

        switch (type)
        {
            case "child":
                if (gender == NameGender.Female)
                {
                    return "daughter";
                }
                if (gender == NameGender.Male)
                {
                    return "son";
                }
                break;
            case "child-in-law":
                if (gender == NameGender.Female)
                {
                    return "daughter-in-law";
                }
                if (gender == NameGender.Male)
                {
                    return "son-in-law";
                }
                break;
            case "parent":
                if (gender == NameGender.Female)
                {
                    return "mother";
                }
                if (gender == NameGender.Male)
                {
                    return "father";
                }
                break;
            case "parent-in-law":
                if (gender == NameGender.Female)
                {
                    return "mother-in-law";
                }
                if (gender == NameGender.Male)
                {
                    return "father-in-law";
                }
                break;
            case "sibling":
                if (gender == NameGender.Female)
                {
                    return "sister";
                }
                if (gender == NameGender.Male)
                {
                    return "brother";
                }
                break;
            case "sibling-in-law":
                if (gender == NameGender.Female)
                {
                    return "sister-in-law";
                }
                if (gender == NameGender.Male)
                {
                    return "brother-in-law";
                }
                break;
            case "pibling":
                if (gender == NameGender.Female)
                {
                    return "aunt";
                }
                if (gender == NameGender.Male)
                {
                    return "uncle";
                }
                break;
            case "nibling":
                if (gender == NameGender.Female)
                {
                    return "niece";
                }
                if (gender == NameGender.Male)
                {
                    return "nephew";
                }
                break;
            case "spouse":
                if (gender == NameGender.Female)
                {
                    return "wife";
                }
                if (gender == NameGender.Male)
                {
                    return "husband";
                }
                break;
            case "ex-spouse":
                if (gender == NameGender.Female)
                {
                    return "ex-wife";
                }
                if (gender == NameGender.Male)
                {
                    return "ex-husband";
                }
                break;
            case "sweetheart":
                if (gender == NameGender.Female)
                {
                    return "girlfriend";
                }
                if (gender == NameGender.Male)
                {
                    return "boyfriend";
                }
                break;
            case "ex-sweetheart":
                if (gender == NameGender.Female)
                {
                    return "ex-girlfriend";
                }
                if (gender == NameGender.Male)
                {
                    return "ex-boyfriend";
                }
                break;
        }
        return type;
    }

    public static void SetRelationshipMaps(List<Character> characters)
    {
        foreach (var character in characters)
        {
            character.RelationshipIds = [character.Id];
            character.RelationshipMap =
            [
                new()
                {
                    Id = character.Id,
                    Type = "self",
                }
            ];
        }

        foreach (var character in characters
            .Where(x => x.Relationships is not null))
        {
            var name = character.GetName();
            var gender = character.GetNameGender();

            foreach (var relationship in character.Relationships!)
            {
                if (relationship.Id is not null
                    && character.RelationshipIds!.Contains(relationship.Id))
                {
                    continue;
                }

                character.RelationshipMap!.Add(relationship);

                if (relationship.Id is null)
                {
                    continue;
                }

                var relative = characters.Find(x => x.Id == relationship.Id);
                if (relative is null)
                {
                    relationship.Id = null;
                    continue;
                }

                character.RelationshipIds!.Add(relationship.Id);
                relationship.Relative = relative;
                relationship.EditedRelativeName = relative.CharacterName;

                if (relative.RelationshipMap?.Any(x => x.Id == character.Id) != true
                    && relative.Relationships?.Any(x => x.Id == character.Id) != true)
                {
                    var inverseType = relationship.InverseType ?? relationship.Type;
                    var inverted = !string.IsNullOrEmpty(relationship.InverseType);
                    if (!string.IsNullOrEmpty(relationship.Type)
                        && !inverted)
                    {
                        if (relationship.Type.EndsWith("grandchild"))
                        {
                            inverseType = relationship.Type[..^5] + "parent";
                            inverted = true;
                        }
                        else if (relationship.Type.EndsWith("grandparent"))
                        {
                            inverseType = relationship.Type[..^6] + "child";
                            inverted = true;
                        }
                        else
                        {
                            switch (relationship.Type)
                            {
                                case "child":
                                    inverseType = "parent";
                                    inverted = true;
                                    break;
                                case "child-in-law":
                                    inverseType = "parent-in-law";
                                    inverted = true;
                                    break;
                                case "parent":
                                    inverseType = "child";
                                    inverted = true;
                                    break;
                                case "parent-in-law":
                                    inverseType = "child-in-law";
                                    inverted = true;
                                    break;
                                case "sibling":
                                case "spouse":
                                case "ex-spouse":
                                case "sweetheart":
                                case "ex-sweetheart":
                                    inverted = true;
                                    break;
                                case "pibling":
                                    inverseType = "nibling";
                                    inverted = true;
                                    break;
                                case "nibling":
                                    inverseType = "pibling";
                                    inverted = true;
                                    break;
                            }
                        }
                    }
                    var relationshipName = GetRelationshipName(inverseType, gender);
                    relative.RelationshipMap!.Add(new Relationship
                    {
                        EditedInverseType = relationship.Type,
                        EditedRelationshipName = relationshipName,
                        EditedRelativeName = name,
                        EditedType = inverseType,
                        Id = character.Id,
                        InverseType = relationship.Type,
                        RelationshipName = relationshipName,
                        Relative = character,
                        Synthetic = true,
                        Type = inverseType,
                    });
                    relative.RelationshipIds!.Add(character.Id);
                }
            }
        }

        void AdjustRelativeMap(
            Character character,
            Relationship relationship,
            HashSet<Character> queue)
        {
            var adjustedMap = AdjustRelationshipMap(
                relationship.Relative!.RelationshipMap,
                relationship.Type switch
                {
                    "child" => AdjustRelationshipForChild,
                    "parent" => AdjustRelationshipForParent,
                    "sibling" => AdjustRelationshipForSibling,
                    "spouse" => AdjustRelationshipForSpouse,
                    _ => _ => null,
                });
            if (adjustedMap is null)
            {
                return;
            }

            var anyAdded = false;
            foreach (var relativeRelationship in adjustedMap)
            {
                if (!string.IsNullOrEmpty(relativeRelationship.Id)
                    && character.RelationshipIds!
                    .Contains(relativeRelationship.Id))
                {
                    continue;
                }
                else if (string.IsNullOrEmpty(relativeRelationship.Id)
                    && character.RelationshipMap!
                    .Any(x => string.IsNullOrEmpty(x.Id)
                    && x.RelativeName == relativeRelationship.RelativeName))
                {
                    continue;
                }

                character.RelationshipMap!.Add(relativeRelationship);
                if (!string.IsNullOrEmpty(relativeRelationship.Id))
                {
                    character.RelationshipIds!.Add(relativeRelationship.Id);
                }
                anyAdded = true;

                if (relationship.Type == "parent"
                    && relativeRelationship.Type == "parent")
                {
                    AdjustRelativeMap(character, relativeRelationship, queue);
                }
            }

            if (anyAdded)
            {
                foreach (var relationship2 in character.RelationshipMap!
                    .Where(x => x.Id != character.Id
                    && x.Relative is not null))
                {
                    queue.Add(relationship2.Relative!);
                }
            }
        }

        var queue = new HashSet<Character>(characters
            .Where(x => x.RelationshipMap!.Count > 1));
        while (queue.Count > 0)
        {
            var character = queue.First();
            queue.Remove(character);

            foreach (var relationship in character.RelationshipMap!
                .Where(x => x.Id != character.Id
                    && x.Relative is not null)
                .ToList())
            {
                AdjustRelativeMap(character, relationship, queue);
            }
        }

        foreach (var character in characters)
        {
            character.RelationshipIds = null;
            character.RelationshipMap!.RemoveAll(x => x.Id == character.Id);
        }
    }

    public IEnumerable<Character> AllCharacters()
    {
        yield return this;
        if (Notes is not null)
        {
            foreach (var note in Notes)
            {
                foreach (var child in note.AllCharacters())
                {
                    yield return child;
                }
            }
        }
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the
    /// same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref
    /// name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Character? other) => other is not null && other.Id == Id;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>
    /// <see langword="true" /> if the specified object  is equal to the current
    /// object; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => Equals(obj as Character);

    public Character? FindCharacter(string id)
    {
        if (Id == id)
        {
            return this;
        }
        if (Notes is not null)
        {
            foreach (var child in Notes.OfType<Character>())
            {
                var found = child.FindCharacter(id);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    public (int? years, int? months, int? days) GetAge(DateTimeOffset? now)
    {
        if (!now.HasValue || !Birthdate.HasValue)
        {
            return (AgeYears, AgeMonths, AgeDays);
        }

        var years = now.Value.Year - Birthdate.Value.Year;
        if (years < 0)
        {
            return (null, null, null);
        }

        var months = now.Value.Month - Birthdate.Value.Month;
        if (months < 0)
        {
            months += 12;
            years--;
            if (years < 0)
            {
                return (null, null, null);
            }
        }

        var days = now.Value.Day - Birthdate.Value.Day;
        if (days < 0)
        {
            months--;
            if (months < 0)
            {
                months += 12;
                years--;
                if (years < 0)
                {
                    return (null, null, null);
                }
            }
            days += DateTime.DaysInMonth(now.Value.Year, now.Value.Month == 1 ? 12 : (now.Value.Month - 1));
        }

        return (years, months, days);
    }

    public (int min, int max, int? mean) GetAgeRange()
    {
        var minAge = 0;
        var maxAge = 100;

        if (RelationshipMap is null)
        {
            return (minAge, maxAge, null);
        }

        int? mean = null;
        var minAgeGood = false;
        var maxAgeGood = false;
        var gender = GetNameGender();

        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Type is null
                || relationship.Relative is null)
            {
                continue;
            }
            var relativeAge = relationship.Relative.DisplayAgeYears;
            if (!relativeAge.HasValue)
            {
                continue;
            }

            var relativeAgeYears = relativeAge.Value;

            var relativeGender = relationship.Relative.GetNameGender();

            if (relationship.Type.EndsWith("child"))
            {
                var counter = 0;
                var type = relationship.Type.AsSpan();
                while (type.StartsWith("great"))
                {
                    counter++;
                    type = type[5..];
                }
                if (type.StartsWith("grand"))
                {
                    counter++;
                    type = type[5..];
                }
                if (type == "child")
                {
                    counter++;

                    minAge = Math.Max(minAge, (counter * 16) + relativeAgeYears);
                    minAgeGood = true;

                    mean = (counter * 22) + relativeAgeYears;

                    if (gender.HasFlag(NameGender.Female))
                    {
                        maxAge = Math.Min(maxAge, 42 + relativeAgeYears);
                        maxAgeGood = true;
                    }
                }
            }
            else if (relationship.Type.EndsWith("parent"))
            {
                var counter = 0;
                var type = relationship.Type.AsSpan();
                while (type.StartsWith("great"))
                {
                    counter++;
                    type = type[5..];
                }
                if (type.StartsWith("grand"))
                {
                    counter++;
                    type = type[5..];
                }
                if (type == "parent")
                {
                    counter++;

                    maxAge = Math.Min(maxAge, relativeAgeYears - (counter * 16));
                    maxAgeGood = true;

                    mean = relativeAgeYears - (counter * 22);

                    if (gender.HasFlag(NameGender.Female))
                    {
                        minAge = Math.Max(minAge, relativeAgeYears - 42);
                        minAgeGood = true;
                    }
                }
            }
            else if (relationship.Type.EndsWith("pibling"))
            {
                var counter = 0;
                var type = relationship.Type.AsSpan();
                while (type.StartsWith("great"))
                {
                    counter++;
                    type = type[5..];
                }
                if (type == "pibling")
                {
                    if (!minAgeGood)
                    {
                        minAge = Math.Max(minAge, relativeAgeYears - ((counter - 1) * 16) - 84);

                        mean = relativeAgeYears - ((counter - 1) * 22) - 84;
                    }
                    if (!maxAgeGood)
                    {
                        maxAge = Math.Min(maxAge, relativeAgeYears - (counter * 16) + 42 - 32);
                    }
                }
            }
            else
            {
                switch (relationship.Type)
                {
                    case "cousin":
                        if (!minAgeGood)
                        {
                            minAge = Math.Max(minAge, relativeAgeYears - 52);

                            mean = relativeAgeYears;
                        }
                        if (!maxAgeGood)
                        {
                            maxAge = Math.Min(maxAge, relativeAgeYears + 52);
                        }
                        break;
                    case "nibling":
                        if (!minAgeGood)
                        {
                            minAge = Math.Max(minAge, relativeAgeYears - 68);

                            mean = relativeAgeYears - 22;
                        }
                        if (!maxAgeGood)
                        {
                            maxAge = Math.Min(maxAge, relativeAgeYears + 68);
                        }
                        break;
                    case "sibling":
                        if (!minAgeGood)
                        {
                            minAge = Math.Max(minAge, relativeAgeYears - 26);

                            mean = relativeAgeYears;
                        }
                        if (!maxAgeGood)
                        {
                            maxAge = Math.Min(maxAge, relativeAgeYears + 26);
                        }
                        break;
                    case "spouse":
                    case "ex-spouse":
                        if (gender.HasFlag(NameGender.Male)
                            && relativeGender.HasFlag(NameGender.Female))
                        {
                            minAge = Math.Max(minAge, relativeAgeYears - 3);
                        }
                        else
                        {
                            minAge = Math.Max(minAge, relativeAgeYears - 20);
                        }

                        mean = relativeAgeYears;

                        if (gender.HasFlag(NameGender.Female)
                            && relativeGender.HasFlag(NameGender.Male))
                        {
                            maxAge = Math.Min(maxAge, relativeAgeYears + 3);
                        }
                        else
                        {
                            maxAge = Math.Min(maxAge, relativeAgeYears + 20);
                        }
                        break;
                }
            }
            if (minAge >= maxAge
                && minAgeGood
                && maxAgeGood)
            {
                break;
            }
        }

        minAge = Math.Max(0, minAge);
        maxAge = Math.Max(0, Math.Min(100, maxAge));
        if (minAge > maxAge)
        {
            if (!minAgeGood)
            {
                minAge = maxAge;
            }
            else if (!maxAgeGood)
            {
                maxAge = minAge;
            }
            else
            {
                minAge = maxAge = (minAge + maxAge) / 2;
            }
        }

        if (mean.HasValue)
        {
            if (mean < minAge)
            {
                mean = minAge;
            }
            if (mean > maxAge)
            {
                mean = maxAge;
            }
        }

        return (minAge, maxAge, mean);
    }

    public List<string[]> GetFamilyEthnicities()
    {
        var familyEthnicities = new List<string[]>();
        if (RelationshipMap is null)
        {
            return familyEthnicities;
        }

        List<string[]>? maternalEthnicities = null;
        List<string[]>? paternalEthnicities = null;
        List<string[]>? miscEthnicities = null;
        var miscEthnicityDegree = 0;
        List<string[]>? spouseEthnicities = null;
        var spouseEthnicityDegree = 0;

        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Type is null
                || relationship.Relative is null
                || relationship.Relative.EthnicityPaths is null
                || relationship.Relative.EthnicityPaths.Count == 0)
            {
                continue;
            }

            if (relationship.Type.EndsWith("child"))
            {
                if (miscEthnicities is null
                    || miscEthnicities.Count == 0
                    || miscEthnicityDegree < 2)
                {
                    miscEthnicities = relationship.Relative.EthnicityPaths;
                    miscEthnicityDegree = 2;
                }
                continue;
            }

            if (relationship.Type != "parent"
                && relationship.Type.EndsWith("parent"))
            {
                if (miscEthnicities is null
                    || miscEthnicities.Count == 0
                    || miscEthnicityDegree < 3)
                {
                    miscEthnicities = relationship.Relative.EthnicityPaths;
                    miscEthnicityDegree = 3;
                }
                continue;
            }

            switch (relationship.Type)
            {
                case "parent":
                    var gender = relationship.Relative.GetNameGender();
                    if (gender == NameGender.Female)
                    {
                        if (maternalEthnicities is null
                            || maternalEthnicities.Count == 0)
                        {
                            maternalEthnicities = relationship.Relative.EthnicityPaths;
                        }
                        else
                        {
                            maternalEthnicities.AddRange(relationship.Relative.EthnicityPaths);
                        }
                    }
                    else if (gender == NameGender.Male)
                    {
                        if (paternalEthnicities is null
                            || paternalEthnicities.Count == 0)
                        {
                            paternalEthnicities = relationship.Relative.EthnicityPaths;
                        }
                        else
                        {
                            paternalEthnicities.AddRange(relationship.Relative.EthnicityPaths);
                        }
                    }
                    else
                    {
                        if (miscEthnicities is null
                            || miscEthnicities.Count == 0
                            || miscEthnicityDegree < 5)
                        {
                            miscEthnicities = relationship.Relative.EthnicityPaths;
                            miscEthnicityDegree = 5;
                        }
                    }
                    break;
                case "sibling":
                    if (miscEthnicities is null
                        || miscEthnicities.Count == 0
                        || miscEthnicityDegree < 4)
                    {
                        miscEthnicities = relationship.Relative.EthnicityPaths;
                        miscEthnicityDegree = 4;
                    }
                    break;
                case "pibling":
                case "cousin":
                case "nibling":
                    if (miscEthnicities is null
                        || miscEthnicities.Count == 0
                        || miscEthnicityDegree < 1)
                    {
                        miscEthnicities = relationship.Relative.EthnicityPaths;
                        miscEthnicityDegree = 1;
                    }
                    break;
                case "spouse":
                    if (spouseEthnicities is null
                        || spouseEthnicities.Count == 0
                        || spouseEthnicityDegree < 2)
                    {
                        spouseEthnicities = relationship.Relative.EthnicityPaths;
                        spouseEthnicityDegree = 2;
                    }
                    break;
                case "ex-spouse":
                    if (spouseEthnicities is null
                        || spouseEthnicities.Count == 0
                        || spouseEthnicityDegree < 1)
                    {
                        spouseEthnicities = relationship.Relative.EthnicityPaths;
                        spouseEthnicityDegree = 1;
                    }
                    break;
            }
        }

        if (maternalEthnicities is not null)
        {
            familyEthnicities.AddRange(maternalEthnicities);
        }
        if (paternalEthnicities is not null)
        {
            familyEthnicities.AddRange(paternalEthnicities);
        }
        if (familyEthnicities.Count == 0
            && miscEthnicities is not null)
        {
            familyEthnicities.AddRange(miscEthnicities);
        }
        if (familyEthnicities.Count == 0
            && spouseEthnicities is not null)
        {
            familyEthnicities.AddRange(spouseEthnicities);
        }
        return familyEthnicities;
    }

    public List<string> GetFamilySurnames()
    {
        var familyNames = new List<string>();
        if (RelationshipMap is null)
        {
            return familyNames;
        }

        string? familyName = null;
        var familyNameDegree = 0;

        string? spousalName = null;
        var spousalNameDegree = 0;

        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Type is null
                || relationship.Relative is null
                || relationship.Relative.Surnames is null
                || relationship.Relative.Surnames.Count == 0)
            {
                continue;
            }

            if (relationship.Type.EndsWith("child")
                || (relationship.Type != "parent"
                && relationship.Type.EndsWith("parent")))
            {
                if (familyName is null
                    || familyNameDegree < 2)
                {
                    familyName = relationship.Relative.Surnames[0];
                    familyNameDegree = 2;
                }
                continue;
            }
            switch (relationship.Type)
            {
                case "parent":
                    if (familyName is null
                        || familyNameDegree < 5)
                    {
                        if (relationship.Relative.Pronouns == Pronouns.HeHim)
                        {
                            familyName = relationship.Relative.Surnames[0];
                            familyNameDegree = 5;
                        }
                        else if (familyNameDegree < 4)
                        {
                            familyName = relationship.Relative.Surnames[0];
                            familyNameDegree = 4;
                        }
                    }
                    break;
                case "sibling":
                    if (familyName is null
                        || familyNameDegree < 3)
                    {
                        familyName = relationship.Relative.Surnames[0];
                        familyNameDegree = 3;
                    }
                    break;
                case "spouse":
                    if (spousalName is null
                        || spousalNameDegree < 4)
                    {
                        if (relationship.Relative.Pronouns == Pronouns.HeHim)
                        {
                            spousalName = relationship.Relative.Surnames[0];
                            spousalNameDegree = 4;
                        }
                        else if (spousalNameDegree < 3)
                        {
                            spousalName = relationship.Relative.Surnames[^1];
                            spousalNameDegree = 3;
                        }
                    }
                    break;
                case "ex-spouse":
                    if (spousalName is null
                        || spousalNameDegree < 2)
                    {
                        if (relationship.Relative.Pronouns == Pronouns.HeHim)
                        {
                            spousalName = relationship.Relative.Surnames[0];
                            spousalNameDegree = 2;
                        }
                        else if (spousalNameDegree < 1 && relationship.Relative.Surnames.Count == 1)
                        {
                            spousalName = relationship.Relative.Surnames[0];
                            spousalNameDegree = 1;
                        }
                    }
                    break;
                case "pibling":
                case "cousin":
                case "nibling":
                    if (familyName is null
                        || familyNameDegree < 1)
                    {
                        familyName = relationship.Relative.Surnames[0];
                        familyNameDegree = 1;
                    }
                    break;
            }
        }

        if (familyName is not null)
        {
            familyNames.Add(familyName);
        }
        if (spousalName is not null)
        {
            if (GetNameGender() != NameGender.Male
                || familyNames.Count == 0)
            {
                familyNames.Add(spousalName);
            }
        }
        return familyNames;
    }

    /// <summary>Serves as the default hash function.</summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(Id);

    public NameGender GetNameGender() => Pronouns switch
    {
        Pronouns.SheHer => NameGender.Female,
        Pronouns.HeHim => NameGender.Male,
        _ => NameGender.Both,
    };

    public double GetNameMatchScore(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        var length = 0;
        var matches = 0;

        var parts = name.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 1)
        {
            length++;

            if (!string.IsNullOrWhiteSpace(Suffix)
                && string.Equals(
                    Suffix.Trim(),
                    string.Join(", ", parts[1..]),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                matches++;
            }
        }

        parts = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        length += parts.Length;

        string[]? titleParts = null;
        if (!string.IsNullOrWhiteSpace(Title)
            && Title.Contains(' '))
        {
            titleParts = Title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        for (var i = 0; i < parts.Length; i++)
        {
            if (Names?.Any(x =>
                string.Equals(parts[i], x.Trim(), StringComparison.InvariantCultureIgnoreCase)) == true
                || Surnames?.Any(x =>
                string.Equals(parts[i], x.Trim(), StringComparison.InvariantCultureIgnoreCase)) == true)
            {
                matches++;
                continue;
            }
            if (!string.IsNullOrWhiteSpace(Title))
            {
                if (string.Equals(parts[i], Title.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    matches++;
                }
                else if (titleParts is not null)
                {
                    if (titleParts.Any(x =>
                        string.Equals(parts[i], x, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        matches++;
                    }
                }
            }
        }

        return Math.Min(matches, length) / length;
    }

    public string? GetRelationshipName(string? type, NameGender? gender = null)
        => GetRelationshipName(type, gender ?? GetNameGender());

    public bool HasEthnicity(Ethnicity ethnicity) => ethnicity.Hierarchy is not null
        && EthnicityPaths?.Any(x => x.StartsWith(ethnicity.Hierarchy)) == true;

    public void Initialize()
    {
        if (Notes is not null)
        {
            foreach (var child in Notes)
            {
                child.Parent = this;
                child.Initialize();
            }
        }
    }

    public void LoadCharacters(Story story)
    {
        SetDisplayAge(story);
        SetDisplayEthnicity();
        SetDisplayTraits();

        if (Notes is not null)
        {
            foreach (var child in Notes)
            {
                child.LoadCharacters(story);
            }
        }
    }

    public void SelectEthnicity(Ethnicity ethnicity, bool value)
    {
        if (ethnicity.Hierarchy is null || ethnicity.Hierarchy.Length == 0)
        {
            return;
        }
        if (value)
        {
            EthnicityPaths?.RemoveAll(x => ethnicity.Hierarchy.StartsWith(x));
            if (EthnicityPaths?.Count == 0)
            {
                EthnicityPaths = null;
            }
            if (EthnicityPaths?.Any(x => x.StartsWith(ethnicity.Hierarchy)) != true)
            {
                (EthnicityPaths ??= []).Add(ethnicity.Hierarchy);
            }
        }
        else
        {
            EthnicityPaths?.RemoveAll(x => x.StartsWith(ethnicity.Hierarchy));
        }
        SetDisplayEthnicity();
    }

    public void SetAge(Story? story, int? years, int? months, int? days)
    {
        if (Birthdate.HasValue
            && story?.Now.HasValue == true
            && (years.HasValue
            || months.HasValue
            || days.HasValue))
        {
            if (!days.HasValue)
            {
                AgeDays = null;
            }
            if (!months.HasValue)
            {
                AgeMonths = null;
            }
            if (!years.HasValue)
            {
                AgeYears = null;
            }
            try
            {
                Birthdate = story.Now.Value.AddYears(-(years ?? 0));
                Birthdate = Birthdate.Value.AddMonths(-(months ?? 0));
                Birthdate = Birthdate.Value.AddDays(-(days ?? 0));
            }
            catch
            {
                AgeYears = years;
                AgeMonths = months;
                AgeDays = days;
                Birthdate = null;
            }
        }
        else
        {
            AgeYears = years;
            AgeMonths = months;
            AgeDays = days;
            if (years.HasValue
                || months.HasValue
                || days.HasValue)
            {
                Birthdate = null;
            }
        }

        SetDisplayAge(story);
    }

    public void SetAgeDays(Story? story, int? value)
    {
        if (!value.HasValue)
        {
            if (AgeDays.HasValue)
            {
                AgeDays = null;
            }
        }
        else if (story?.Now.HasValue == true)
        {
            try
            {
                Birthdate = story.Now.Value;
                if (DisplayAgeYears.HasValue)
                {
                    Birthdate = Birthdate.Value.AddYears(-DisplayAgeYears.Value);
                }
                if (DisplayAgeMonths.HasValue)
                {
                    Birthdate = Birthdate.Value.AddMonths(-DisplayAgeMonths.Value);
                }
                if (DisplayAgeDays.HasValue)
                {
                    Birthdate = Birthdate.Value.AddDays(-value.Value);
                }
            }
            catch
            {
                AgeYears = DisplayAgeYears;
                AgeMonths = DisplayAgeMonths;
                AgeDays = value;
                Birthdate = null;
            }
        }
        else if (AgeDays != value)
        {
            AgeYears = DisplayAgeYears;
            AgeMonths = DisplayAgeMonths;
            AgeDays = value;
            Birthdate = null;
        }

        SetDisplayAge(story);
    }

    public void SetAgeMonths(Story? story, int? value)
    {
        if (!value.HasValue)
        {
            if (AgeMonths.HasValue)
            {
                AgeMonths = null;
            }
        }
        else if (story?.Now.HasValue == true)
        {
            try
            {
                Birthdate = story.Now.Value;
                if (DisplayAgeYears.HasValue)
                {
                    Birthdate = Birthdate.Value.AddYears(-DisplayAgeYears.Value);
                }
                if (DisplayAgeMonths.HasValue)
                {
                    Birthdate = Birthdate.Value.AddMonths(-value.Value);
                }
                if (DisplayAgeDays.HasValue)
                {
                    Birthdate = Birthdate.Value.AddDays(-DisplayAgeDays.Value);
                }
            }
            catch
            {
                AgeYears = DisplayAgeYears;
                AgeMonths = value;
                AgeDays = DisplayAgeDays;
                Birthdate = null;
            }
        }
        else if (AgeMonths != value)
        {
            AgeYears = DisplayAgeYears;
            AgeMonths = value;
            AgeDays = DisplayAgeDays;
            Birthdate = null;
        }

        SetDisplayAge(story);
    }

    public void SetAgeYears(Story? story, int? value)
    {
        if (!value.HasValue)
        {
            if (AgeYears.HasValue)
            {
                AgeYears = null;
            }
        }
        else if (story?.Now.HasValue == true)
        {
            try
            {
                Birthdate = story.Now.Value.AddYears(-value.Value);
                if (DisplayAgeMonths.HasValue)
                {
                    Birthdate = Birthdate.Value.AddMonths(-DisplayAgeMonths.Value);
                }
                if (DisplayAgeDays.HasValue)
                {
                    Birthdate = Birthdate.Value.AddDays(-DisplayAgeDays.Value);
                }
            }
            catch
            {
                AgeYears = value;
                AgeMonths = DisplayAgeMonths;
                AgeDays = DisplayAgeDays;
                Birthdate = null;
            }
        }
        else if (AgeYears != value)
        {
            AgeYears = value;
            AgeMonths = DisplayAgeMonths;
            AgeDays = DisplayAgeDays;
            Birthdate = null;
        }

        SetDisplayAge(story);
    }

    public void SetBirthdate(Story? story, DateTimeOffset? value)
    {
        Birthdate = value;
        SetDisplayAge(story);
    }

    public void SetDisplayAge(Story? story)
    {
        if (story?.Now.HasValue == true)
        {
            (DisplayAgeYears, DisplayAgeMonths, DisplayAgeDays) = GetAge(story.Now.Value);
        }
        else if (Birthdate.HasValue && !AgeYears.HasValue)
        {
            (DisplayAgeYears, DisplayAgeMonths, DisplayAgeDays) = GetAge(DateTimeOffset.Now);
        }
        else
        {
            DisplayAgeYears = AgeYears;
            DisplayAgeMonths = AgeMonths;
            DisplayAgeDays = AgeDays;
        }
    }

    public void SetDisplayEthnicity()
    {
        if (EthnicityPaths is null
            || EthnicityPaths.Count == 0)
        {
            DisplayEthnicity = null;
            return;
        }

        var ethnicities = new List<Ethnicity>();
        foreach (var path in EthnicityPaths)
        {
            if (path.Length == 0)
            {
                continue;
            }
            var ethnicity = ethnicities
                .Find(x => string.Equals(x.Type, path[0], StringComparison.OrdinalIgnoreCase));
            if (ethnicity is null)
            {
                ethnicity = new()
                {
                    Type = path[0],
                };
                ethnicities.Add(ethnicity);
            }
            var parent = ethnicity;
            var i = 1;
            while (path.Length > i)
            {
                parent.Types ??= [];
                ethnicity = parent.Types
                    .Find(x => string.Equals(x.Type, path[i], StringComparison.OrdinalIgnoreCase));
                if (ethnicity is null)
                {
                    ethnicity = new()
                    {
                        Type = path[i],
                    };
                    parent.Types.Add(ethnicity);
                }
                i++;
                parent = ethnicity;
            }
        }

        if (ethnicities.Count == 0)
        {
            DisplayEthnicity = null;
            return;
        }

        static void AppendEthnicityTypes(StringBuilder sb, List<Ethnicity> ethnicities, string separator)
        {
            for (var i = 0; i < ethnicities.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(ethnicities[i].Type.ToTitle());
                if (ethnicities[i].Types?.Count > 0)
                {
                    sb.Append(": ");
                    AppendEthnicityTypes(sb, ethnicities[i].Types!, ", ");
                }
            }
        }

        var sb = new StringBuilder(": ");
        AppendEthnicityTypes(sb, ethnicities, "; ");
        DisplayEthnicity = sb.ToString();
    }

    public void SetEthnicities(List<string[]> ethnicities)
    {
        EthnicityPaths = ethnicities;
        SetDisplayEthnicity();
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => CharacterFullName ?? Name ?? Type;

    private static List<Relationship>? AdjustRelationshipMap(
        List<Relationship>? relationshipMap,
        Func<Relationship, Relationship?> func)
    {
        if (relationshipMap is null)
        {
            return null;
        }

        var newMap = new List<Relationship>();
        foreach (var relationship in relationshipMap)
        {
            var newRelationship = func(relationship);
            if (newRelationship is not null)
            {
                newMap.Add(newRelationship);
            }
        }

        return newMap.Count > 0
            ? newMap
            : null;
    }

    private static Relationship? AdjustRelationshipForChild(Relationship relationship)
    {
        var newRelationship = JsonSerializer.Deserialize<Relationship>(JsonSerializer.Serialize(relationship));
        if (newRelationship is null
            || relationship.Type is null)
        {
            return null;
        }
        newRelationship.Relative = relationship.Relative;
        newRelationship.Synthetic = true;

        if (relationship.Type == "child")
        {
            newRelationship.Type = "grandchild";
            newRelationship.RelationshipName = $"grand{relationship.RelationshipName}";
            return newRelationship;
        }

        if (relationship.Type.EndsWith("grandchild"))
        {
            newRelationship.Type = $"great{relationship.Type}";
            newRelationship.RelationshipName = $"great{relationship.RelationshipName}";
            return newRelationship;
        }

        var gender = relationship.Relative?.GetNameGender();

        switch (relationship.Type)
        {
            case "sibling":
                newRelationship.Type = "child";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "daughter";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "son";
                }
                else
                {
                    newRelationship.RelationshipName = "child";
                }
                return newRelationship;
            case "spouse":
                newRelationship.Type = "child-in-law";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "daughter-in-law";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "son-in-law";
                }
                else
                {
                    newRelationship.RelationshipName = "child-in-law";
                }
                return newRelationship;
        }

        return null;
    }

    private static Relationship? AdjustRelationshipForParent(Relationship relationship)
    {
        var newRelationship = JsonSerializer.Deserialize<Relationship>(JsonSerializer.Serialize(relationship));
        if (newRelationship is null
            || relationship.Type is null)
        {
            return null;
        }
        newRelationship.Relative = relationship.Relative;
        newRelationship.Synthetic = true;

        if (relationship.Type == "cousin")
        {
            return newRelationship;
        }

        if (relationship.Type == "parent")
        {
            newRelationship.Type = $"grand{relationship.Type}";
            newRelationship.RelationshipName = $"grand{relationship.RelationshipName}";
            return newRelationship;
        }

        if (relationship.Type.EndsWith("grandparent")
            || relationship.Type.EndsWith("pibling"))
        {
            newRelationship.Type = $"great{relationship.Type}";
            newRelationship.RelationshipName = $"great{relationship.RelationshipName}";
            return newRelationship;
        }

        var gender = relationship.Relative?.GetNameGender();

        switch (relationship.Type)
        {
            case "child":
                newRelationship.Type = "sibling";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "sister";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "brother";
                }
                else
                {
                    newRelationship.RelationshipName = "sibling";
                }
                return newRelationship;
            case "child-in-law":
                newRelationship.Type = "sibling-in-law";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "sister-in-law";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "brother-in-law";
                }
                else
                {
                    newRelationship.RelationshipName = "sibling-in-law";
                }
                return newRelationship;
            case "sibling":
                newRelationship.Type = "pibling";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "aunt";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "uncle";
                }
                else
                {
                    newRelationship.RelationshipName = "pibling";
                }
                return newRelationship;
            case "spouse":
                newRelationship.Type = "parent";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "mother";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "father";
                }
                else
                {
                    newRelationship.RelationshipName = "parent";
                }
                return newRelationship;
        }

        return null;
    }

    private static Relationship? AdjustRelationshipForSibling(Relationship relationship)
    {
        var newRelationship = JsonSerializer.Deserialize<Relationship>(JsonSerializer.Serialize(relationship));
        if (newRelationship is null
            || relationship.Type is null)
        {
            return null;
        }
        newRelationship.Relative = relationship.Relative;
        newRelationship.Synthetic = true;

        if (relationship.Type == "cousin"
            || relationship.Type == "sibling"
            || relationship.Type.EndsWith("parent")
            || relationship.Type.EndsWith("pibling"))
        {
            return newRelationship;
        }

        var gender = relationship.Relative?.GetNameGender();

        switch (relationship.Type)
        {
            case "child":
                newRelationship.Type = "nibling";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "niece";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "nephew";
                }
                else
                {
                    newRelationship.RelationshipName = "nibling";
                }
                return newRelationship;
            case "spouse":
                newRelationship.Type = "sibling-in-law";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "sister-in-law";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "brother-in-law";
                }
                else
                {
                    newRelationship.RelationshipName = "sibling-in-law";
                }
                return newRelationship;
        }

        return null;
    }

    private static Relationship? AdjustRelationshipForSpouse(Relationship relationship)
    {
        var newRelationship = JsonSerializer.Deserialize<Relationship>(JsonSerializer.Serialize(relationship));
        if (newRelationship is null
            || relationship.Type is null)
        {
            return null;
        }
        newRelationship.Relative = relationship.Relative;
        newRelationship.Synthetic = true;

        var gender = relationship.Relative?.GetNameGender();

        switch (relationship.Type)
        {
            case "child":
            case "child-in-law":
            case "spouse":
                return newRelationship;
            case "parent":
                newRelationship.Type = "parent-in-law";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "mother-in-law";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "father-in-law";
                }
                else
                {
                    newRelationship.RelationshipName = "parent-in-law";
                }
                return newRelationship;
            case "parent-in-law":
                newRelationship.Type = "parent";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "mother";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "father";
                }
                else
                {
                    newRelationship.RelationshipName = "parent";
                }
                return newRelationship;
            case "sibling":
                newRelationship.Type = "sibling-in-law";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "sister-in-law";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "brother-in-law";
                }
                else
                {
                    newRelationship.RelationshipName = "sibling-in-law";
                }
                return newRelationship;
            case "sibling-in-law":
                newRelationship.Type = "sibling";
                if (gender == NameGender.Female)
                {
                    newRelationship.RelationshipName = "sister";
                }
                else if (gender == NameGender.Male)
                {
                    newRelationship.RelationshipName = "brother";
                }
                else
                {
                    newRelationship.RelationshipName = "sibling";
                }
                return newRelationship;
        }

        return null;
    }

    private void BuildName(StringBuilder sb, bool full = false)
    {
        sb.Append(Title);
        if (Names is not null)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            if (full)
            {
                sb.AppendJoin(' ', Names);
            }
            else
            {
                sb.Append(Names[0]);
            }
        }
        if (Surnames is not null)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }
            if (HyphenatedSurname)
            {
                sb.AppendJoin('-', Surnames);
            }
            else if (!full && GetNameGender() == NameGender.Female)
            {
                sb.Append(Surnames[^1]);
            }
            else
            {
                sb.AppendJoin(' ', Surnames);
            }
        }
        if (!string.IsNullOrWhiteSpace(Suffix))
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }
            sb.Append(Suffix);
        }
    }

    private string? GetName(bool full = false)
    {
        var sb = new StringBuilder();
        BuildName(sb, full);
        return sb.Length == 0 ? null : sb.ToString();
    }

    public static bool operator ==(Character? left, Character? right) => EqualityComparer<Character>.Default.Equals(left, right);
    public static bool operator !=(Character? left, Character? right) => !(left == right);
}

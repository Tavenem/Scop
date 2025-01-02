using Scop.Enums;
using Scop.Interfaces;
using Scop.Services;
using System.Text;
using System.Text.Json.Serialization;
using Tavenem.Blazor.Framework;
using Tavenem.Randomize;

namespace Scop.Models;

public class Character : TraitContainer, INote, IEquatable<Character>
{
    public int? AgeYears { get; set; }

    public int? AgeMonths { get; set; }

    public int? AgeDays { get; set; }

    public DateTimeOffset? Birthdate { get; set; }

    [JsonIgnore] public string? CharacterFullName => CharacterName?.ToString();

    public CharacterName? CharacterName { get; set; }

    internal string? _characterShortName;
    [JsonIgnore]
    public string? CharacterShortName => _characterShortName ??= CharacterName?.ToShortName();

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

    [JsonIgnore]
    public string? DisplayGender
    {
        get
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Gender))
            {
                sb.Append(Gender);
            }
            if (Pronouns != Pronouns.Other)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(Pronouns.GetDescription());
            }
            if (sb.Length > 0)
            {
                sb.Insert(0, ": ");
            }
            return sb.ToString();
        }
    }

    [JsonIgnore]
    public string DisplayName => IsDeceased
        ? $"{CharacterName?.ToShortName() ?? Name ?? Type} 💀"
        : CharacterName?.ToShortName() ?? Name ?? Type;

    public List<string[]>? EthnicityPaths { get; set; }

    public string? Gender { get; set; }

    [Obsolete("Use CharacterName.HasHyphenatedSurname")]
    public bool HyphenatedSurname { get; set; }

    [JsonIgnore] public int IconIndex => 1;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonIgnore]
    public bool Initialized { get; set; }

    public bool IsDeceased { get; set; }

    [JsonIgnore]
    public bool IsUnnamed => CharacterName?.IsEmpty != false && string.IsNullOrWhiteSpace(Name);

    [JsonIgnore] public ElementList<INote>? List { get; set; }

    public string? Name { get; set; }

    [Obsolete("Use CharacterName.GivenNames and CharacterName.MiddleNames")]
    public List<string>? Names { get; set; }

    public List<INote>? Notes { get; set; }

    [JsonIgnore] public INote? Parent { get; set; }

    [JsonIgnore] public string PlaceholderName => CharacterName?.ToShortName() ?? "New Character";

    public Pronouns Pronouns { get; set; }

    [JsonIgnore] public HashSet<string>? RelationshipIds { get; set; }

    [JsonIgnore] public List<Relationship>? RelationshipMap { get; set; }

    public List<Relationship>? Relationships { get; set; }

    [Obsolete("Use CharacterName.Suffixes")]
    public string? Suffix { get; set; }

    [Obsolete("Use CharacterName.Surnames")]
    public List<string>? Surnames { get; set; }

    [Obsolete("Use CharacterName.Title")]
    public string? Title { get; set; }

    [JsonIgnore] public string Type => "Character";

    public static Character FromNote(INote note) => new()
    {
        Content = note.Content,
        Name = note.Name,
        Notes = note.Notes,
        Parent = note.Parent,
    };

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

    public static void SetRelationshipMaps(ScopData data, List<Character> characters)
    {
        // must be separate loops: InitializeRelationshipMap affects the RelationshipMap of multiple characters
        foreach (var character in characters)
        {
            character.RelationshipMap = null;
        }
        foreach (var character in characters)
        {
            character.InitializeRelationshipMap(data, characters);
        }

        void AdjustRelativeMap(
            Character character,
            Relationship relationship,
            HashSet<Character> queue)
        {
            var anyAdded = false;
            foreach (var relativeRelationship in Relationship.GetAdjustedRelationships(
                data,
                character,
                relationship,
                relationship
                    .Relative?
                    .RelationshipMap?
                    .Where(x
                        => x.RelativeId is null
                        || (!string.Equals(x.RelativeId, character.Id, StringComparison.Ordinal)
                            && character.RelationshipIds?.Contains(x.RelativeId) != true))
                    .ToList()))
            {
                (character.RelationshipMap ??= []).Add(relativeRelationship);
                if (!string.IsNullOrEmpty(relativeRelationship.RelativeId))
                {
                    (character.RelationshipIds ??= []).Add(relativeRelationship.RelativeId);
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
                foreach (var relationship2 in character
                    .RelationshipMap!
                    .Where(x => x.Relative is not null))
                {
                    queue.Add(relationship2.Relative!);
                }
            }
        }

        var queue = new HashSet<Character>(characters
            .Where(x => x.RelationshipMap?.Count > 1));
        while (queue.Count > 0)
        {
            var character = queue.First();
            queue.Remove(character);

            foreach (var relationship in character
                .RelationshipMap!
                .Where(x => x.Relative is not null)
                .ToList())
            {
                AdjustRelativeMap(character, relationship, queue);
            }
        }

        foreach (var character in characters)
        {
            character.RelationshipIds = null;
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

    public void CopyFamilySurnames()
    {
        var familySurnames = GetFamilySurnames();
        if (familySurnames is null)
        {
            return;
        }

        var added = false;
        foreach (var name in familySurnames)
        {
            if (CharacterName?.Surnames?.Contains(name) != true)
            {
                ((CharacterName ??= new()).Surnames ??= []).Add(name);
                added = true;
            }
        }
        if (added)
        {
            _characterShortName = null;
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
            days += DateTime.DaysInMonth(now.Value.Year, now.Value.Month == 1 ? 12 : now.Value.Month - 1);
        }

        return (years, months, days);
    }

    public AgeGap GetAgeRange(Story? story)
    {
        if (RelationshipMap is null)
        {
            return new();
        }

        InitializeCharacter(story);
        var ageYears = DisplayAgeYears;
        var gender = GetNameGender();

        var minAge = 0;
        int? mean = null;
        var meanStrength = 0;
        var maxAge = 100;

        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Relative is null)
            {
                continue;
            }

            relationship.Relative.InitializeCharacter(story);
            var relativeAge = relationship.Relative.DisplayAgeYears;
            if (!relativeAge.HasValue)
            {
                continue;
            }

            var relativeAgeYears = relativeAge.Value;
            var relativeGender = relationship.Relative.GetNameGender();

            GetRelationshipAgeGap(
                ref minAge,
                ref mean,
                ref meanStrength,
                ref maxAge,
                relationship,
                relativeAgeYears,
                relativeGender);

            if (relationship.Inverse is not null
                && ageYears.HasValue)
            {
                GetRelationshipAgeGap(
                    ref minAge,
                    ref mean,
                    ref meanStrength,
                    ref maxAge,
                    relationship.Inverse,
                    ageYears.Value,
                    gender,
                    true);
            }

            if (minAge >= maxAge)
            {
                break;
            }
        }

        if (minAge > maxAge)
        {
            minAge = maxAge = (minAge + maxAge) / 2;
        }

        if (mean < minAge || mean > maxAge)
        {
            mean = null;
        }

        return new(minAge, mean, maxAge);

        static void GetRelationshipAgeGap(
            ref int minAge,
            ref int? mean,
            ref int meanStrength,
            ref int maxAge,
            Relationship relationship,
            int relativeAgeYears,
            NameGender relativeGender,
            bool inverse = false)
        {
            if (relationship.RelationshipType?.AgeGap is null
                || (!relationship.RelationshipType.AgeGap.TryGetValue(relativeGender, out var ageGap)
                && !relationship.RelationshipType.AgeGap.TryGetValue(NameGender.None, out ageGap)))
            {
                return;
            }

            if (ageGap.Min.HasValue)
            {
                if (inverse)
                {
                    maxAge = Math.Min(maxAge, relativeAgeYears - ageGap.Min.Value);
                }
                else
                {
                    minAge = Math.Max(minAge, relativeAgeYears + ageGap.Min.Value);
                }
            }

            if (ageGap.Max.HasValue)
            {
                if (inverse)
                {
                    minAge = Math.Max(minAge, relativeAgeYears - ageGap.Max.Value);
                }
                else
                {
                    maxAge = Math.Min(maxAge, relativeAgeYears + ageGap.Max.Value);
                }
            }

            if (ageGap.Mean.HasValue)
            {
                var relativeMeanStrength = 0;
                if (ageGap.Mean.Value != 0)
                {
                    relativeMeanStrength += 2;
                }
                if (ageGap.Min.HasValue)
                {
                    relativeMeanStrength++;
                }
                if (ageGap.Max.HasValue)
                {
                    relativeMeanStrength++;
                }

                if (!mean.HasValue || relativeMeanStrength > meanStrength)
                {
                    mean = relativeAgeYears + (inverse ? -ageGap.Mean.Value : ageGap.Mean.Value);
                    meanStrength = relativeMeanStrength;
                }
            }
        }
    }

    public List<string[]>? GetFamilyEthnicities()
    {
        if (!(RelationshipMap?.Count > 0))
        {
            return null;
        }

        Dictionary<int, List<string[]>>? familyEthnicities = null;
        Dictionary<int, List<string[]>>? spousalEthnicities = null;
        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Type is null
                || relationship.Relative is null
                || relationship.Relative.CharacterName?.IsEmpty != false
                || !(relationship.Relative.CharacterName.Surnames?.Count > 0)
                || !(relationship.Relative.EthnicityPaths?.Count > 0))
            {
                continue;
            }

            IEnumerable<string[]>? relativeFamilyEthnicities = null;
            IEnumerable<string[]>? relativeSpousalEthnicities = null;
            var familyConfidenceKey = 0;
            var spousalConfidenceKey = 0;

            if (relationship.Type != "child"
                && relationship.Type.EndsWith("child"))
            {
                relativeFamilyEthnicities = relationship
                    .Relative
                    .EthnicityPaths;
                familyConfidenceKey = 4;
            }
            else if (relationship.Type != "parent"
                && relationship.Type.EndsWith("parent"))
            {
                relativeFamilyEthnicities = relationship
                    .Relative
                    .EthnicityPaths;
                familyConfidenceKey = 1;
            }
            else
            {
                switch (relationship.Type)
                {
                    case "child":
                        relativeSpousalEthnicities = relationship
                            .Relative
                            .EthnicityPaths;
                        spousalConfidenceKey = 3;
                        break;
                    case "parent":
                    case "sibling":
                        relativeFamilyEthnicities = relationship
                            .Relative
                            .EthnicityPaths;
                        break;
                    case "spouse":
                    case "ex-spouse":
                        relativeSpousalEthnicities = relationship
                            .Relative
                            .EthnicityPaths;
                        spousalConfidenceKey = 5;
                        break;
                    case "pibling":
                    case "cousin":
                    case "nibling":
                        relativeFamilyEthnicities = relationship
                            .Relative
                            .EthnicityPaths;
                        familyConfidenceKey = 2;
                        break;
                }
            }

            if (relativeFamilyEthnicities?.Any() == true)
            {
                if (familyEthnicities?.TryGetValue(
                    familyConfidenceKey,
                    out var relativeEthnicities) != true)
                {
                    relativeEthnicities = [];
                    (familyEthnicities ??= [])[familyConfidenceKey] = relativeEthnicities;
                }
                relativeEthnicities!.AddRange(relativeFamilyEthnicities);
            }

            if (relativeSpousalEthnicities?.Any() == true)
            {
                if (spousalEthnicities?.TryGetValue(
                    spousalConfidenceKey,
                    out var relativeEthnicities) != true)
                {
                    relativeEthnicities = [];
                    (spousalEthnicities ??= [])[spousalConfidenceKey] = relativeEthnicities;
                }
                relativeEthnicities!.AddRange(relativeSpousalEthnicities);
            }
        }

        List<string[]>? ethnicities = null;

        if (familyEthnicities?.Count > 0)
        {
            ethnicities = [];
            var familyEthnicityCandidates = familyEthnicities[familyEthnicities
                .Keys
                .Order()
                .First()];
            ethnicities.AddRange(familyEthnicityCandidates);
        }

        if (!(ethnicities?.Count > 0)
            && spousalEthnicities?.Count > 0
            && Randomizer.Instance.NextDouble() < 0.81) // odds of ethnically-homogeneous marriage
        {
            ethnicities ??= [];
            var spousalEthnicityCandidates = spousalEthnicities[spousalEthnicities
                .Keys
                .Order()
                .First()];
            ethnicities.AddRange(spousalEthnicityCandidates);
        }

        return ethnicities;
    }

    public List<Surname>? GetFamilySurnames()
    {
        if (!(RelationshipMap?.Count > 0))
        {
            return null;
        }

        var skipFamilial = CharacterName?.Surnames?.Any(x => !x.IsSpousal) == true;
        var skipSpousal = CharacterName?.Surnames?.Any(x => x.IsSpousal) == true;

        Dictionary<int, List<(Surname name, bool inheritedMatronym)>>? familySurnames = null;
        Dictionary<int, List<(Surname name, bool inheritedMatronym)>>? spousalSurnames = null;
        foreach (var relationship in RelationshipMap)
        {
            if (relationship.Type is null
                || relationship.Relative is null
                || relationship.Relative.CharacterName?.IsEmpty != false
                || !(relationship.Relative.CharacterName.Surnames?.Count > 0))
            {
                continue;
            }

            IEnumerable<(Surname name, bool inheritedMatronym)>? relativeFamilyNames = null;
            IEnumerable<(Surname name, bool inheritedMatronym)>? relativeSpousalNames = null;
            var familyConfidenceKey = 0;
            var spousalConfidenceKey = 0;

            if (relationship.Type != "child"
                && relationship.Type.EndsWith("child"))
            {
                if (!skipFamilial)
                {
                    relativeFamilyNames = relationship
                        .Relative
                        .CharacterName
                        .Surnames
                        .Where(x =>
                            !x.IsSpousal
                            && x.IsMatronymic == (Pronouns == Pronouns.SheHer))
                        .Select(x => (
                            new Surname(x.Name, Pronouns == Pronouns.SheHer),
                            false));
                    familyConfidenceKey = 4;
                }
            }
            else if (relationship.Type != "parent"
                && relationship.Type.EndsWith("parent"))
            {
                if (!skipFamilial)
                {
                    relativeFamilyNames = relationship
                        .Relative
                        .CharacterName
                        .Surnames
                        .Where(x => !x.IsSpousal)
                        .Select(x => (
                            new Surname(x.Name, relationship.Relative.Pronouns == Pronouns.SheHer),
                            x.IsMatronymic));
                    familyConfidenceKey = 1;
                }
            }
            else
            {
                switch (relationship.Type)
                {
                    case "child":
                        if (skipFamilial)
                        {
                            break;
                        }
                        relativeFamilyNames = relationship
                            .Relative
                            .CharacterName
                            .Surnames
                            .Where(x => !x.IsSpousal)
                            .Select(x => (
                                new Surname(x.Name, x.IsMatronymic),
                                x.IsMatronymic));
                        familyConfidenceKey = 3;
                        break;
                    case "parent":
                        if (skipFamilial)
                        {
                            break;
                        }
                        relativeFamilyNames = relationship
                            .Relative
                            .CharacterName
                            .Surnames
                            .Where(x => !x.IsSpousal)
                            .Select(x => (
                                new Surname(x.Name, relationship.Relative.Pronouns == Pronouns.SheHer),
                                x.IsMatronymic));
                        break;
                    case "sibling":
                        if (skipFamilial)
                        {
                            break;
                        }
                        relativeFamilyNames = relationship
                            .Relative
                            .CharacterName
                            .Surnames
                            .Where(x => !x.IsSpousal)
                            .Select(x => (
                                new Surname(x.Name, x.IsMatronymic),
                                false));
                        break;
                    case "spouse":
                        if (!skipFamilial)
                        {
                            relativeFamilyNames = relationship
                                .Relative
                                .CharacterName
                                .Surnames
                                .Where(x => x.IsSpousal)
                                .Select(x => (
                                    new Surname(x.Name, x.IsMatronymic),
                                    false));
                            familyConfidenceKey = Pronouns == Pronouns.HeHim
                                ? 1
                                : 2;
                        }

                        if (skipSpousal)
                        {
                            break;
                        }

                        relativeSpousalNames = relationship
                            .Relative
                            .CharacterName
                            .Surnames
                            .Where(x => !x.IsSpousal)
                            .Select(x => (
                                new Surname(x.Name, x.IsMatronymic, true),
                                x.IsMatronymic));
                        spousalConfidenceKey = relationship.Relative.Pronouns == Pronouns.HeHim
                            ? 0
                            : 2;
                        break;
                    case "pibling":
                    case "cousin":
                    case "nibling":
                        if (skipFamilial)
                        {
                            break;
                        }
                        relativeFamilyNames = relationship
                            .Relative
                            .CharacterName
                            .Surnames
                            .Where(x => !x.IsSpousal)
                            .Select(x => (
                                new Surname(x.Name, x.IsMatronymic),
                                x.IsMatronymic));
                        familyConfidenceKey = 3;
                        break;
                }
            }

            if (relativeFamilyNames?.Any() == true)
            {
                if (familySurnames?.TryGetValue(
                    familyConfidenceKey,
                    out var relativeSurnames) != true)
                {
                    relativeSurnames = [];
                    (familySurnames ??= [])[familyConfidenceKey] = relativeSurnames;
                }
                relativeSurnames!.AddRange(relativeFamilyNames);
            }

            if (relativeSpousalNames?.Any() == true)
            {
                if (spousalSurnames?.TryGetValue(
                    spousalConfidenceKey,
                    out var relativeSurnames) != true)
                {
                    relativeSurnames = [];
                    (spousalSurnames ??= [])[spousalConfidenceKey] = relativeSurnames;
                }
                relativeSurnames!.AddRange(relativeSpousalNames);
            }
        }

        List<Surname>? familyNames = null;

        if (familySurnames?.Count > 0)
        {
            familyNames = [];
            var familyNameCandidates = familySurnames[familySurnames
                .Keys
                .Order()
                .First()];
            if (familyNameCandidates.Any(x => !x.inheritedMatronym))
            {
                familyNames
                    .AddRange(familyNameCandidates
                    .Where(x => !x.inheritedMatronym)
                    .Select(x => x.name));
            }
            else
            {
                familyNames.AddRange(familyNameCandidates.Select(x => x.name));
            }
        }

        if (spousalSurnames?.Count > 0)
        {
            familyNames ??= [];
            var spousalNameCandidates = spousalSurnames[spousalSurnames
                .Keys
                .Order()
                .First()];
            if (spousalNameCandidates.Any(x => !x.inheritedMatronym))
            {
                familyNames
                    .AddRange(spousalNameCandidates
                    .Where(x => !x.inheritedMatronym)
                    .Select(x => x.name));
            }
            else
            {
                familyNames.AddRange(spousalNameCandidates.Select(x => x.name));
            }
        }

        return familyNames;
    }

    /// <summary>Serves as the default hash function.</summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(Id);

    public int GetMarriageChance() => this switch
    {
        { AgeYears: < 18 } => 0,
        { Pronouns: Pronouns.SheHer } => AgeYears switch
        {
            18 => 2,
            19 => 4,
            20 => 7,
            21 => 10,
            22 => 15,
            23 => 20,
            24 => 26,
            25 => 32,
            26 => 38,
            27 => 44,
            28 => 50,
            29 => 55,
            30 => 60,
            31 => 64,
            32 => 67,
            33 => 70,
            < 36 => 74,
            < 41 => 81,
            < 51 => 87,
            < 61 => 91,
            < 71 => 93,
            _ => 96,
        },
        { AgeYears: 18 } => 1,
        { AgeYears: 19 } => 2,
        { AgeYears: 20 } => 3,
        { AgeYears: 21 } => 5,
        { AgeYears: 22 } => 8,
        { AgeYears: 23 } => 12,
        { AgeYears: 24 } => 16,
        { AgeYears: 25 } => 21,
        { AgeYears: 26 } => 27,
        { AgeYears: 27 } => 33,
        { AgeYears: 28 } => 38,
        { AgeYears: 29 } => 44,
        { AgeYears: 30 } => 48,
        { AgeYears: 31 } => 54,
        { AgeYears: 32 } => 58,
        { AgeYears: 33 } => 61,
        { AgeYears: < 36 } => 67,
        { AgeYears: < 41 } => 76,
        { AgeYears: < 51 } => 83,
        { AgeYears: < 61 } => 90,
        { AgeYears: < 71 } => 93,
        _ => 96,
    };

    public NameGender GetNameGender() => Pronouns switch
    {
        Pronouns.SheHer => NameGender.Female,
        Pronouns.HeHim => NameGender.Male,
        _ => NameGender.Both,
    };

    public int GetSignificantOtherChance() => AgeYears switch
    {
        < 13 => 0,
        < 15 => 8,
        < 18 => 24,
        < 30 => 53,
        < 50 => 79,
        _ => 70,
    };

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

    public void InitializeCharacter(Story? story)
    {
        if (Initialized)
        {
            return;
        }
        SetDisplayAge(story);
        SetBirthdate(story);
        SetDisplayEthnicity();
        SetDisplayTraits();
        Initialized = true;
    }

    public async Task RandomizeAsync(DataService dataService, Story? story = null)
    {
        RandomizeAge(story);
        RandomizeEthnicities(dataService);
        RandomizeGender(dataService.Data, story);
        RandomizeTraits(dataService.Data, true);
        await RandomizeFullNameAsync(dataService);
    }

    public void RandomizeAge(Story? story = null)
    {
        var (min, mean, max) = GetAgeRange(story);
        min = min.HasValue ? Math.Max(0, min.Value) : 0;
        max ??= 105;

        double years;
        if (mean.HasValue)
        {
            var magnitude = int.MaxMagnitude(mean.Value - min.Value, max.Value - mean.Value);
            years = Randomizer.Instance
                .NormalDistributionSample(mean.Value, magnitude / 3.0, min, max);
        }
        else
        {
            years = Randomizer.Instance.NextDouble(min.Value, max.Value);
        }

        if (story?.Now.HasValue == true)
        {
            var birthDate = story.Now.Value.AddYears(-(int)Math.Floor(years));
            birthDate = birthDate.Subtract(TimeSpan.FromDays(Math.Floor(years % 1 * 365.25)));
            SetBirthdate(story, birthDate);
        }
        else if (years >= 1)
        {
            SetAge(story, (int)years, null, null);
        }
        else
        {
            var days = Math.Floor(years * 365.25);
            var months = 0;
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 28)
            {
                days -= 28;
                months++;
            }
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 30)
            {
                days -= 30;
                months++;
            }
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 30)
            {
                days -= 30;
                months++;
            }
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 30)
            {
                days -= 30;
                months++;
            }
            if (days > 31)
            {
                days -= 31;
                months++;
            }
            if (days > 30)
            {
                days -= 30;
                months++;
            }
            if (months >= 1)
            {
                SetAge(
                    story,
                    0,
                    months,
                    null);
            }
            else
            {
                SetAge(
                    story,
                    0,
                    0,
                    Math.Max(0, (int)Math.Floor(days)));
            }
        }
    }

    private void RandomizeLifespan(Story? story)
    {
        if (story is null
            || (!Birthdate.HasValue
            && !AgeYears.HasValue
            && !AgeMonths.HasValue
            && !AgeDays.HasValue))
        {
            return;
        }

        var (age, _, _) = GetAge(story.Now ?? DateTime.UtcNow);
        if (age > 105)
        {
            IsDeceased = true;
            return;
        }

        age ??= 0;

        var birthYear = Birthdate?.Year ?? (DateTime.UtcNow.Year - age.Value);
        double deathAge = birthYear switch
        {
            > 2009 => 80,
            > 1975 => 75,
            > 1955 => 70,
            > 1940 => 65,
            > 1920 => 60,
            > 1910 => 55,
            > 1890 => 50,
            > 1885 => 45,
            _ => 40,
        };
        if (Pronouns == Pronouns.HeHim)
        {
            deathAge -= double.Lerp(2.7, 5.4, 1 - Math.Min(1, age.Value / 65));
        }
        deathAge = Randomizer.Instance.NormalDistributionSample(deathAge, 3.5, 0, 105);
        if (deathAge < age.Value)
        {
            IsDeceased = true;
        }
    }

    public async Task RandomizeAndInitializeAsync(DataService dataService, Story? story = null)
    {
        Initialize();
        if (Pronouns == Pronouns.Other)
        {
            RandomizeGender(dataService.Data, story);
        }
        story?.ResetCharacterRelationshipMaps(dataService.Data);
        RandomizeAge(story);
        RandomizeLifespan(story);
        RandomizeEthnicities(dataService);
        RandomizeTraits(dataService.Data, true);
        await RandomizeFullNameAsync(dataService);
    }

    public void RandomizeEthnicities(DataService dataService)
    {
        var familyEthnicities = GetFamilyEthnicities();
        if (familyEthnicities?.Count > 0)
        {
            SetEthnicities(familyEthnicities);
        }
        else
        {
            SetEthnicities(dataService.GetRandomEthnicities());
        }
    }

    public async Task RandomizeFullNameAsync(DataService dataService)
    {
        CopyFamilySurnames();
        if (CharacterName?.Surnames?.Count(x => !x.IsSpousal) > 0)
        {
            await RandomizeNameAsync(dataService);
            return;
        }

        var (givenName, middleName, surname) = await dataService
            .GetRandomFullNameAsync(GetNameGender(), EthnicityPaths);
        if (!string.IsNullOrEmpty(givenName))
        {
            ((CharacterName ??= new()).GivenNames ??= []).Add(givenName);
            _characterShortName = null;
        }
        if (!string.IsNullOrEmpty(middleName))
        {
            ((CharacterName ??= new()).MiddleNames ??= []).Add(middleName);
        }
        if (!string.IsNullOrEmpty(surname))
        {
            ((CharacterName ??= new()).Surnames ??= []).Add(new(surname));
            _characterShortName = null;
        }
    }

    public void RandomizeGender(ScopData data, Story? story = null)
    {
        var chance = Randomizer.Instance.NextDouble();
        if (chance < 0.01)
        {
            Pronouns = Pronouns.TheyThem;
            Gender = "Non-binary";
        }
        else if (chance < 0.505)
        {
            Pronouns = Pronouns.SheHer;
            if (chance >= 0.495)
            {
                Gender = "Trans female";
            }
            else
            {
                Gender = "Female";
            }
        }
        else
        {
            Pronouns = Pronouns.HeHim;
            if (chance >= 0.99)
            {
                Gender = "Trans male";
            }
            else
            {
                Gender = "Male";
            }
        }
        story?.ResetCharacterRelationshipMaps(data);
    }

    public async Task RandomizeGivenNameAsync(DataService dataService)
    {
        var name = await dataService
            .GetRandomNameAsync(GetNameGender(), EthnicityPaths);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        (CharacterName ??= new()).GivenNames = [name];
        _characterShortName = null;
    }

    public async Task RandomizeMiddleNameAsync(DataService dataService)
    {
        var name = await dataService
            .GetRandomNameAsync(GetNameGender(), EthnicityPaths);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        (CharacterName ??= new()).MiddleNames = [name];
    }

    public async Task RandomizeNameAsync(DataService dataService)
    {
        await RandomizeGivenNameAsync(dataService);
        await RandomizeMiddleNameAsync(dataService);
    }

    public void RandomizeTraits(ScopData data, bool reset = true)
    {
        if (reset)
        {
            ClearTraits();
        }
        if (data.Traits is not null)
        {
            foreach (var trait in data.Traits)
            {
                trait.Randomize(this);
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
        InitializeCharacter(story);

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
        InitializeCharacter(story);

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
        InitializeCharacter(story);

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

    public void SetBirthdate(Story? story)
    {
        if (story?.Now.HasValue != true)
        {
            return;
        }

        InitializeCharacter(story);

        try
        {
            if (DisplayAgeYears.HasValue)
            {
                Birthdate = story.Now!.Value.AddYears(-DisplayAgeYears.Value);
            }
            if (DisplayAgeMonths.HasValue)
            {
                Birthdate = (Birthdate ?? story.Now!).Value.AddMonths(-DisplayAgeMonths.Value);
            }
            if (DisplayAgeDays.HasValue)
            {
                Birthdate = (Birthdate ?? story.Now!).Value.AddDays(-DisplayAgeDays.Value);
            }
        }
        catch
        {
            Birthdate = null;
        }
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

    private void InitializeRelationshipMap(ScopData data, List<Character> characters)
    {
        if (Relationships is null)
        {
            return;
        }

        foreach (var relationship in Relationships)
        {
            InitializeRelationship(relationship, data, characters);
        }
    }

    private void InitializeRelationship(
        Relationship relationship,
        ScopData data,
        List<Character> characters)
    {
        if (relationship.RelativeId is not null
            && RelationshipIds?.Contains(relationship.RelativeId) == true)
        {
            return;
        }

        (RelationshipMap ??= []).Add(relationship);

        if (RelationshipType.GetRelationshipType(data, relationship.Type) is RelationshipType type)
        {
            relationship.RelationshipType = type;
            relationship.EditedRelationshipType = type;
            relationship.Type = type.Name;
            relationship.EditedType = relationship.GetRelationshipTypeName();
        }
        else
        {
            relationship.EditedType = relationship.Type;
        }

        relationship.EditedRelativeGender = relationship.RelativeGender;

        relationship.Inverse = relationship.GetInverseRelationship(data, this);
        relationship.InverseType = relationship.Inverse.Type;
        relationship.EditedInverseType = relationship.Inverse.GetRelationshipTypeName();

        if (relationship.RelativeId is null)
        {
            return;
        }

        (RelationshipIds ??= []).Add(relationship.RelativeId);

        var relative = characters.Find(x => x.Id == relationship.RelativeId);
        if (relative is null)
        {
            relationship.EditedRelativeName = relationship.RelativeId;
            return;
        }

        relationship.Relative = relative;
        relationship.EditedRelative = relative;
        relationship.RelativeGender = relative.GetNameGender();
        relationship.EditedRelativeGender = relationship.RelativeGender;
        relationship.EditedRelativeName = relative.DisplayName;

        if (relative.RelationshipMap?.Any(x => x.RelativeId == Id) != true
            && relative.Relationships?.Any(x => x.RelativeId == Id) != true)
        {
            (relative.RelationshipMap ??= []).Add(relationship.GetInverseRelationship(data, this));
            (relative.RelationshipIds ??= []).Add(Id);
        }
    }

    public static bool operator ==(Character? left, Character? right) => EqualityComparer<Character>.Default.Equals(left, right);
    public static bool operator !=(Character? left, Character? right) => !(left == right);
}

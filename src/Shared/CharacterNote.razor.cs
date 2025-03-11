using Microsoft.AspNetCore.Components;
using Scop.Enums;
using Scop.Models;
using Scop.Pages;
using Scop.Services;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Randomize;

namespace Scop.Shared;

public partial class CharacterNote
{
    [Parameter] public Character? Character { get; set; }

    [Parameter] public Story? Story { get; set; }

    [CascadingParameter] public StoryPage? Page { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    private bool EditingAge { get; set; }

    private bool EditingEthnicity { get; set; }

    private bool EditingName { get; set; }

    private bool EditingGender { get; set; }

    private bool EditingTraits { get; set; }

    private string? NewCharacterSurname { get; set; }

    private string? NewEthnicityValue { get; set; }

    private DateTimeOffset? SelectedBirthdate { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var oldCharacter = Character;
        await base.SetParametersAsync(parameters);
        if (Character != oldCharacter)
        {
            Character?.InitializeCharacter(Story);
            EditingAge = false;
            EditingEthnicity = false;
            EditingGender = false;
            EditingName = false;
            EditingTraits = false;
            NewCharacterSurname = null;
            NewEthnicityValue = null;
            SelectedBirthdate = Character?.Birthdate;
        }
    }

    private static double Factorial(int value) => value <= 2 ? value : (value * Factorial(value - 1));

    private static Task<IEnumerable<KeyValuePair<string, object>>> GetGenders(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Strings.Genders
                .Select(x => new KeyValuePair<string, object>(x, x)));
        }

        var trimmed = value.Trim();

        return Task.FromResult(Strings.Genders
            .Where(x => !string.IsNullOrEmpty(x)
                && x.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(x => new KeyValuePair<string, object>(x!, x!)));
    }

    private static Task<IEnumerable<KeyValuePair<string, object>>> GetSuffixes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Strings.Suffixes
                .Select(x => new KeyValuePair<string, object>(x, x)));
        }

        var trimmed = value.Trim();

        return Task.FromResult(Strings.Suffixes
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(x => new KeyValuePair<string, object>(x, x)));
    }

    private static Task<IEnumerable<KeyValuePair<string, object>>> GetTitles(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Strings.Titles
                .Select(x => new KeyValuePair<string, object>(x, x)));
        }

        var trimmed = value.Trim();

        return Task.FromResult(Strings.Titles
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(x => new KeyValuePair<string, object>(x, x)));
    }

    private static double PoissonProbability(int events, double expectedValue)
        => Math.Pow(expectedValue, events) * Math.Exp(-expectedValue) / Factorial(events);

    private static int PoissonRandomValue(double expectedValue)
    {
        var events = 0;
        while (Randomizer.Instance.NextDouble() <= PoissonProbability(events, expectedValue))
        {
            events++;
        }
        return events;
    }

    private async Task GenerateChildrenAsync(
        string relationshipTypeName,
        RelationshipType characterRelationshipType,
        List<Character>? likelySecondParents,
        bool isCharacterChild = false)
    {
        if (Character is null
            || Story is null)
        {
            return;
        }

        var children = Character
            .RelationshipMap?
            .Where(x => x.RelationshipType?.Name == relationshipTypeName)
            .ToList();
        // If we have people in this relationship from another source (i.e. another relative defines
        // these relationships), do not add them directly, as this would create a confusing hierarchy.
        if (children?.Any(x => x.Synthetic) == true)
        {
            return;
        }

        var originYear = (Character.Birthdate?.Year ?? DateTime.UtcNow.Year)
            - (isCharacterChild
                ? Character.AgeYears
                : Math.Max(0, (Character.AgeYears ?? 0) - 24));
        var expectedSibship = originYear switch
        {
            > 1971 => 1.94,
            > 1966 => 2.5,
            > 1964 => 3,
            > 1961 => 3.5,
            > 1955 => 3.8,
            > 1952 => 3.5,
            _ => 3,
        };
        var childNumber = PoissonRandomValue(expectedSibship);
        if (childNumber <= 0)
        {
            return;
        }

        childNumber -= children?.Count ?? 0;
        if (isCharacterChild)
        {
            childNumber--;
        }

        if (childNumber <= 0)
        {
            return;
        }

        var childAges = children is null
            ? new HashSet<int>()
            : [.. children
                .Where(x => x.Relative?.AgeYears.HasValue == true)
                .Select(x => x.Relative!.AgeYears!.Value)];
        if (isCharacterChild && Character.AgeYears.HasValue)
        {
            childAges.Add(Character.AgeYears.Value);
        }
        var hasBaby = children?.Any(x
            => x.Relative?.AgeYears == 0
            || (x.Relative?.AgeYears.HasValue == false
                && (x.Relative.AgeMonths > 0
                    || x.Relative.AgeDays > 0))) == true
            || (isCharacterChild
                && (Character.AgeYears == 0
                || (!Character.AgeYears.HasValue
                    && (Character.AgeMonths > 0
                        || Character.AgeDays > 0))));

        var parentType = string.Equals(characterRelationshipType.Name, "parent", StringComparison.Ordinal)
            ? characterRelationshipType
            : RelationshipType.GetRelationshipType(DataService.Data, "parent");
        var secondParentIndex = 0;
        for (var i = 0; i < childNumber; i++)
        {
            var child = new Character();
            child.Relationships = [Relationship.FromType(DataService.Data, characterRelationshipType, Character, child)];
            if (parentType is not null && likelySecondParents?.Count > secondParentIndex)
            {
                child.Relationships.Add(Relationship.FromType(DataService.Data, parentType, likelySecondParents[secondParentIndex], child));
                if (secondParentIndex < likelySecondParents.Count - 1
                    && Randomizer.Instance.NextDouble() < 0.5)
                {
                    secondParentIndex++;
                }
            }
            (Character.Notes ??= []).Add(child);
            child.Parent = Character;
            await child.RandomizeAndInitializeAsync(DataService, Story);

            while (child.AgeYears.HasValue
                && child.AgeYears > 0
                && childAges.Contains(child.AgeYears.Value))
            {
                child.SetAge(
                    Story,
                    child.AgeYears.Value - 1,
                    child.AgeYears.Value == 1
                        ? Randomizer.Instance.Next(1, 12)
                        : null,
                    null);
            }
            if (child.AgeYears > 0)
            {
                childAges.Add(child.AgeYears.Value);
            }
            else if (child.AgeYears == 0)
            {
                if (hasBaby)
                {
                    Character.Notes.Remove(child);
                    if (Character.Notes.Count == 0)
                    {
                        Character.Notes = null;
                    }
                    Story.ResetCharacterRelationshipMaps(DataService.Data);
                }
                else
                {
                    hasBaby = true;
                }
                break;
            }
        }
    }

    private async Task GenerateParentsAsync(RelationshipType? spouseType, RelationshipType? exSpouseType)
    {
        if (Character is null)
        {
            return;
        }

        var childType = RelationshipType.GetRelationshipType(DataService.Data, "child");
        if (childType is null)
        {
            return;
        }

        var parents = Character
            .RelationshipMap?
            .Where(x => x.RelationshipType?.Name == "parent")
            .ToList();
        var parentCount = parents?.Count ?? 0;
        if (parentCount > 1)
        {
            return;
        }

        Character? parent = null;
        if (parentCount == 0)
        {
            parent = new Character();
            parent.Relationships = [Relationship.FromType(DataService.Data, childType, Character, parent)];
            (Character.Notes ??= []).Add(parent);
            parent.Parent = Character;
            await parent.RandomizeAndInitializeAsync(DataService, Story);
        }

        if (Randomizer.Instance.NextDouble() <= 0.08) // single parent
        {
            return;
        }

        var secondParent = new Character();
        secondParent.Relationships = [Relationship.FromType(DataService.Data, childType, Character, secondParent)];
        if ((parents?.FirstOrDefault()?.Relative ?? parent) is Character firstParent
            && (Randomizer.Instance.NextDouble() < 0.41 // chance of divorce
                || firstParent.AgeYears - Character.AgeYears < Randomizer.Instance.NormalDistributionSample(12, 9) // duration of marriage
                ? spouseType
                : exSpouseType) is RelationshipType parentSpouseRelationshipType)
        {
            secondParent.Relationships.Add(Relationship.FromType(
                DataService.Data,
                parentSpouseRelationshipType,
                firstParent,
                secondParent));
            secondParent.Pronouns = firstParent.Pronouns switch
            {
                Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.SheHer : Pronouns.HeHim,
                Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.HeHim : Pronouns.SheHer,
                _ => Pronouns.Other,
            };
            secondParent.Gender = secondParent.Pronouns switch
            {
                Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.005 ? "Trans female" : "Female",
                Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.005 ? "Trans male" : "Male",
                _ => null,
            };

            // If the first parent was feminine, and the new one masculine, and the first parent's
            // current surname is a familial name shared with the original character, mark it as a
            // spousal name and add a new maiden name.
            if (firstParent.Pronouns == Pronouns.SheHer
                && secondParent.Pronouns == Pronouns.HeHim
                && firstParent.CharacterName?.Surnames?.FirstOrDefault(x
                    => Character.CharacterName?.Surnames?.Any(y
                        => string.Equals(y.Name, x.Name, StringComparison.OrdinalIgnoreCase)) == true)
                is { Name.Length: > 0, IsSpousal: false } inheritedSurname)
            {
                firstParent.CharacterName.Surnames.Remove(inheritedSurname);
                firstParent.CharacterName.Surnames.Add(inheritedSurname with { IsSpousal = true });

                if (await DataService
                    .GetRandomSurnameAsync(Character.EthnicityPaths) is string name
                    && !string.IsNullOrEmpty(name))
                {
                    firstParent.CharacterName.Surnames.Add(new(name, false, false));
                }
            }
        }
        (Character.Notes ??= []).Add(secondParent);
        secondParent.Parent = Character;
        await secondParent.RandomizeAndInitializeAsync(DataService, Story);
    }

    private async Task<int> GenerateSpousesAsync(
        RelationshipType? spouseType,
        RelationshipType? exSpouseType,
        bool hasSignificantOther,
        List<Character> spouses)
    {
        if (Character is null)
        {
            return 0;
        }

        var spouseRelationships = Character
            .RelationshipMap?
            .Where(x => x.RelationshipType?.Name == "spouse")
            .ToList();
        if (spouseRelationships is not null)
        {
            spouses.AddRange(spouseRelationships
                .Where(x => x.Relative is not null)
                .Select(x => x.Relative!));
        }
        var spouseCount = spouseRelationships?.Count ?? 0;
        if (!hasSignificantOther
            || Randomizer.Instance.NextDouble() >= Character.GetMarriageChance())
        {
            return spouseCount;
        }

        var exSpouseRelationships = Character
            .RelationshipMap?
            .Where(x => x.RelationshipType?.Name == "ex-spouse")
            .ToList();
        if (exSpouseRelationships is not null)
        {
            spouses.AddRange(exSpouseRelationships
                .Where(x => x.Relative is not null)
                .Select(x => x.Relative!));
        }
        var exSpouseCount = exSpouseRelationships?.Count ?? 0;

        if (exSpouseType is not null
            && Randomizer.Instance.NextDouble() < 0.41 // chance of divorce
            && Character.AgeYears
                > Randomizer.Instance.LogisticDistributionSample(26, 14.7, 18, Character.AgeYears) // age at marriage
                + Randomizer.Instance.NormalDistributionSample(12, 9, 0) // duration of marriage
            && (exSpouseCount == 0
                || Randomizer.Instance.NextDouble() < Character.AgeYears switch // chance of having multiple ex-spouses
                {
                    < 25 => 0,
                    < 35 => 0.01,
                    < 45 => 0.1,
                    < 55 => 0.25,
                    _ => 0.33,
                }))
        {
            var exSpouse = new Character
            {
                Pronouns = Character.Pronouns switch
                {
                    Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.SheHer : Pronouns.HeHim,
                    Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.HeHim : Pronouns.SheHer,
                    _ => Pronouns.Other,
                }
            };
            (spouses ??= []).Add(exSpouse);
            exSpouse.Relationships = [Relationship.FromType(DataService.Data, exSpouseType, Character, exSpouse)];
            exSpouse.Gender = exSpouse.Pronouns switch
            {
                Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.005 ? "Trans female" : "Female",
                Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.005 ? "Trans male" : "Male",
                _ => null,
            };
            (Character.Notes ??= []).Add(exSpouse);
            exSpouse.Parent = Character;
            await exSpouse.RandomizeAndInitializeAsync(DataService, Story);
            exSpouseCount++;
        }

        if (spouseType is not null
            && spouseCount == 0
            && (exSpouseCount == 0
                || Randomizer.Instance.NextDouble() < spouseCount switch // chance of being remarried
                {
                    1 => Character.AgeYears switch // 2nd marriage
                    {
                        < 25 => 0.29,
                        < 35 => 0.43,
                        < 45 => 0.57,
                        < 55 => 0.63,
                        < 65 => 0.67,
                        _ => 0.5,
                    },
                    2 => Character.AgeYears switch // 3rd marriage
                    {
                        < 25 => 0,
                        < 35 => 0.01,
                        < 45 => 0.1,
                        < 55 => 0.25,
                        _ => 0.33,
                    },
                    _ => Character.AgeYears switch // 4th or greater
                    {
                        < 45 => 0,
                        < 55 => 0.25 / (spouseCount - 1),
                        _ => 0.33 / (spouseCount - 1),
                    }
                }))
        {
            var spouse = new Character
            {
                Pronouns = Character.Pronouns switch
                {
                    Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.SheHer : Pronouns.HeHim,
                    Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.HeHim : Pronouns.SheHer,
                    _ => Pronouns.Other,
                }
            };
            (spouses ??= []).Add(spouse);
            spouse.Relationships = [Relationship.FromType(DataService.Data, spouseType, Character, spouse)];
            spouse.Gender = spouse.Pronouns switch
            {
                Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.005 ? "Trans female" : "Female",
                Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.005 ? "Trans male" : "Male",
                _ => null,
            };
            (Character.Notes ??= []).Add(spouse);
            spouse.Parent = Character;
            await spouse.RandomizeAndInitializeAsync(DataService, Story);
            if (spouse.Pronouns == Pronouns.HeHim
                && Character.Pronouns == Pronouns.SheHer
                && Character.CharacterName?.Surnames?.Any(x => x.IsSpousal) != true)
            {
                var name = await DataService
                    .GetRandomSurnameAsync(spouse.EthnicityPaths);
                if (!string.IsNullOrEmpty(name))
                {
                    (spouse.CharacterName ??= new()).Surnames = [new(name, false, false)];

                    ((Character.CharacterName ??= new()).Surnames ??= [])
                        .Add(new(name, false, true));
                }
            }
            spouseCount++;
        }

        return spouseCount;
    }

    private async Task<Character?> GenerateSweetheartAsync(RelationshipType sweetheartType)
    {
        if (Character is null)
        {
            return null;
        }

        var sweetheartRelationships = Character
            .RelationshipMap?
            .Where(x => x.RelationshipType?.Name == "sweetheart")
            .ToList();
        if (sweetheartRelationships?.Count > 0)
        {
            return sweetheartRelationships
                .Select(x => x.Relative)
                .OrderByDescending(x => x?.AgeYears)
                .FirstOrDefault();
        }

        var sweetheart = new Character
        {
            Pronouns = Character.Pronouns switch
            {
                Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.SheHer : Pronouns.HeHim,
                Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.012 ? Pronouns.HeHim : Pronouns.SheHer,
                _ => Pronouns.Other,
            }
        };
        sweetheart.Relationships = [Relationship.FromType(DataService.Data, sweetheartType, Character, sweetheart)];
        sweetheart.Gender = sweetheart.Pronouns switch
        {
            Pronouns.SheHer => Randomizer.Instance.NextDouble() < 0.005 ? "Trans female" : "Female",
            Pronouns.HeHim => Randomizer.Instance.NextDouble() < 0.005 ? "Trans male" : "Male",
            _ => null,
        };
        (Character.Notes ??= []).Add(sweetheart);
        sweetheart.Parent = Character;
        await sweetheart.RandomizeAndInitializeAsync(DataService, Story);
        return sweetheart;
    }

    private Task<IEnumerable<KeyValuePair<string, object>>> GetCharacterNames(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Story?
                .AllCharacters()
                .Where(x => x != Character
                    && !string.IsNullOrEmpty(x.CharacterShortName))
                .Select(x => new KeyValuePair<string, object>(
                    x.CharacterShortName!,
                    x.CharacterShortName!))
                ?? []);
        }

        var trimmed = value.Trim();

        return Task.FromResult(Story?
            .AllCharacters()
            .Where(x => x != Character
                && x.CharacterShortName?
                .Contains(trimmed, StringComparison.InvariantCultureIgnoreCase) == true)
            .Select(x => new KeyValuePair<string, object>(
                x.CharacterShortName!,
                x.CharacterShortName!))
            ?? []);
    }

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetGivenNames(string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null || Character is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? []
                : [new KeyValuePair<string, object>(trimmed, trimmed)];
        }

        return (await DataService
            .GetNameListAsync(Character.GetNameGender(), Character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrEmpty(x)
                && (string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase)))
            .Distinct()
            .Select(x => new KeyValuePair<string, object>(x!, x!));
    }

    private Task<IEnumerable<KeyValuePair<string, object>>> GetRelationshipTypes(string? value)
        => Task.FromResult(GetRelationshipTypesInner(value));

    private IEnumerable<KeyValuePair<string, object>> GetRelationshipTypesInner(string? value)
    {
        if (DataService.Data.RelationshipTypes is null)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            foreach (var type in DataService
                .Data
                .RelationshipTypes
                .Where(x => !string.IsNullOrEmpty(x.Name)))
            {
                yield return new KeyValuePair<string, object>(type.Name!, type.Name!);
            }
            yield break;
        }

        var trimmed = value.Trim();

        foreach (var type in DataService
            .Data
            .RelationshipTypes
            .Where(x => !string.IsNullOrEmpty(x.Name)))
        {
            if (type.Name!.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                yield return new KeyValuePair<string, object>(type.Name, type.Name);
            }

            if (type.Names is null)
            {
                continue;
            }
            foreach (var name in type
                .Names
                .Where(x => x.Value.Contains(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new KeyValuePair<string, object>(name.Value, name.Value);
            }
        }
    }

    private Character? GetRelative(string? name) => !CharacterName.TryParse(name, out var relativeName)
        || relativeName.IsEmpty
        ? null
        : Story?
            .AllCharacters()
            .Select(x => (character: x, score: relativeName.GetMatchScore(x.CharacterName)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => Character?.Relationships?.Any(y => y.RelativeId == x.character.Id) == true
                ? 1
                : 0)
            .Select(x => x.character)
            .FirstOrDefault();

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetSurnames(string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null || Character is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? []
                : [new KeyValuePair<string, object>(trimmed, trimmed)];
        }

        return (await DataService
            .GetSurnameListAsync(Character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrEmpty(x)
                && (string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase)))
            .Distinct()
            .Select(x => new KeyValuePair<string, object>(x!, x!));
    }

    private async Task OnAddRandomEthnicityAsync(Ethnicity ethnicity)
    {
        if (Character is null)
        {
            return;
        }

        var path = ethnicity.Types is null
            ? ethnicity.Hierarchy
            : Ethnicity.GetRandomEthnicity(ethnicity.Types);
        if (path is not null)
        {
            (Character.EthnicityPaths ??= []).Add(path);
            await DataService.SaveAsync();
        }
    }

    private void OnAddRelationship()
    {
        if (Character is null)
        {
            return;
        }

        var relationship = new Relationship();
        (Character.Relationships ??= []).Add(relationship);
        (Character.RelationshipMap ??= []).Add(relationship);
    }

    private async Task OnAgeDaysChangedAsync(int? value)
    {
        if (Character is null
            || value == Character.DisplayAgeDays)
        {
            return;
        }

        Character.SetAgeDays(Story, value);
        SelectedBirthdate = Character.Birthdate;
        await DataService.SaveAsync();
    }

    private async Task OnAgeMonthsChangedAsync(int? value)
    {
        if (Character is null
            || value == Character.DisplayAgeMonths)
        {
            return;
        }

        Character.SetAgeMonths(Story, value);
        SelectedBirthdate = Character.Birthdate;
        await DataService.SaveAsync();
    }

    private async Task OnAgeYearsChangedAsync(int? value)
    {
        if (Character is null
            || value == Character.DisplayAgeYears)
        {
            return;
        }

        Character.SetAgeYears(Story, value);
        SelectedBirthdate = Character.Birthdate;
        await DataService.SaveAsync();
    }

    private async Task OnBirthdayChangedAsync(DateTimeOffset? value)
    {
        if (SelectedBirthdate == value)
        {
            return;
        }
        SelectedBirthdate = value;

        if (Character is null || Character.Birthdate == value)
        {
            return;
        }

        Character.SetBirthdate(Story, value);
        await DataService.SaveAsync();
    }

    private static void OnCancelEditingRelationship(Relationship relationship)
    {
        relationship.EditedInverseType = relationship.InverseType;
        relationship.EditedRelativeGender = relationship.Relative?.GetNameGender() ?? relationship.RelativeGender;
        relationship.EditedRelativeName = relationship.Relative?.DisplayName ?? relationship.RelativeId;
        relationship.EditedType = relationship.GetRelationshipTypeName();
        relationship.IsEditing = false;
    }

    private async Task OnChangeGenderAsync(string? value)
    {
        if (Character is null
            || string.Equals(value, Character.Gender, StringComparison.Ordinal))
        {
            return;
        }

        Character.Gender = value?.Trim();
        var gender = Character.Gender?.ToLowerInvariant() ?? string.Empty;
        if (gender.EndsWith("female")
            || gender.EndsWith("woman"))
        {
            Character.Pronouns = Pronouns.SheHer;
            Story?.ResetCharacterRelationshipMaps(DataService.Data);
        }
        else if (gender.EndsWith("male")
            || gender.EndsWith("man"))
        {
            Character.Pronouns = Pronouns.HeHim;
            Story?.ResetCharacterRelationshipMaps(DataService.Data);
        }
        await DataService.SaveAsync();
    }

    private async Task OnCharacterTitleChangedAsync(string? value)
    {
        if (Character is null
            || string.Equals(value, Character.CharacterName?.Title, StringComparison.Ordinal))
        {
            return;
        }

        (Character.CharacterName ??= new()).Title = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
        await DataService.SaveAsync();
    }

    private async Task OnCopyCharacterEthnicitiesAsync()
    {
        if (Character is null)
        {
            return;
        }

        var familyEthnicities = Character.GetFamilyEthnicities();
        if (familyEthnicities?.Count > 0)
        {
            Character.EthnicityPaths = familyEthnicities;
            await DataService.SaveAsync();
        }
    }

    private async Task OnCopyCharacterSurnameAsync()
    {
        if (Character is null)
        {
            return;
        }

        Character.CopyFamilySurnames();
        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private Task OnDeleteEthnicityAsync(Ethnicity ethnicity)
        => DataService.RemoveEthnicityAsync(ethnicity);

    private async Task OnDeleteRelationshipAsync(Relationship relationship)
    {
        if (Character is null)
        {
            return;
        }

        var removed = Character.Relationships?.Remove(relationship) ?? false;
        if (removed)
        {
            Story?.ResetCharacterRelationshipMaps(DataService.Data);
            StateHasChanged();
            await DataService.SaveAsync();
        }
    }

    private async Task OnDoneEditingRelationship(Relationship relationship)
    {
        if (Character is null)
        {
            relationship.IsEditing = false;
            return;
        }

        var change = false;

        var originalId = relationship.RelativeId;

        var relativeName = relationship.EditedRelativeName?.Trim();
        var relative = relationship.EditedRelative ?? GetRelative(relativeName);
        if (relative is null)
        {
            if (!string.Equals(relationship.RelativeId, relativeName, StringComparison.OrdinalIgnoreCase))
            {
                relationship.RelativeId = relativeName;
                relationship.Relative = null;
                change = true;
            }
        }
        else if (relationship.RelativeId != relative.Id)
        {
            relationship.RelativeId = relative.Id;
            relationship.Relative = relative;
            if (!string.IsNullOrEmpty(relative.CharacterShortName))
            {
                relationship.EditedRelativeName = relative.CharacterShortName;
            }
            change = true;
        }

        relationship.RelativeGender = relative?.GetNameGender() ?? relationship.EditedRelativeGender;
        relationship.EditedRelativeGender = relationship.RelativeGender;

        bool typeChange;
        if (relationship.EditedRelationshipType is not null)
        {
            typeChange = relationship.EditedRelationshipType != relationship.RelationshipType;
            if (typeChange)
            {
                relationship.RelationshipType = relationship.EditedRelationshipType;
                relationship.Type = relationship.RelationshipType.Name;
                change = true;
            }
        }
        else
        {
            var type = relationship.EditedType?.Trim().ToLowerInvariant();
            typeChange = !string.Equals(relationship.Type, type, StringComparison.OrdinalIgnoreCase);
            if (typeChange)
            {
                relationship.RelationshipType = RelationshipType.GetRelationshipType(DataService.Data, type);
                relationship.EditedRelationshipType = relationship.RelationshipType;
                relationship.Type = relationship.RelationshipType?.Name ?? type;
                change = true;
            }
        }

        if (typeChange)
        {
            var typeName = relationship.GetRelationshipTypeName();
            if (!string.IsNullOrEmpty(typeName)
                && !string.Equals(relationship.Type, typeName, StringComparison.OrdinalIgnoreCase))
            {
                relationship.Type = typeName;
                change = true;
            }
        }

        var inverse = relationship.GetInverseRelationship(DataService.Data, Character);
        if (inverse is not null
            && relationship.Inverse != inverse)
        {
            relationship.Inverse = inverse;
            relationship.InverseType = inverse.GetRelationshipTypeName();
            relationship.EditedInverseType = relationship.InverseType;
            change = true;
        }

        OnCancelEditingRelationship(relationship);

        if (change)
        {
            if (!string.IsNullOrEmpty(originalId))
            {
                Character.Relationships?.RemoveAll(x => x.RelativeId == originalId);
            }

            if (!string.IsNullOrEmpty(relationship.RelativeId))
            {
                Character.Relationships?.RemoveAll(x => x.RelativeId == relationship.RelativeId);
            }
            (Character.Relationships ??= []).Add(relationship);

            Story?.ResetCharacterRelationshipMaps(DataService.Data);
            StateHasChanged();
            await DataService.SaveAsync();
        }

        relationship.IsEditing = false;
    }

    private void OnEditAge()
    {
        EditingAge = true;
        EditingEthnicity = false;
        EditingGender = false;
        EditingName = false;
        EditingTraits = false;
    }

    private void OnEditEthnicity()
    {
        EditingAge = false;
        EditingEthnicity = true;
        EditingGender = false;
        EditingName = false;
        EditingTraits = false;
    }

    private async Task OnEditEthnicityAsync(Ethnicity ethnicity, string? newValue)
    {
        ethnicity.IsEditing = false;
        if (string.Equals(ethnicity.Type, newValue, StringComparison.Ordinal))
        {
            return;
        }
        await DataService.EditEthnicityAsync(ethnicity, newValue);
    }

    private void OnEditRelationship(Relationship relationship)
    {
        if (Character?.RelationshipMap is null)
        {
            return;
        }
        var editing = Character.RelationshipMap.FirstOrDefault(x => x.IsEditing);
        if (editing is not null)
        {
            OnCancelEditingRelationship(editing);
        }
        relationship.IsEditing = true;
    }

    private void OnEditGender()
    {
        EditingAge = false;
        EditingEthnicity = false;
        EditingGender = true;
        EditingName = false;
        EditingTraits = false;
    }

    private void OnEditName()
    {
        EditingAge = false;
        EditingEthnicity = false;
        EditingGender = false;
        EditingName = true;
        EditingTraits = false;
    }

    private void OnEditTraits()
    {
        EditingAge = false;
        EditingEthnicity = false;
        EditingGender = false;
        EditingName = false;
        EditingTraits = true;
    }

    private async Task OnEthnicitySelectAsync(bool value, Ethnicity? ethnicity)
    {
        if (Character is not null && ethnicity is not null)
        {
            Character.SelectEthnicity(ethnicity, value);
            await DataService.SaveAsync();
        }
    }

    private async Task OnGenerateFamilyAsync()
    {
        if (Character is null
            || Story is null)
        {
            return;
        }

        var spouseType = RelationshipType.GetRelationshipType(DataService.Data, "spouse");
        var exSpouseType = spouseType?.GetExType();

        await GenerateParentsAsync(spouseType, exSpouseType);

        if (RelationshipType.GetRelationshipType(DataService.Data, "sibling") is RelationshipType siblingType)
        {
            await GenerateChildrenAsync("sibling", siblingType, null, true);
        }

        var hasSignificantOther = Randomizer.Instance.NextDouble() < Character.GetSignificantOtherChance();
        List<Character> spouses = [];
        var spouseCount = await GenerateSpousesAsync(
            spouseType,
            exSpouseType,
            hasSignificantOther,
            spouses);

        Character? sweetheart = null;
        if (hasSignificantOther
            && spouseCount == 0
            && RelationshipType.GetRelationshipType(DataService.Data, "sweetheart") is RelationshipType sweetheartType)
        {
            sweetheart = await GenerateSweetheartAsync(sweetheartType);
        }

        if (RelationshipType.GetRelationshipType(DataService.Data, "parent") is RelationshipType parentType
            && (spouseCount > 0
            || (Character.AgeYears >= 16
            && Randomizer.Instance.NextDouble() <= 0.08))) // single parent
        {
            List<Character>? likelyCoparents = null;
            if (spouses.Any(x => x.Pronouns != Character.Pronouns))
            {
                likelyCoparents = [.. spouses
                    .Where(x => x.Pronouns != Character.Pronouns)
                    .OrderByDescending(x => x.AgeYears)];
            }
            else if (spouses.Count > 0)
            {
                likelyCoparents = [.. spouses
                    .OrderByDescending(x => x.AgeYears)];
            }
            else if (sweetheart is not null)
            {
                likelyCoparents = [sweetheart];
            }
            await GenerateChildrenAsync("child", parentType, likelyCoparents);
        }

        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnGivenNameChangeAsync(string? value)
    {
        if (Character is null
            || string.Equals(value, Character.CharacterName?.GivenName, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (Character.CharacterName is null)
            {
                return;
            }

            Character.CharacterName.GivenNames = null;
            Character._characterShortName = null;
            if (Character.CharacterName.IsDefault)
            {
                Character.CharacterName = null;
            }
            Page?.SelectNoteAsync(Character);

            return;
        }

        (Character.CharacterName ??= new()).GivenNames = [.. value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        Character._characterShortName = null;

        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnMiddleNameChangeAsync(string? value)
    {
        if (Character is null
            || string.Equals(value, Character.CharacterName?.MiddleName, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (Character.CharacterName is null)
            {
                return;
            }

            Character.CharacterName.MiddleNames = null;
            if (Character.CharacterName.IsDefault)
            {
                Character.CharacterName = null;
            }

            return;
        }

        (Character.CharacterName ??= new()).MiddleNames = [.. value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        await DataService.SaveAsync();
    }

    private async Task OnNewCharacterSurnameAsync(string? value)
    {
        if (Character is null)
        {
            return;
        }

        NewCharacterSurname = value;
        if (string.IsNullOrWhiteSpace(NewCharacterSurname))
        {
            return;
        }

        ((Character.CharacterName ??= new()).Surnames ??= []).Add(new(NewCharacterSurname.Trim()));
        Character._characterShortName = null;
        NewCharacterSurname = null;
        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnNewEthnicityAsync(string? newValue)
    {
        NewEthnicityValue = newValue;
        if (Story is null
            || string.IsNullOrEmpty(NewEthnicityValue))
        {
            return;
        }

        var trimmed = NewEthnicityValue.Trim();
        if (DataService
            .Data
            .Ethnicities?
            .Any(x => string.Equals(x.Type, trimmed, StringComparison.OrdinalIgnoreCase)) == true)
        {
            NewEthnicityValue = string.Empty;
            return;
        }
        var newEthnicity = new Ethnicity()
        {
            Hierarchy = [trimmed],
            Type = trimmed,
        };
        NewEthnicityValue = string.Empty;
        await DataService.AddEthnicityAsync(newEthnicity);
    }

    private async Task OnNewEthnicityAsync(Ethnicity parent, string? newValue)
    {
        parent.NewEthnicityValue = newValue;
        if (string.IsNullOrEmpty(parent.NewEthnicityValue))
        {
            return;
        }

        parent.NewEthnicityValue = string.Empty;
        var trimmed = parent.NewEthnicityValue.Trim();
        if (parent
            .Types?
            .Any(x => string.Equals(x.Type, trimmed, StringComparison.OrdinalIgnoreCase)) != true)
        {
            await DataService.AddEthnicityAsync(parent, trimmed);
        }
    }

    private async Task OnPronounsChangedAsync(Pronouns value)
    {
        if (Character is not null && Character.Pronouns != value)
        {
            Character.Pronouns = value;
            await DataService.SaveAsync();
        }
    }

    private async Task OnRandomizeCharacterAsync()
    {
        if (Character is not null)
        {
            await Character.RandomizeAsync(DataService, Story);
            await DataService.SaveAsync();
            Page?.SelectNoteAsync(Character);
        }
    }

    private async Task OnRandomizeCharacterAgeAsync(bool deferSave = false)
    {
        if (Character is null)
        {
            return;
        }

        Character.RandomizeAge(Story);

        if (!deferSave)
        {
            await DataService.SaveAsync();
        }
    }

    private async Task OnRandomizeCharacterEthnicitiesAsync(bool deferSave = false)
    {
        if (Character is null)
        {
            return;
        }

        Character.RandomizeEthnicities(DataService);

        if (!deferSave)
        {
            await DataService.SaveAsync();
        }
    }

    private async Task OnRandomizeCharacterFullNameAsync()
    {
        if (Character is null)
        {
            return;
        }

        await Character.RandomizeFullNameAsync(DataService);
        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnRandomizeCharacterGenderAsync(bool deferSave = false)
    {
        if (Character is null)
        {
            return;
        }

        Character.RandomizeGender(DataService.Data, Story);

        if (!deferSave)
        {
            await DataService.SaveAsync();
        }
    }

    private async Task OnRandomizeCharacterGivenNameAsync(bool deferSave = false)
    {
        if (Character is null)
        {
            return;
        }

        await Character.RandomizeGivenNameAsync(DataService);
        if (!deferSave)
        {
            await DataService.SaveAsync();
        }
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnRandomizeCharacterMiddleNameAsync()
    {
        if (Character is null)
        {
            return;
        }

        await Character.RandomizeMiddleNameAsync(DataService);
        await DataService.SaveAsync();
    }

    private async Task OnRandomizeCharacterSurnameAsync(int? index = null)
    {
        if (Character is null)
        {
            return;
        }

        var name = await DataService
            .GetRandomSurnameAsync(Character.EthnicityPaths);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        if (index.HasValue)
        {
            if (Character.CharacterName?.Surnames?.Count > index)
            {
                Character.CharacterName.Surnames[index.Value] = Character.CharacterName.Surnames[index.Value] with { Name = name };
            }
            else
            {
                ((Character.CharacterName ??= new()).Surnames ??= []).Add(new(name));
            }
        }
        else
        {
            (Character.CharacterName ??= new()).Surnames = [new(name)];
        }
        Character._characterShortName = null;
        await DataService.SaveAsync();
        Page?.SelectNoteAsync(Character);
    }

    private async Task OnRegenerateFamilyAsync()
    {
        if (Character is null)
        {
            return;
        }

        Character.Notes?.RemoveAll(x => x is Character);
        Story?.ResetCharacterRelationshipMaps(DataService.Data);
        await OnGenerateFamilyAsync();
    }

    private void OnRelationshipTypeChange(Relationship relationship, string? value)
    {
        if (string.Equals(relationship.EditedType, value, StringComparison.Ordinal))
        {
            return;
        }

        relationship.EditedType = value;

        relationship.EditedRelationshipType = RelationshipType.GetRelationshipType(DataService.Data, value);
        if (relationship.EditedRelationshipType is null)
        {
            return;
        }

        var newTypeName = relationship.GetRelationshipTypeName();
        if (newTypeName is not null)
        {
            relationship.EditedType = newTypeName;
        }

        var newInverseTypeName = relationship
            .GetInverseRelationship(DataService.Data, Character)
            .GetRelationshipTypeName(relative: Character);
        if (newInverseTypeName is not null)
        {
            relationship.EditedInverseType = newInverseTypeName;
        }
    }

    private void OnRelationshipInverseTypeChange(Relationship relationship, string? value)
    {
        if (string.Equals(relationship.EditedInverseType, value, StringComparison.Ordinal))
        {
            return;
        }

        relationship.EditedInverseType = value;

        var inverseRelationship = RelationshipType.GetRelationshipType(DataService.Data, value);
        if (inverseRelationship is null)
        {
            return;
        }

        var newInverseTypeName = relationship.GetRelationshipTypeName(inverseRelationship, Character);
        if (newInverseTypeName is not null)
        {
            relationship.EditedInverseType = newInverseTypeName;
        }

        if (relationship.EditedRelationshipType is not null
            || !string.IsNullOrWhiteSpace(relationship.EditedType))
        {
            return;
        }

        var reverse = inverseRelationship.InverseName ?? inverseRelationship.Name;
        relationship.EditedRelationshipType = RelationshipType.GetRelationshipType(DataService.Data, reverse);
        relationship.EditedType = relationship.EditedRelationshipType?.Name ?? reverse;
    }

    private void OnRelativeNameChange(Relationship relationship, string? value)
    {
        if (string.Equals(relationship.EditedRelativeName, value, StringComparison.Ordinal))
        {
            return;
        }

        relationship.EditedRelativeName = value;

        relationship.EditedRelative = GetRelative(value);
        if (relationship.EditedRelative is null)
        {
            return;
        }

        relationship.EditedRelativeName = relationship.EditedRelative.DisplayName;

        var newTypeName = relationship.GetRelationshipTypeName();
        if (newTypeName is not null)
        {
            relationship.EditedType = newTypeName;
        }

        var newInverseTypeName = relationship
            .GetInverseRelationship(DataService.Data, Character)
            .GetRelationshipTypeName(relative: Character);
        if (newInverseTypeName is not null)
        {
            relationship.EditedInverseType = newInverseTypeName;
        }
    }

    private async Task OnSuffixChangeAsync(string? value)
    {
        if (Character is null
            || string.Equals(value, Character.CharacterName?.Suffix))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            if (Character.CharacterName is null)
            {
                return;
            }

            Character.CharacterName.Suffixes = null;
            Character._characterShortName = null;
            if (Character.CharacterName.IsDefault)
            {
                Character.CharacterName = null;
            }

            return;
        }

        (Character.CharacterName ??= new()).Suffixes = [.. value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        Character._characterShortName = null;

        await DataService.SaveAsync();
    }

    private async Task OnSurnameChangeAsync(int index, string? value)
    {
        if (Character is null
            || !(Character.CharacterName?.Surnames?.Count > 0)
            || string.Equals(Character.CharacterName.Surnames[index].Name, value, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            Character.CharacterName.Surnames.RemoveAt(index);
            if (Character.CharacterName.Surnames.Count == 0)
            {
                Character.CharacterName.Surnames = null;
            }
            if (Character.CharacterName.IsDefault)
            {
                Character.CharacterName = null;
            }
            return;
        }
        else
        {
            Character.CharacterName.Surnames[index] = Character.CharacterName.Surnames[index] with { Name = value.Trim() };
        }

        await DataService.SaveAsync();
    }

    private async Task OnSurnameMatronymicChangeAsync(int index, bool value)
    {
        if (Character is null
            || !(Character.CharacterName?.Surnames?.Count > 0)
            || Character.CharacterName.Surnames[index].IsMatronymic == value)
        {
            return;
        }

        Character.CharacterName.Surnames[index] = Character.CharacterName.Surnames[index] with { IsMatronymic = value };

        await DataService.SaveAsync();
    }

    private async Task OnSurnameSpousalChangeAsync(int index, bool value)
    {
        if (Character is null
            || !(Character.CharacterName?.Surnames?.Count > 0)
            || Character.CharacterName.Surnames[index].IsSpousal == value)
        {
            return;
        }

        Character.CharacterName.Surnames[index] = Character.CharacterName.Surnames[index] with { IsSpousal = value };

        await DataService.SaveAsync();
    }
}
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Randomize;

namespace Scop.Shared;

public partial class CharacterNote
{
    [Parameter] public Character? Character { get; set; }

    [Parameter] public DateTimeOffset? SelectedBirthdate { get; set; }

    [Parameter] public EventCallback<DateTimeOffset?> SelectedBirthdateChanged { get; set; }

    [Parameter] public INote? SelectedNote { get; set; }

    [Parameter] public Story? Story { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;

    private bool EthnicitiesVisible { get; set; }

    private string? NewCharacterName { get; set; }

    private string? NewCharacterSurname { get; set; }

    private string? NewEthnicityValue { get; set; }

    private string? NewTraitValue { get; set; }

    private bool TraitsVisible { get; set; }

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

    private static Task<IEnumerable<KeyValuePair<string, object>>> GetRelationshipTypes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Strings.RelationshipTypes
                .Select(x => new KeyValuePair<string, object>(x, x)));
        }

        var trimmed = value.Trim();

        return Task.FromResult(Strings.RelationshipTypes
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(x => new KeyValuePair<string, object>(x, x)));
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

    private static void OnAddRelationship(Character character)
    {
        var relationship = new Relationship();
        (character.Relationships ??= new()).Add(relationship);
        (character.RelationshipMap ??= new()).Add(relationship);
    }

    private static void OnEditEthnicity(Ethnicity ethnicity, string? newValue)
    {
        ethnicity.Type = newValue?.Trim();
        ethnicity.IsEditing = false;
    }

    private Task<IEnumerable<KeyValuePair<string, object>>> GetCharacterNames(Character character, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(Story?
                .AllCharacters()
                .Where(x => x != character
                    && !string.IsNullOrEmpty(x.CharacterName))
                .Select(x => new KeyValuePair<string, object>(
                    x.CharacterName!,
                    x.CharacterName!))
                ?? Enumerable.Empty<KeyValuePair<string, object>>());
        }

        var trimmed = value.Trim();

        return Task.FromResult(Story?.Notes?
            .OfType<Character>()
            .Where(x => x != character
                    && !string.IsNullOrEmpty(x.CharacterName)
                    && x.CharacterName
                        .Contains(trimmed, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => new KeyValuePair<string, object>(x.CharacterName!, x.CharacterName!))
            ?? Enumerable.Empty<KeyValuePair<string, object>>());
    }

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetGivenNames(Character character, string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? Enumerable.Empty<KeyValuePair<string, object>>()
                : new[] { new KeyValuePair<string, object>(trimmed, trimmed) };
        }

        return (await DataService
            .GetNameListAsync(character.GetNameGender(), character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrEmpty(x)
                && (string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase)))
            .Distinct()
            .Select(x => new KeyValuePair<string, object>(x!, x!));
    }

    private async Task<IEnumerable<KeyValuePair<string, object>>> GetSurnames(Character character, string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? Enumerable.Empty<KeyValuePair<string, object>>()
                : new[] { new KeyValuePair<string, object>(trimmed, trimmed) };
        }

        return (await DataService
            .GetSurnameListAsync(character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrEmpty(x)
                && (string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase)))
            .Distinct()
            .Select(x => new KeyValuePair<string, object>(x!, x!));
    }

    private async Task OnAddRandomEthnicityAsync(Ethnicity ethnicity, Character character)
    {
        var path = ethnicity.Types is null
            ? ethnicity.Hierarchy
            : Ethnicity.GetRandomEthnicity(ethnicity.Types);
        if (path is not null)
        {
            (character.EthnicityPaths ??= new()).Add(path);
            await OnChangeAsync();
        }
    }

    private async Task OnAgeDaysChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeDays)
        {
            character.SetAgeDays(Story, value);
            SelectedBirthdate = character.Birthdate;
            await SelectedBirthdateChanged.InvokeAsync(SelectedBirthdate);
            await OnChangeAsync();
        }
    }

    private async Task OnAgeMonthsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeMonths)
        {
            character.SetAgeMonths(Story, value);
            SelectedBirthdate = character.Birthdate;
            await SelectedBirthdateChanged.InvokeAsync(SelectedBirthdate);
            await OnChangeAsync();
        }
    }

    private async Task OnAgeYearsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeYears)
        {
            character.SetAgeYears(Story, value);
            SelectedBirthdate = character.Birthdate;
            await SelectedBirthdateChanged.InvokeAsync(SelectedBirthdate);
            await OnChangeAsync();
        }
    }

    private async Task OnBirthdayChangedAsync(DateTimeOffset? value)
    {
        SelectedBirthdate = value;
        await SelectedBirthdateChanged.InvokeAsync(SelectedBirthdate);
        if (SelectedNote is not Character character)
        {
            return;
        }
        if (character.Birthdate != value)
        {
            character.SetBirthdate(Story, value);
            await OnChangeAsync();
        }
    }

    private static void OnCancelEditingRelationship(Relationship relationship)
    {
        relationship.EditedInverseType = relationship.InverseType;
        relationship.EditedRelationshipName = relationship.RelationshipName;
        relationship.EditedRelativeName = relationship.Relative?.CharacterName ?? relationship.RelativeName;
        relationship.EditedType = relationship.Type;
        relationship.IsEditing = false;
    }

    private Task OnChangeAsync() => DataService.SaveAsync();

    private async Task OnChangeGenderAsync(Character character, string? value)
    {
        character.Gender = value?.Trim();
        var gender = character.Gender?.ToLowerInvariant() ?? string.Empty;
        if (gender.EndsWith("female")
            || gender.EndsWith("woman"))
        {
            character.Pronouns = Pronouns.SheHer;
            Story?.ResetCharacterRelationshipMaps();
        }
        else if (gender.EndsWith("male")
            || gender.EndsWith("man"))
        {
            character.Pronouns = Pronouns.HeHim;
            Story?.ResetCharacterRelationshipMaps();
        }
        await OnChangeAsync();
    }

    private async Task OnCharacterSuffixChangedAsync(Character character, string? value)
    {
        character.Suffix = value;
        await DataService.SaveAsync();
    }

    private async Task OnCharacterTitleChangedAsync(Character character, string? value)
    {
        character.Title = value;
        await DataService.SaveAsync();
    }

    private async Task OnCopyCharacterEthnicitiesAsync(Character character)
    {
        var familyEthnicities = character.GetFamilyEthnicities();
        if (familyEthnicities.Count > 0)
        {
            character.EthnicityPaths = familyEthnicities;
            await OnChangeAsync();
        }
    }

    private async Task OnCopyCharacterSurnameAsync(Character character)
    {
        var familySurnames = character.GetFamilySurnames();
        if (familySurnames.Count > 0)
        {
            character.Surnames = familySurnames;
            await OnChangeAsync();
        }
    }

    private async Task OnDeleteEthnicityAsync(Ethnicity ethnicity)
    {
        ethnicity.Parent?.Types?.Remove(ethnicity);
        var top = ethnicity;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        static bool HasUserDefined(Ethnicity ethnicity)
        {
            if (ethnicity.UserDefined)
            {
                return true;
            }
            if (ethnicity.Types is not null)
            {
                foreach (var child in ethnicity.Types)
                {
                    if (HasUserDefined(child))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        if (DataService?.Data.Ethnicities is not null
            && !HasUserDefined(top))
        {
            DataService.Data.Ethnicities.Remove(top);
        }

        await OnChangeAsync();
    }

    private async Task OnDeleteRelationshipAsync(Character character, Relationship relationship)
    {
        var removed = character.Relationships?.Remove(relationship) ?? false;
        if (removed)
        {
            Story?.ResetCharacterRelationshipMaps();
            await OnChangeAsync();
        }
    }

    private async Task OnDeleteTraitAsync(Trait trait)
    {
        trait.Parent?.Children?.Remove(trait);
        var top = trait;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        static bool HasUserDefined(Trait trait)
        {
            if (trait.UserDefined)
            {
                return true;
            }
            if (trait.Children is not null)
            {
                foreach (var child in trait.Children)
                {
                    if (HasUserDefined(child))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        if (DataService?.Data.Traits is not null
            && !HasUserDefined(top))
        {
            DataService.Data.Traits.Remove(top);
        }

        await OnChangeAsync();
    }

    private async Task OnDoneEditingRelationship(Relationship relationship, Character character)
    {
        var change = false;

        var originalId = relationship.Id;
        var originalName = relationship.RelativeName;

        var name = relationship.EditedRelativeName?.Trim();
        var relative = Story?
            .AllCharacters()
            .Select(x => (character: x, score: x.GetNameMatchScore(name)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => character.Relationships?.Any(y => y.Id == x.character.Id) == true
                ? 1
                : 0)
            .Select(x => x.character)
            .FirstOrDefault();
        if (relative is null)
        {
            if (!string.IsNullOrEmpty(relationship.Id)
                || !string.Equals(relationship.RelativeName, name, StringComparison.OrdinalIgnoreCase))
            {
                relationship.Id = null;
                relationship.Relative = null;
                relationship.RelativeName = name;
                change = true;
            }
        }
        else if (relationship.Id != relative.Id)
        {
            relationship.Id = relative.Id;
            relationship.Relative = relative;
            relationship.RelativeName = null;
            change = true;
        }

        var type = relationship.EditedType?.Trim().ToLowerInvariant();
        var typeChange = !string.Equals(relationship.Type, type, StringComparison.OrdinalIgnoreCase);
        if (typeChange)
        {
            relationship.Type = type;
            change = true;
        }

        var typeName = relationship.EditedRelationshipName?.Trim().ToLowerInvariant();
        if (typeChange
            && !string.IsNullOrEmpty(relationship.Type)
            && string.Equals(relationship.RelationshipName, typeName, StringComparison.OrdinalIgnoreCase))
        {
            typeName = relationship.Relative is null
                ? Character.GetRelationshipName(relationship.Type, NameGender.None)
                : relationship.Relative.GetRelationshipName(relationship.Type);
        }

        if (string.Equals(relationship.Type, typeName, StringComparison.OrdinalIgnoreCase))
        {
            typeName = null;
        }
        if (!string.Equals(relationship.RelationshipName, typeName, StringComparison.OrdinalIgnoreCase))
        {
            relationship.RelationshipName = typeName;
            change = true;
        }

        var inverse = relationship.EditedInverseType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(inverse))
        {
            if (relationship.InverseType is not null)
            {
                relationship.InverseType = null;
                change = true;
            }
        }
        else if (!string.Equals(relationship.InverseType, inverse, StringComparison.OrdinalIgnoreCase))
        {
            relationship.InverseType = inverse;
            change = true;
        }

        OnCancelEditingRelationship(relationship);

        if (change)
        {
            if (!string.IsNullOrEmpty(originalId))
            {
                character.Relationships?.RemoveAll(x => x.Id == originalId);
            }
            else
            {
                character.Relationships?.RemoveAll(x => x.Id is null && string.Equals(x.RelativeName, originalName, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!string.IsNullOrEmpty(relationship.Id))
            {
                character.Relationships?.RemoveAll(x => x.Id == relationship.Id);
            }
            else
            {
                character.Relationships?.RemoveAll(x => x.Id is null && string.Equals(x.RelativeName, relationship.RelativeName, StringComparison.InvariantCultureIgnoreCase));
            }
            (character.Relationships ??= new()).Add(relationship);

            Story?.ResetCharacterRelationshipMaps();
            await OnChangeAsync();
        }
    }

    private async Task OnEditTraitAsync(Trait trait)
    {
        if (DialogService is null)
        {
            return;
        }

        var parameters = new DialogParameters();
        if (trait is not null)
        {
            parameters.Add(nameof(TraitDialog.Trait), trait);
        }
        var dialog = DialogService.Show<TraitDialog>("Trait", parameters);
        await dialog.Result;
        await OnChangeAsync();
    }

    private async Task OnEthnicitySelectAsync(bool value, Ethnicity? ethnicity, Character character)
    {
        if (ethnicity is not null)
        {
            character.SelectEthnicity(ethnicity, value);
            await OnChangeAsync();
        }
    }

    private async Task OnNameChangeAsync(Character character, int index, string? value)
    {
        if (character.Names is null
            || character.Names.Count <= index
            || string.Equals(character.Names[index], value, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            character.Names.RemoveAt(index);
            if (character.Names.Count == 0)
            {
                character.Names = null;
            }
            return;
        }
        else
        {
            character.Names[index] = value;
        }

        await OnChangeAsync();
    }

    private async Task OnNewCharacterNameAsync(Character character, string? value)
    {
        NewCharacterName = value;
        if (string.IsNullOrWhiteSpace(NewCharacterName))
        {
            return;
        }

        (character.Names ??= new()).Add(NewCharacterName.Trim());
        NewCharacterName = null;
        await OnChangeAsync();
    }

    private async Task OnNewCharacterSurnameAsync(Character character, string? value)
    {
        NewCharacterSurname = value;
        if (string.IsNullOrWhiteSpace(NewCharacterSurname))
        {
            return;
        }

        (character.Surnames ??= new()).Add(NewCharacterSurname.Trim());
        NewCharacterSurname = null;
        await OnChangeAsync();
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
            .Ethnicities
            .Any(x => string.Equals(x.Type, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            NewEthnicityValue = string.Empty;
            return;
        }
        var newEthnicity = new Ethnicity()
        {
            Hierarchy = new string[] { trimmed },
            Type = trimmed,
            UserDefined = true,
        };
        (DataService.Data.Ethnicities ??= new()).Add(newEthnicity);
        DataService.Ethnicities.Add(newEthnicity);
        NewEthnicityValue = string.Empty;
        await DataService.SaveAsync();
    }

    private async Task OnNewEthnicityAsync(Ethnicity parent, string? newValue)
    {
        parent.NewEthnicityValue = newValue;
        if (string.IsNullOrEmpty(parent.NewEthnicityValue))
        {
            return;
        }

        var trimmed = parent.NewEthnicityValue.Trim();
        if (parent
            .Types?
            .Any(x => string.Equals(x.Type, trimmed, StringComparison.OrdinalIgnoreCase)) == true)
        {
            parent.NewEthnicityValue = string.Empty;
            return;
        }

        var hierarchy = new string[(parent.Hierarchy?.Length ?? 0) + 1];
        if (parent.Hierarchy is not null)
        {
            Array.Copy(parent.Hierarchy, hierarchy, parent.Hierarchy.Length);
        }
        hierarchy[^1] = trimmed;

        var newEthnicity = new Ethnicity
        {
            Hierarchy = hierarchy,
            Parent = parent,
            Type = parent.NewEthnicityValue.Trim(),
            UserDefined = true,
        };

        (parent.Types ??= new()).Add(newEthnicity);
        var top = parent;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }
        if (DataService.Data.Ethnicities?.Any(x => x == newEthnicity) != true)
        {
            (DataService.Data.Ethnicities ??= new()).Add(top);
        }
        parent.NewEthnicityValue = string.Empty;
        await DataService.SaveAsync();
    }

    private async Task OnNewTraitAsync(string? newValue)
    {
        NewTraitValue = newValue;
        if (Story is null
            || string.IsNullOrEmpty(NewTraitValue))
        {
            return;
        }

        var trimmed = NewTraitValue.Trim();
        var newName = trimmed;
        var i = 0;
        while (DataService
            .Traits
            .Any(x => string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase))
            || DataService
            .Data
            .Traits?
            .Any(x => string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)) == true)
        {
            newName = $"{trimmed} ({i++})";
        }

        var newTrait = new Trait
        {
            Hierarchy = new string[] { newName },
            Name = newName,
            UserDefined = true,
        };

        (DataService.Data.Traits ??= new()).Add(newTrait);
        DataService.Traits.Add(newTrait);

        NewTraitValue = string.Empty;
        await DataService.SaveAsync();
    }

    private async Task OnNewTraitAsync(Trait parent, string? newValue)
    {
        parent.NewTraitValue = newValue;
        if (string.IsNullOrEmpty(parent.NewTraitValue))
        {
            return;
        }

        var trimmed = parent.NewTraitValue.Trim();
        var newName = trimmed;
        var i = 0;
        while (parent
            .Children?
            .Any(x => string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)) == true)
        {
            newName = $"{trimmed} ({i++})";
        }

        var hierarchy = new string[(parent.Hierarchy?.Length ?? 0) + 1];
        if (parent.Hierarchy is not null)
        {
            Array.Copy(parent.Hierarchy, hierarchy, parent.Hierarchy.Length);
        }
        hierarchy[^1] = newName;

        var newTrait = new Trait
        {
            Hierarchy = hierarchy,
            Name = newName,
            Parent = parent,
            UserDefined = true,
        };

        (parent.Children ??= new()).Add(newTrait);
        var top = parent;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }
        if (DataService.Data.Traits?.Any(x => x == newTrait) != true)
        {
            (DataService.Data.Traits ??= new()).Add(top);
        }

        parent.NewTraitValue = string.Empty;
        await DataService.SaveAsync();
    }

    private async Task OnPronounsChangedAsync(Character character, Pronouns value)
    {
        if (character.Pronouns != value)
        {
            character.Pronouns = value;
            await OnChangeAsync();
        }
    }

    private async Task OnRandomizeCharacterAsync(Character? character)
    {
        if (character is null)
        {
            return;
        }

        await OnRandomizeCharacterAgeAsync(character, true);
        await OnRandomizeCharacterEthnicitiesAsync(character, true);
        await OnRandomizeCharacterGenderAsync(character, true);
        await OnRandomizeCharacterTraitsAsync(character, true, true);
        await OnRandomizeCharacterFullNameAsync(character);
    }

    private async Task OnRandomizeCharacterAgeAsync(Character? character, bool deferSave = false)
    {
        if (character is null)
        {
            return;
        }

        var (min, max, mean) = character.GetAgeRange();
        double years;
        if (mean.HasValue)
        {
            var magnitude = Math.MaxMagnitude(mean.Value - min, max - mean.Value);
            years = Randomizer.Instance
                .NormalDistributionSample(mean.Value, magnitude / 3, min, max);
        }
        else
        {
            years = Randomizer.Instance.NextDouble(min, max);
        }
        if (Story?.Now.HasValue == true)
        {
            var birthDate = Story.Now.Value.AddYears(-(int)Math.Floor(years));
            birthDate = birthDate.Subtract(TimeSpan.FromDays(Math.Floor(years % 1 * 365.25)));
            character.SetBirthdate(Story, birthDate);
        }
        else
        {
            years = Math.Floor(years);
            var days = Math.Floor(years % 1 * 365.25);
            var months = 1;
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
            character.SetAge(
                Story,
                (int)years,
                months,
                Math.Max(0, (int)Math.Floor(days)));
        }
        if (!deferSave)
        {
            await OnChangeAsync();
        }
    }

    private async Task OnRandomizeCharacterEthnicitiesAsync(Character? character, bool deferSave = false)
    {
        if (character is null)
        {
            return;
        }

        var familyEthnicities = character.GetFamilyEthnicities();
        if (familyEthnicities.Count > 0)
        {
            character.SetEthnicities(familyEthnicities);
            if (!deferSave)
            {
                await OnChangeAsync();
            }
            return;
        }

        character.SetEthnicities(DataService.GetRandomEthnicities());
        if (!deferSave)
        {
            await OnChangeAsync();
        }
    }

    private async Task OnRandomizeCharacterFullNameAsync(Character? character)
    {
        if (character is null)
        {
            return;
        }

        var familySurnames = character.GetFamilySurnames();
        if (familySurnames.Count > 0)
        {
            character.Surnames = familySurnames;
            await OnRandomizeCharacterNameAsync(character);
            return;
        }

        var (givenName, surname) = await DataService
            .GetRandomFullNameAsync(character.GetNameGender(), character.EthnicityPaths);
        if (!string.IsNullOrEmpty(givenName))
        {
            character.Names = new() { givenName };
        }
        if (!string.IsNullOrEmpty(surname))
        {
            character.Surnames = new() { surname };
        }
        await OnChangeAsync();
    }

    private async Task OnRandomizeCharacterGenderAsync(Character? character, bool deferSave = false)
    {
        if (character is null)
        {
            return;
        }

        var chance = Randomizer.Instance.NextDouble();
        if (chance < 0.01)
        {
            character.Pronouns = Pronouns.TheyThem;
            character.Gender = "Non-binary";
        }
        else if (chance < 0.505)
        {
            character.Pronouns = Pronouns.SheHer;
            if (chance >= 0.495)
            {
                character.Gender = "Trans female";
            }
            else
            {
                character.Gender = "Female";
            }
        }
        else
        {
            character.Pronouns = Pronouns.HeHim;
            if (chance >= 0.99)
            {
                character.Gender = "Trans male";
            }
            else
            {
                character.Gender = "Male";
            }
        }
        Story?.ResetCharacterRelationshipMaps();
        if (!deferSave)
        {
            await OnChangeAsync();
        }
    }

    private async Task OnRandomizeCharacterNameAsync(Character? character, int? index = null)
    {
        if (character is null)
        {
            return;
        }

        var name = await DataService
            .GetRandomNameAsync(character.GetNameGender(), character.EthnicityPaths);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        if (index.HasValue)
        {
            if (character.Names?.Count > index)
            {
                character.Names[index.Value] = name;
            }
            else
            {
                (character.Names ??= new()).Add(name);
            }
        }
        else
        {
            character.Names = new() { name };
        }
        await OnChangeAsync();
    }

    private async Task OnRandomizeCharacterSurnameAsync(Character? character, int? index = null)
    {
        if (character is null)
        {
            return;
        }

        var name = await DataService
            .GetRandomSurnameAsync(character.EthnicityPaths);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
        if (index.HasValue)
        {
            if (character.Surnames?.Count > index)
            {
                character.Surnames[index.Value] = name;
            }
            else
            {
                (character.Surnames ??= new()).Add(name);
            }
        }
        else
        {
            character.Surnames = new() { name };
        }
        await OnChangeAsync();
    }

    private async Task OnRandomizeCharacterTraitsAsync(Character? character, bool reset = true, bool deferSave = false)
    {
        if (Story is null
            || character is null)
        {
            return;
        }

        if (reset)
        {
            character.ClearTraits();
        }
        foreach (var trait in DataService.Traits)
        {
            trait.Randomize(character);
        }
        if (!deferSave)
        {
            await OnChangeAsync();
        }
    }

    private async Task OnSurnameChangeAsync(Character character, int index, string? value)
    {
        if (character.Surnames is null
            || character.Surnames.Count <= index
            || string.Equals(character.Surnames[index], value, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            character.Surnames.RemoveAt(index);
            if (character.Surnames.Count == 0)
            {
                character.Surnames = null;
            }
            return;
        }
        else
        {
            character.Surnames[index] = value;
        }

        await OnChangeAsync();
    }

    private async Task OnTraitSelectAsync(bool value, Trait? trait, Character character)
    {
        if (trait is not null)
        {
            trait.Select(value, character);
            await OnChangeAsync();
        }
    }
}
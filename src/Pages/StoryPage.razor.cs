using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Scop.Shared;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Randomize;

namespace Scop.Pages;

public partial class StoryPage : IDisposable
{
    private static readonly List<string> _storyIcons = new()
    {
        "note",
        "person",
    };

    private bool _disposedValue;
    private DotNetObjectReference<StoryPage>? _dotNetObjectRef;
    private bool _initialized;
    private bool _loading = true;
    private Story? _story;

    [Parameter] public string? Id { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;

    private bool EthnicitiesVisible { get; set; }

    private bool IsTimelineSelected { get; set; }

    [Inject] private ScopJsInterop JsInterop { get; set; } = default!;

    private string? NewCharacterName { get; set; }

    private string? NewCharacterSurname { get; set; }

    private string? NewEthnicityValue { get; set; }

    private string? NewNoteValue { get; set; }

    private string? NewTraitValue { get; set; }

    public DateTimeOffset? SelectedBirthdate { get; set; }

    private INote? SelectedNote { get; set; }

    private string StoryName => _story?.Name ?? "Story";

    private bool TraitsVisible { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (!_initialized)
        {
            return;
        }

        if (Id is null)
        {
            _story = null;
            return;
        }

        if (_story is null
            || _story.Id != Id)
        {
            await LoadStoryAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            DataService.DataLoaded += OnDataLoaded;
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            DataService.GDriveSync = await JsInterop
                .GetDriveSignedIn(_dotNetObjectRef);
            await DataService.LoadAsync();
            _initialized = true;
            if (_story is null
                && Id is not null)
            {
                await LoadStoryAsync();
            }
            StateHasChanged();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dotNetObjectRef?.Dispose();
                _dotNetObjectRef = null;

                DataService.DataLoaded -= OnDataLoaded;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <para>
    /// Updates the current Google Drive signed-in status.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="isSignedIn">Whether the user is currently signed in.</param>
    [JSInvokable]
    public async Task UpdateDriveStatus(bool isSignedIn, string? userName)
    {
        DataService.GDriveSync = isSignedIn;
        DataService.GDriveUserName = userName;
        if (!isSignedIn)
        {
            return;
        }

        _loading = true;
        await InvokeAsync(StateHasChanged);

        await DataService.LoadAsync();

        if (_story is null
            && !string.IsNullOrEmpty(Id))
        {
            await LoadStoryAsync();
        }
        else
        {
            await DataService.SaveAsync();
        }
    }

    private static bool Contains(INote dropTarget, INote dropped)
    {
        if (dropTarget.Notes is null)
        {
            return false;
        }
        if (dropTarget.Notes.Contains(dropped))
        {
            return true;
        }
        foreach (var child in dropTarget.Notes)
        {
            if (Contains(child, dropped))
            {
                return true;
            }
        }
        return false;
    }

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

    private static void OnCancelEditingRelationship(Relationship relationship)
    {
        relationship.EditedInverseType = relationship.InverseType;
        relationship.EditedRelationshipName = relationship.RelationshipName;
        relationship.EditedRelativeName = relationship.Relative?.CharacterName ?? relationship.RelativeName;
        relationship.EditedType = relationship.Type;
        relationship.IsEditing = false;
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
            return Task.FromResult(_story?
                .AllCharacters()
                .Where(x => x != character
                    && !string.IsNullOrEmpty(x.CharacterName))
                .Select(x => new KeyValuePair<string, object>(
                    x.CharacterName!,
                    x.CharacterName!))
                ?? Enumerable.Empty<KeyValuePair<string, object>>());
        }

        var trimmed = value.Trim();

        return Task.FromResult(_story?.Notes?
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

    private async Task LoadStoryAsync()
    {
        if (!_loading)
        {
            _loading = true;
            await InvokeAsync(StateHasChanged);
        }
        _story = string.IsNullOrEmpty(Id)
            ? null
            : DataService.Data.Stories.Find(x => x.Id == Id);

        _story?.Initialize();

        _loading = false;
        await InvokeAsync(StateHasChanged);
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

    private static Note? SwitchToNote(INote note)
    {
        if (note is not Character character)
        {
            return null;
        }
        return new Note
        {
            Content = character.ToContent(),
            Name = character.Name,
            Notes = character.Notes,
        };
    }

    private static Character? SwitchToCharacter(INote note)
    {
        if (note is Character)
        {
            return null;
        }

        return new Character
        {
            Content = note.Content,
            Name = note.Name,
            Notes = note.Notes,
        };
    }

    private async Task OnAgeDaysChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeDays)
        {
            character.SetAgeDays(_story, value);
            SelectedBirthdate = character.Birthdate;
            await OnChangeAsync();
        }
    }

    private async Task OnAgeMonthsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeMonths)
        {
            character.SetAgeMonths(_story, value);
            SelectedBirthdate = character.Birthdate;
            await OnChangeAsync();
        }
    }

    private async Task OnAgeYearsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeYears)
        {
            character.SetAgeYears(_story, value);
            SelectedBirthdate = character.Birthdate;
            await OnChangeAsync();
        }
    }

    private async Task OnBirthdayChangedAsync(DateTimeOffset? value)
    {
        SelectedBirthdate = value;
        if (SelectedNote is not Character character)
        {
            return;
        }
        if (character.Birthdate != value)
        {
            character.SetBirthdate(_story, value);
            await OnChangeAsync();
        }
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
            _story?.ResetCharacterRelationshipMaps();
        }
        else if (gender.EndsWith("male")
            || gender.EndsWith("man"))
        {
            character.Pronouns = Pronouns.HeHim;
            _story?.ResetCharacterRelationshipMaps();
        }
        await OnChangeAsync();
    }

    private async Task OnChangeStoryNameAsync(string? name)
    {
        if (_story is null)
        {
            return;
        }
        _story.Name = name;
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

    private async Task OnContentChangedAsync(string? value)
    {
        if (SelectedNote is null)
        {
            return;
        }
        SelectedNote.Content = value;
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

    private async void OnDataLoaded(object? sender, EventArgs e) => await InvokeAsync(StateHasChanged);

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

    private async Task OnDeleteNoteAsync(INote note)
    {
        List<INote>? notes = null;
        if (note.Parent is not null)
        {
            notes = note.Parent.Notes;
        }
        else if (_story is not null)
        {
            notes = _story.Notes;
        }
        if (notes is null)
        {
            return;
        }

        notes.Remove(note);
        if (SelectedNote == note)
        {
            SelectedNote = null;
        }
        await DataService.SaveAsync();
    }

    private async Task OnDeleteRelationshipAsync(Character character, Relationship relationship)
    {
        var removed = character.Relationships?.Remove(relationship) ?? false;
        if (removed)
        {
            _story?.ResetCharacterRelationshipMaps();
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
        var relative = _story?
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

            _story?.ResetCharacterRelationshipMaps();
            await OnChangeAsync();
        }
    }

    private Task OnDropAsync(DropEventArgs e)
        => OnDropAsync(e, null);

    private async Task OnDropAsync(DropEventArgs e, INote? targetParent)
    {
        if (_story is null
            || e.Data is null)
        {
            return;
        }

        var note = DragDropService.TryGetData<INote>(e.Data);
        if (note is null)
        {
            return;
        }

        if (note.Parent is null)
        {
            _story.Notes?.Remove(note);
        }
        else
        {
            note.Parent.Notes?.Remove(note);
        }

        if (targetParent is null)
        {
            (_story.Notes ??= new()).Add(note);
        }
        else
        {
            (targetParent.Notes ??= new()).Add(note);
        }
        await OnChangeAsync();
    }

    private static void OnDropped(DragEffect e) { }

    private Task OnDropIndexAsync(DropIndexEventArgs e)
        => OnDropIndexAsync(e, null);

    private async Task OnDropIndexAsync(DropIndexEventArgs e, INote? targetParent)
    {
        if (_story is null
            || e.Data is null)
        {
            return;
        }

        var note = DragDropService.TryGetData<INote>(e.Data);
        if (note is null)
        {
            return;
        }

        var targetList = targetParent is null
            ? _story.Notes
            : targetParent.Notes;

        var target = !e.Index.HasValue
            || targetList is null
            || targetList.Count <= e.Index
            ? null
            : targetList[e.Index.Value];
        if (target is null)
        {
            await OnDropAsync(e, targetParent);
            return;
        }

        // avoid dropping into the note's own child tree
        if (Contains(target, note))
        {
            return;
        }

        if (note.Parent is null)
        {
            _story.Notes?.Remove(note);
        }
        else
        {
            note.Parent.Notes?.Remove(note);
        }

        if (targetParent is null)
        {
            (_story.Notes ??= new()).Add(note);
        }
        else
        {
            (targetParent.Notes ??= new()).Add(note);
        }
        await OnChangeAsync();
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

    private async Task OnNewCharacterSurnameAsync(Character character)
    {
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
        if (_story is null
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

    private async Task OnNewNoteSetAsync(string? newValue)
    {
        NewNoteValue = newValue;
        if (_story is null
            || string.IsNullOrEmpty(NewNoteValue))
        {
            return;
        }
        var newNote = new Note() { Name = NewNoteValue };
        (_story.Notes ??= new()).Add(newNote);
        NewNoteValue = string.Empty;
        SelectedNote = newNote;
        await DataService.SaveAsync();
    }

    private async Task OnNewNoteSetAsync(INote parent, string? newValue)
    {
        parent.NewNoteValue = newValue;
        if (string.IsNullOrEmpty(parent.NewNoteValue))
        {
            return;
        }
        var newNote = new Note() { Name = parent.NewNoteValue };
        (parent.Notes ??= new()).Add(newNote);
        parent.NewNoteValue = string.Empty;
        SelectedNote = newNote;
        await DataService.SaveAsync();
    }

    private async Task OnNewTraitAsync(string? newValue)
    {
        NewTraitValue = newValue;
        if (_story is null
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

    private async Task OnNowChangeAsync(DateTimeOffset? old)
    {
        if (_story?.Notes is null)
        {
            return;
        }

        foreach (var character in _story.AllCharacters())
        {
            if (_story.Now.HasValue)
            {
                if (character.AgeYears.HasValue
                    || character.AgeMonths.HasValue
                    || character.AgeDays.HasValue)
                {
                    if (!character.Birthdate.HasValue)
                    {
                        var birthdate = _story.Now.Value.Date;
                        try
                        {
                            if (character.AgeYears.HasValue)
                            {
                                birthdate = birthdate.AddYears(-character.AgeYears.Value);
                            }
                            if (character.AgeMonths.HasValue)
                            {
                                birthdate = birthdate.AddMonths(-character.AgeMonths.Value);
                            }
                            if (character.AgeDays.HasValue)
                            {
                                birthdate = birthdate.AddDays(-character.AgeDays.Value);
                            }
                            character.Birthdate = birthdate;
                            character.AgeYears = null;
                            character.AgeMonths = null;
                            character.AgeDays = null;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            if (old.HasValue)
                            {
                                var delta = _story.Now.Value - old.Value;
                                var years = (int)Math.Floor(delta.Days % 365.25);
                                var days = delta.Days - (int)Math.Floor(years * 365.25);
                                var months = (int)Math.Floor(days % 30.437);
                                days -= (int)Math.Floor(months * 30.437);
                                if (delta > TimeSpan.Zero)
                                {
                                    character.AgeDays += days;
                                    if (character.AgeDays >= 31)
                                    {
                                        character.AgeMonths++;
                                    }
                                    character.AgeMonths += months;
                                    if (character.AgeMonths >= 12)
                                    {
                                        character.AgeYears++;
                                    }
                                    character.AgeYears += years;
                                }
                                else
                                {
                                    character.AgeDays -= days;
                                    if (character.AgeDays < 0)
                                    {
                                        character.AgeMonths--;
                                    }
                                    character.AgeMonths -= months;
                                    if (character.AgeMonths < 0)
                                    {
                                        character.AgeYears--;
                                    }
                                    character.AgeYears -= years;
                                }
                            }
                        }
                    }
                }
            }
            else if (old.HasValue
                && character.Birthdate.HasValue)
            {
                var years = old.Value.Year - character.Birthdate.Value.Year;
                if (old.Value.Month > character.Birthdate.Value.Month
                    || (old.Value.Month == character.Birthdate.Value.Month
                    && old.Value.Day >= character.Birthdate.Value.Day))
                {
                    years++;
                }
                if (years >= 0)
                {
                    var months = old.Value.Month - character.Birthdate.Value.Month;
                    if (months < 0)
                    {
                        months += 12;
                    }
                    var days = old.Value.Day - character.Birthdate.Value.Day;
                    if (days < 0)
                    {
                        months--;
                        if (months < 0)
                        {
                            months += 12;
                            years--;
                        }
                        days += DateTime.DaysInMonth(old.Value.Year, old.Value.Month == 1 ? 12 : (old.Value.Month - 1));
                    }
                    character.AgeYears = years;
                    character.AgeMonths = months;
                    character.AgeDays = days;
                }
            }
        }

        await OnChangeAsync();
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
        if (_story?.Now.HasValue == true)
        {
            var birthDate = _story.Now.Value.AddYears(-(int)Math.Floor(years));
            birthDate = birthDate.Subtract(TimeSpan.FromDays(Math.Floor(years % 1 * 365.25)));
            character.SetBirthdate(_story, birthDate);
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
                _story,
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
        _story?.ResetCharacterRelationshipMaps();
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
        if (_story is null
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

    private async Task OnSelectedNoteNameChangedAsync(string? value)
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.Name = value;
        await DataService.SaveAsync();
    }

    private void OnSelectNote(INote? note)
    {
        IsTimelineSelected = false;
        SelectedNote = note;
        SelectedBirthdate = note is Character character
            ? character.Birthdate
            : null;
    }

    private void OnSelectTimeline()
    {
        SelectedNote = null;
        SelectedBirthdate = null;
        IsTimelineSelected = true;
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

    private async Task OnSwitchNoteTypeAsync(INote note, int iconIndex)
    {
        List<INote>? parentCollection = null;
        if (note.Parent is not null)
        {
            parentCollection = note.Parent.Notes;
        }
        else if (_story is not null)
        {
            parentCollection = _story.Notes;
        }
        if (parentCollection is null)
        {
            return;
        }

        var index = parentCollection.IndexOf(note);
        if (index == -1)
        {
            return;
        }

        parentCollection.RemoveAt(index);

        INote? newNote;
        if (iconIndex == 1)
        {
            newNote = SwitchToCharacter(note);
        }
        else
        {
            newNote = SwitchToNote(note);
        }
        if (newNote is not null)
        {
            parentCollection.Insert(index, newNote);
            SelectedNote = newNote;
            await DataService.SaveAsync();
        }
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

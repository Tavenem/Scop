using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Scop.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Tavenem.Blazor.MarkdownEditor;
using Tavenem.Randomize;

namespace Scop.Pages;

public partial class StoryPage : IDisposable
{
    private static readonly List<string> _storyIcons = new()
    {
        Icons.Material.Filled.Note,
        Icons.Material.Filled.Person,
    };

    private readonly Note _timelineDummyNote = new();

    private bool _disposedValue;
    private DotNetObjectReference<StoryPage>? _dotNetObjectRef;
    private bool _initialized;
    private bool _loading = true;
    private Story? _story;
    private Timeline? _timeline;

    [Parameter] public string? Id { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    [Inject] private IDialogService? DialogService { get; set; }

    private CustomTreeViewEventArgs<INote>? DragData { get; set; }

    private bool EthnicitiesVisible { get; set; }

    private bool IsTimelineEventEndTimeDisplayed { get; set; }

    private bool IsTimelineEventStartTimeDisplayed { get; set; }

    private bool IsTimelineSelected => SelectedNote == _timelineDummyNote;

    [Inject] private ScopJsInterop? JsInterop { get; set; }

    [CascadingParameter] private MudTheme? MudTheme { get; set; }

    private string? NewCharacterName { get; set; }

    private string? NewCharacterSurname { get; set; }

    private string? NewEthnicityValue { get; set; }

    private string? NewNoteValue { get; set; }

    private string? NewTraitValue { get; set; }

    public DateTime? SelectedBirthdate { get; set; }

    private TimelineEvent? SelectedEvent { get; set; }

    private INote? SelectedNote { get; set; }

    private string StoryName => _story?.Name ?? "Story";

    [CascadingParameter] private MarkdownEditorTheme Theme { get; set; }

    private DateTime? TimelineEventEndDate { get; set; }

    private TimeSpan? TimelineEventEndTime { get; set; }

    private DateTime? TimelineEventStartDate { get; set; }

    private TimeSpan? TimelineEventStartTime { get; set; }

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
        if (firstRender
            && JsInterop is not null
            && DataService is not null)
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

                if (DataService is not null)
                {
                    DataService.DataLoaded -= OnDataLoaded;
                }
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
        if (DataService is not null)
        {
            DataService.GDriveSync = isSignedIn;
            DataService.GDriveUserName = userName;
            if (isSignedIn)
            {
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
        }
    }

    private static Task<IEnumerable<string?>> GetGenders(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string?>>(Strings.Genders);
        }

        var trimmed = value.Trim();

        var list = Strings.Genders
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var index = list.IndexOf(trimmed);
        if (index != -1)
        {
            list.RemoveAt(index);
        }
        list.Insert(0, trimmed);

        return Task.FromResult<IEnumerable<string?>>(list);
    }

    private static Task<IEnumerable<string?>> GetRelationshipTypes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string?>>(Strings.RelationshipTypes);
        }

        var trimmed = value.Trim();

        var list = Strings.RelationshipTypes
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var index = list.IndexOf(trimmed);
        if (index != -1)
        {
            list.RemoveAt(index);
        }
        list.Insert(0, trimmed);

        return Task.FromResult<IEnumerable<string?>>(list);
    }

    private static Task<IEnumerable<string?>> GetSuffixes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string?>>(Strings.Suffixes);
        }

        var trimmed = value.Trim();

        var list = Strings.Suffixes
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var index = list.IndexOf(trimmed);
        if (index != -1)
        {
            list.RemoveAt(index);
        }
        list.Insert(0, trimmed);

        return Task.FromResult<IEnumerable<string?>>(list);
    }

    private static Task<IEnumerable<string?>> GetTitles(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string?>>(Strings.Titles);
        }

        var trimmed = value.Trim();

        var list = Strings.Titles
            .Where(x => x
                .Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var index = list.IndexOf(trimmed);
        if (index != -1)
        {
            list.RemoveAt(index);
        }
        list.Insert(0, trimmed);

        return Task.FromResult<IEnumerable<string?>>(list);
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

    private Task<IEnumerable<string?>> GetCharacterNames(Character character, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(_story?
                .AllCharacters()
                .Where(x => x != character)
                .Select(x => x.CharacterName)
                ?? Enumerable.Empty<string?>());
        }

        var trimmed = value.Trim();

        var list = _story?.Notes?
            .OfType<Character>()
            .Where(x => x != character)
            .Select(x => x.CharacterName)
            .Where(x => !string.IsNullOrEmpty(x)
                && x.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase))
            .ToList() ?? new();

        var index = list.IndexOf(trimmed);
        if (index != -1)
        {
            list.RemoveAt(index);
        }
        list.Insert(0, trimmed);

        return Task.FromResult<IEnumerable<string?>>(list);
    }

    private async Task<IEnumerable<string>> GetGivenNames(Character character, string? value)
    {
        var list = await GetNewGivenNames(character, value);
        return list
             .Where(x => !string.IsNullOrEmpty(x))
             .Select(x => x!);
    }

    private async Task<IEnumerable<string?>> GetNewGivenNames(Character character, string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? Enumerable.Empty<string?>()
                : new[] { trimmed };
        }

        var nameList = (await DataService
            .GetNameListAsync(character.GetNameGender(), character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase))
            .Distinct()
            .ToList();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var index = nameList.IndexOf(trimmed);
            if (index != -1)
            {
                nameList.RemoveAt(index);
            }
            nameList.Insert(0, trimmed);
        }
        return nameList;
    }

    private async Task<IEnumerable<string?>> GetNewSurnames(Character character, string? value)
    {
        var trimmed = value?.Trim();

        if (DataService is null)
        {
            return string.IsNullOrWhiteSpace(trimmed)
                ? Enumerable.Empty<string?>()
                : new[] { trimmed };
        }

        var nameList = (await DataService
            .GetSurnameListAsync(character.EthnicityPaths))
            .Select(x => x.Name)
            .Where(x => string.IsNullOrWhiteSpace(trimmed)
                || x!.Contains(trimmed, StringComparison.InvariantCultureIgnoreCase))
            .Distinct()
            .ToList();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var index = nameList.IndexOf(trimmed);
            if (index != -1)
            {
                nameList.RemoveAt(index);
            }
            nameList.Insert(0, trimmed);
        }
        return nameList;
    }

    private async Task<IEnumerable<string>> GetSurnames(Character character, string? value)
    {
        var names = await GetNewSurnames(character, value);
        return names
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!);
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

        if (_story?.Notes is not null)
        {
            foreach (var note in _story.Notes)
            {
                note.LoadCharacters(_story);
            }
            Character.SetRelationshipMaps(_story, _story.AllCharacters().ToList());
        }

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
            await OnChangeAsync();
        }
    }

    private async Task OnAgeMonthsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeMonths)
        {
            character.SetAgeMonths(_story, value);
            await OnChangeAsync();
        }
    }

    private async Task OnAgeYearsChangedAsync(Character character, int? value)
    {
        if (value != character.DisplayAgeYears)
        {
            character.SetAgeYears(_story, value);
            await OnChangeAsync();
        }
    }

    private async Task OnBirthdayChangedAsync(DateTime? value)
    {
        if (SelectedNote is not Character character)
        {
            return;
        }
        SelectedBirthdate = value;
        if (character.Birthdate != value)
        {
            character.SetBirthdate(_story, value);
            await OnChangeAsync();
        }
    }

    private Task OnChangeAsync() => DataService.SaveAsync();

    private async Task OnChangeGenderAsync(Character character)
    {
        var gender = character.Gender?.Trim().ToLowerInvariant() ?? string.Empty;
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

    private Task OnChildNoteDropAsync(CustomTreeViewEventArgs<INote> e) => OnNoteDropAsync(new()
    {
        NodeValue = e.ParentValue,
    }, true);

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

    private async Task OnDeleteNoteAsync(CustomTreeViewEventArgs<INote> e)
    {
        if (e.NodeValue is null)
        {
            return;
        }

        List<INote>? notes = null;
        if (e.ParentValue is not null)
        {
            notes = e.ParentValue.Notes;
        }
        else if (_story is not null)
        {
            notes = _story.Notes;
        }
        if (notes is null)
        {
            return;
        }

        notes.Remove(e.NodeValue);
        if (SelectedNote == e.NodeValue)
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

    private async Task OnDropAsync()
    {
        if (_story is null
            || DragData?.NodeValue is null)
        {
            return;
        }

        if (DragData.ParentValue is null)
        {
            _story.Notes?.Remove(DragData.NodeValue);
        }
        else
        {
            DragData.ParentValue.Notes?.Remove(DragData.NodeValue);
        }

        (_story.Notes ??= new()).Add(DragData.NodeValue);
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

    private async Task OnNewCharacterNameAsync(Character character)
    {
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

    private void OnNoteDrag(CustomTreeViewEventArgs<INote> e) => DragData = e;

    private async Task OnNoteDropAsync(CustomTreeViewEventArgs<INote> e, bool child)
    {
        if (_story is null
            || DragData?.NodeValue is null
            || e.NodeValue is null
            || DragData.NodeValue == e.NodeValue)
        {
            return;
        }

        // avoid dropping into the note's own child tree
        static bool Contains(INote note, INote target)
        {
            if (note.Notes is null)
            {
                return false;
            }
            if (note.Notes.Contains(target))
            {
                return true;
            }
            foreach (var child in note.Notes)
            {
                if (Contains(child, target))
                {
                    return true;
                }
            }
            return false;
        }
        if (Contains(DragData.NodeValue, e.NodeValue))
        {
            return;
        }

        if (DragData.ParentValue is null)
        {
            _story.Notes?.Remove(DragData.NodeValue);
        }
        else
        {
            DragData.ParentValue.Notes?.Remove(DragData.NodeValue);
        }

        if (child)
        {
            (e.NodeValue.Notes ??= new()).Add(DragData.NodeValue);
        }
        else if (e.ParentValue is null)
        {
            var index = _story.Notes?.IndexOf(e.NodeValue) ?? -1;
            if (index == -1)
            {
                (_story.Notes ??= new()).Add(DragData.NodeValue);
            }
            else
            {
                (_story.Notes ??= new()).Insert(index, DragData.NodeValue);
            }
        }
        else
        {
            var index = e.ParentValue.Notes?.IndexOf(e.NodeValue) ?? -1;
            if (index == -1)
            {
                (e.ParentValue.Notes ??= new()).Add(DragData.NodeValue);
            }
            else
            {
                (e.ParentValue.Notes ??= new()).Insert(index, DragData.NodeValue);
            }
        }
        await OnChangeAsync();
    }

    private Task OnNoteDropAsync(CustomTreeViewEventArgs<INote> e)
        => OnNoteDropAsync(e, false);

    private async Task OnNowChangeAsync(DateTime? old)
    {
        if (_story?.Notes is null)
        {
            return;
        }

        foreach (var character in _story.Notes.OfType<Character>())
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

    private void OnSelectedEventChanged(TimelineEvent? selectedEvent)
    {
        SelectedEvent = selectedEvent;

        TimelineEventEndDate = SelectedEvent?.EffectiveEnd;
        TimelineEventEndTime = SelectedEvent?.EffectiveEnd?.TimeOfDay;
        IsTimelineEventEndTimeDisplayed = TimelineEventEndTime.HasValue && TimelineEventEndTime.Value.Ticks > 0;

        TimelineEventStartDate = SelectedEvent?.EffectiveStart;
        TimelineEventStartTime = SelectedEvent?.EffectiveStart?.TimeOfDay;
        IsTimelineEventStartTimeDisplayed = TimelineEventStartTime.HasValue && TimelineEventStartTime.Value.Ticks > 0;
    }

    private async Task OnSelectedEventChangedAsync(string? newValue)
    {
        if (SelectedEvent is null)
        {
            return;
        }
        SelectedEvent.Content = newValue;
        if (_timeline is not null)
        {
            await _timeline.UpdateSelectedEventAsync();
            await OnChangeAsync();
        }
    }

    private async Task OnSelectedEventEndDateChangedAsync(DateTime? value)
    {
        TimelineEventEndDate = value;
        if (SelectedEvent is not null)
        {
            SelectedEvent.End = TimelineEventEndDate?.Date.ToUniversalTime();
            if (SelectedEvent.End.HasValue && TimelineEventEndTime.HasValue)
            {
                SelectedEvent.End = SelectedEvent.End.Value.Add(TimelineEventEndTime.Value);
            }
        }
        if (_timeline is not null)
        {
            await _timeline.UpdateSelectedEventAsync();
        }
    }

    private async Task OnSelectedEventEndTimeChangedAsync(TimeSpan? value)
    {
        TimelineEventEndTime = value;
        if (SelectedEvent is not null)
        {
            SelectedEvent.End = TimelineEventEndDate?.Date.ToUniversalTime();
            if (SelectedEvent.End.HasValue && TimelineEventEndTime.HasValue)
            {
                SelectedEvent.End = SelectedEvent.End.Value.Add(TimelineEventEndTime.Value);
            }
        }
        if (_timeline is not null)
        {
            await _timeline.UpdateSelectedEventAsync();
        }
    }

    private async Task OnSelectedEventStartDateChangedAsync(DateTime? value)
    {
        TimelineEventStartDate = value;
        if (SelectedEvent is not null)
        {
            SelectedEvent.Start = TimelineEventStartDate?.Date.ToUniversalTime();
            if (SelectedEvent.Start.HasValue && TimelineEventStartTime.HasValue)
            {
                SelectedEvent.Start = SelectedEvent.Start.Value.Add(TimelineEventStartTime.Value);
            }
        }
        if (_timeline is not null)
        {
            await _timeline.UpdateSelectedEventAsync();
        }
    }

    private async Task OnSelectedEventStartTimeChangedAsync(TimeSpan? value)
    {
        TimelineEventStartTime = value;
        if (SelectedEvent is not null)
        {
            SelectedEvent.Start = TimelineEventStartDate?.Date.ToUniversalTime();
            if (SelectedEvent.Start.HasValue && TimelineEventStartTime.HasValue)
            {
                SelectedEvent.Start = SelectedEvent.Start.Value.Add(TimelineEventStartTime.Value);
            }
        }
        if (_timeline is not null)
        {
            await _timeline.UpdateSelectedEventAsync();
        }
    }

    private void OnSelectNote(INote note)
    {
        SelectedNote = note;
        SelectedBirthdate = note is Character character
            ? character.Birthdate
            : null;
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

    private async Task OnSwitchNoteTypeAsync(CustomTreeViewIntEventArgs<INote> e)
    {
        if (e.NodeValue is null)
        {
            return;
        }

        List<INote>? parentCollection = null;
        if (e.ParentValue is not null)
        {
            parentCollection = e.ParentValue.Notes;
        }
        else if (_story is not null)
        {
            parentCollection = _story.Notes;
        }
        if (parentCollection is null)
        {
            return;
        }

        var index = parentCollection.IndexOf(e.NodeValue);
        if (index == -1)
        {
            return;
        }

        parentCollection.RemoveAt(index);

        INote? newNote;
        if (e.Value == 1)
        {
            newNote = SwitchToCharacter(e.NodeValue);
        }
        else
        {
            newNote = SwitchToNote(e.NodeValue);
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

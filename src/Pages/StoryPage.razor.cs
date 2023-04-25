using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;

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

    private string? EditorContent { get; set; }

    private bool IsTimelineSelected { get; set; }

    [Inject] private ScopJsInterop JsInterop { get; set; } = default!;

    private string? NewNoteValue { get; set; }

    public DateTimeOffset? SelectedBirthdate { get; set; }

    private INote? SelectedNote { get; set; }

    private string StoryName => _story?.Name ?? "Story";

    private INote? TopSelectedNote { get; set; }

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

    private Task OnChangeAsync() => DataService.SaveAsync();

    private async Task OnChangeStoryNameAsync(string? name)
    {
        if (_story is null)
        {
            return;
        }
        _story.Name = name;
        await OnChangeAsync();
    }

    private async Task OnContentChangedAsync()
    {
        if (SelectedNote is null)
        {
            return;
        }
        SelectedNote.Content = EditorContent;
        await DataService.SaveAsync();
    }

    private async void OnDataLoaded(object? sender, EventArgs e) => await InvokeAsync(StateHasChanged);

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
            EditorContent = null;
        }
        await DataService.SaveAsync();
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
        EditorContent = SelectedNote.Content;
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
        EditorContent = SelectedNote.Content;
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

    private async Task OnSelectChildNoteAsync(INote? note)
    {
        if (note?.Equals(SelectedNote) != false)
        {
            return;
        }
        TopSelectedNote = null;
        SelectedNote = note;
        EditorContent = note.Content;
        if (_story?.Notes is not null)
        {
            foreach (var child in _story.Notes)
            {
                await child.SetSelectionAsync(SelectedNote);
            }
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

    private async Task OnSelectedNoteChangedAsync()
    {
        if (TopSelectedNote?.Equals(SelectedNote) != false)
        {
            Console.WriteLine("returning");
            return;
        }
        SelectedNote = TopSelectedNote;
        EditorContent = SelectedNote.Content;
        IsTimelineSelected = false;
        SelectedBirthdate = SelectedNote is Character character
            ? character.Birthdate
            : null;
        if (_story?.Notes is not null)
        {
            foreach (var child in _story.Notes)
            {
                await child.SetSelectionAsync(SelectedNote);
            }
        }
    }

    private void OnSelectTimeline()
    {
        IsTimelineSelected = true;
        SelectedNote = null;
        EditorContent = null;
        SelectedBirthdate = null;
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
            EditorContent = SelectedNote.Content;
            await DataService.SaveAsync();
        }
    }
}

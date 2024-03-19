using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Scop.Models;
using Scop.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tavenem.Blazor.Framework;

namespace Scop.Pages;

public partial class StoryPage : IDisposable
{
    private static readonly List<string> _storyIcons =
    [
        "note",
        "person",
    ];
    private static readonly List<string> _storyAddIcons =
    [
        "note_add",
        "person_add",
    ];

    private bool _disposedValue;
    private DotNetObjectReference<StoryPage>? _dotNetObjectRef;
    private bool _initialized;
    private bool _loading = true;
    private Story? _story;

    [Parameter] public string? Id { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    [Inject, NotNull] private DialogService? DialogService { get; set; }

    private string? EditorContent { get; set; }

    private bool IsTimelineSelected { get; set; }

    [Inject, NotNull] private ScopJsInterop? JsInterop { get; set; }

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
    /// This method is invoked by internal JavaScript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="isSignedIn">Whether the user is currently signed in.</param>
    [JSInvokable]
    public async Task UpdateDriveStatus(bool isSignedIn)
    {
        DataService.GDriveSync = isSignedIn;
        if (!isSignedIn)
        {
            return;
        }

        _loading = true;
        await InvokeAsync(StateHasChanged);

        await DataService.LoadAsync(true);

        if (_story is null
            && !string.IsNullOrEmpty(Id))
        {
            await LoadStoryAsync();
        }
        else
        {
            await DataService.SaveAsync();

            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadStoryAsync()
    {
        if (!_loading)
        {
            _loading = true;
            await InvokeAsync(StateHasChanged);
        }

        _story = null;
        if (string.IsNullOrEmpty(Id))
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _story = DataService.Data.Stories.Find(x => x.Id == Id);
        if (_story is null
            && !DataService.GDriveSync)
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            if (await JsInterop
                .DriveAuthorize(_dotNetObjectRef))
            {
                await DataService.LoadAsync(true);
                _story = DataService.Data.Stories.Find(x => x.Id == Id);
            }
        }

        _story?.Initialize();

        _loading = false;
        await InvokeAsync(StateHasChanged);
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
        if (note.Parent is not null)
        {
            Console.WriteLine("Parent is not null");
            if (note.Parent.Notes is null)
            {
                Console.WriteLine("Parent.Notes is null");
                return;
            }
            note.Parent.Notes.Remove(note);
        }
        else if (_story is not null)
        {
            if (_story.Notes is null)
            {
                return;
            }
            _story.Notes.Remove(note);
        }
        else
        {
            return;
        }

        if (note.Contains(SelectedNote))
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
            (_story.Notes ??= []).Add(note);
        }
        else
        {
            (targetParent.Notes ??= []).Add(note);
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
        if (note.Contains(target))
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
            (_story.Notes ??= []).Add(note);
        }
        else
        {
            (targetParent.Notes ??= []).Add(note);
        }
        await OnChangeAsync();
    }

    private async Task OnNewNoteOfTypeAsync(int index)
    {
        if (_story is null)
        {
            return;
        }
        INote newNote = index == 1
            ? new Character()
            : new Note();
        (_story.Notes ??= []).Add(newNote);
        SelectedNote = newNote;
        EditorContent = null;
        await DataService.SaveAsync();
    }

    private async Task OnNewNoteOfTypeAsync(INote parent, int index)
    {
        INote newNote = index == 1
            ? new Character()
            : new Note();
        newNote.Parent = parent;
        (parent.Notes ??= []).Add(newNote);
        SelectedNote = newNote;
        EditorContent = null;
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
        TopSelectedNote = null;
    }

    private async Task OnWritingPromptAsync()
    {
        if (_story is null)
        {
            return;
        }

        var result = await DialogService.Show<WritingPromptDialog>("Writing Prompt").Result;
        if (result.Choice != DialogChoice.Ok
            || result.Data is not WritingPrompt prompt)
        {
            return;
        }

        var blankNote = _story.Notes?.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Content));
        if (blankNote is null)
        {
            blankNote = new Note();
            (_story.Notes ??= []).Add(blankNote);
        }

        if (string.IsNullOrWhiteSpace(blankNote.Name))
        {
            blankNote.Name = "Writing Prompt";
        }

        var promptContent = new StringBuilder();

        var firstSentence = !string.IsNullOrWhiteSpace(prompt.Genre)
            || !string.IsNullOrWhiteSpace(prompt.Subgenre)
            || prompt.Themes?.Count > 0;

        if (firstSentence)
        {
            promptContent.Append("This");
        }

        if (!string.IsNullOrWhiteSpace(prompt.Genre))
        {
            if (string.IsNullOrWhiteSpace(prompt.Subgenre)
                && !(prompt.Themes?.Count > 0))
            {
                promptContent.Append(" is a");
            }
            promptContent
                .Append(' ')
                .Append(prompt.Genre);
        }

        if (firstSentence)
        {
            promptContent.Append(" story");
        }

        if (!string.IsNullOrWhiteSpace(prompt.Subgenre))
        {
            promptContent.Append(" is");
            promptContent
                .Append(' ')
                .Append(prompt.Subgenre);
        }

        if (prompt.Themes?.Count > 0)
        {
            promptContent.Append(" about ");
            for (var i = 0; i < prompt.Themes.Count; i++)
            {
                if (i > 0)
                {
                    promptContent.Append(", ");
                }
                if (i > 0 && i == prompt.Themes.Count - 1)
                {
                    promptContent.Append("and ");
                }
                promptContent.Append(prompt.Themes[i]);
            }
        }

        if (firstSentence)
        {
            promptContent.Append(". ");
        }

        if (prompt.Settings?.Count > 0)
        {
            if (firstSentence)
            {
                promptContent.Append("It begins ");
            }
            else
            {
                promptContent.Append("This story begins ");
            }

            for (var i = 0; i < prompt.Settings.Count; i++)
            {
                if (i == 1)
                {
                    promptContent.Append(", while other events take place");
                }
                else if (prompt.Settings.Count > 2 && i == prompt.Settings.Count - 1)
                {
                    promptContent.Append(", and");
                }
                else if (i > 0)
                {
                    promptContent.Append(", ");
                }
                promptContent.Append(prompt.Settings[i]);
            }

            promptContent.Append(", ");
        }

        var secondSentence = prompt.Settings?.Count > 0
            || prompt.Subjects?.Count > 0;

        if (prompt.Subjects?.Count > 0)
        {
            if (prompt.Settings?.Count > 1)
            {
                promptContent.Append(" starting things off with ");
            }
            else if (prompt.Settings?.Count > 0)
            {
                promptContent.Append(" with ");
            }
            else if (firstSentence)
            {
                promptContent.Append("It begins with ");
            }
            else
            {
                promptContent.Append("This story begins with ");
            }

            for (var i = 0; i < prompt.Subjects.Count; i++)
            {
                if (i > 0)
                {
                    promptContent.Append(", ");
                }
                if (i == 1)
                {
                    promptContent.Append("and features ");
                }
                else if (prompt.Subjects.Count > 2 && i == prompt.Subjects.Count - 1)
                {
                    promptContent.Append("along with ");
                }
                else if (prompt.Subjects.Count > 1 && i > 0 && i >= prompt.Subjects.Count - 2)
                {
                    promptContent.Append("and ");
                }
                promptContent.Append(prompt.Subjects[i]);
            }
        }
        if (secondSentence)
        {
            promptContent.Append('.');
        }

        var firstParagraph = firstSentence || secondSentence;
        if (firstParagraph)
        {
            promptContent.AppendLine().AppendLine();
        }

        var characterParagraph = !string.IsNullOrEmpty(prompt.Protagonist)
            || prompt.SecondaryCharacters?.Count > 0;

        if (!string.IsNullOrEmpty(prompt.Protagonist))
        {
            promptContent
                .Append("The main character ");
            if (!firstParagraph)
            {
                promptContent.Append("of this story ");
            }
            promptContent.Append("is ")
                .Append(prompt.Protagonist);
            if (prompt.ProtagonistTraits?.Count > 0)
            {
                promptContent.Append(", who ");
                for (var i = 0; i < prompt.ProtagonistTraits.Count; i++)
                {
                    if (i > 0)
                    {
                        promptContent.Append(", ");
                        if (i == prompt.ProtagonistTraits.Count - 1)
                        {
                            promptContent.Append("and ");
                        }
                    }
                    promptContent.Append(prompt.ProtagonistTraits[i]);
                }
            }
            promptContent.Append(". ");
        }

        if (prompt.SecondaryCharacters?.Count > 0)
        {
            if (firstParagraph || !string.IsNullOrEmpty(prompt.Protagonist))
            {
                promptContent.Append("The story also features ");
            }
            else
            {
                promptContent.Append("This story features ");
            }
            for (var i = 0; i < prompt.SecondaryCharacters.Count; i++)
            {
                if (i > 0)
                {
                    promptContent.Append(", ");
                    if (i == prompt.SecondaryCharacters.Count - 1)
                    {
                        promptContent.Append("and ");
                    }
                }
                promptContent.Append(prompt.SecondaryCharacters[i].Description ?? "another character");
                if (prompt.SecondaryCharacters[i].Traits?.Count > 0)
                {
                    promptContent.Append(", who ");
                    for (var j = 0; j < prompt.SecondaryCharacters[i].Traits!.Count; j++)
                    {
                        if (j > 0)
                        {
                            promptContent.Append(", ");
                            if (j == prompt.SecondaryCharacters[i].Traits!.Count - 1)
                            {
                                promptContent.Append("and ");
                            }
                        }
                        promptContent.Append(prompt.SecondaryCharacters[i].Traits![j]);
                    }
                }
            }
            promptContent.Append('.');
        }
        if (characterParagraph)
        {
            promptContent.AppendLine().AppendLine();
        }

        if (prompt.Features?.Count > 0)
        {
            promptContent.Append("Keep in mind that ");
            for (var i = 0; i < prompt.Features.Count; i++)
            {
                if (i > 0)
                {
                    promptContent.Append(", ");
                    if (i == prompt.Features.Count - 1)
                    {
                        promptContent.Append("and ");
                    }
                }
                promptContent.Append(prompt.Features[i]);
            }
            promptContent.AppendLine(".").AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(prompt.Plot?.Name))
        {
            promptContent
                .Append("Plot archetype: ")
                .Append(prompt.Plot.Name)
                .Append('.');
            if (!string.IsNullOrEmpty(prompt.Plot.Description))
            {
                promptContent
                    .Append(' ')
                    .Append(prompt.Plot.Description);
            }
        }

        blankNote.Content = promptContent.ToString();

        await DataService.SaveAsync();

        if (blankNote.Parent is null)
        {
            TopSelectedNote = blankNote;
            await OnSelectedNoteChangedAsync();
        }
        else
        {
            await OnSelectChildNoteAsync(blankNote);
        }
    }
}

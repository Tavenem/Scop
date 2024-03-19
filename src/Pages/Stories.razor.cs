using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Scop.Pages;

public partial class Stories : IDisposable
{
    private readonly Random _random = new();

    private Story? _deleteStory;
    private bool _disposedValue;
    private DotNetObjectReference<Stories>? _dotNetObjectRef;
    private bool _loading = true;

    [Inject, NotNull] private DataService? DataService { get; set; }

    private bool DeleteDialogOpen { get; set; }

    private bool FirstRandomSelection { get; set; } = true;

    [Inject, NotNull] private ScopJsInterop? JsInterop { get; set; }

    [Inject, NotNull] private NavigationManager? NavigationManager { get; set; }

    private Story? SelectedStory { get; set; }

    private HashSet<string> SelectedStories { get; set; } = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            DataService.DataLoaded += OnDataLoaded;
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            DataService.GDriveSync = await JsInterop
                .GetDriveSignedIn(_dotNetObjectRef);
            await DataService.LoadAsync();
            _loading = false;
            StateHasChanged();
        }
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

        _loading = false;
        await InvokeAsync(StateHasChanged);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
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

    private void OnAddStory()
    {
        var story = new Story
        {
            Id = Guid.NewGuid().ToString(),
        };
        DataService.Data.Stories.Add(story);
        // do not save an empty story; wait for the user to make a real change
        OnOpenStory(story);
    }

    private async Task OnConfirmDeleteAsync()
    {
        DeleteDialogOpen = false;
        if (_deleteStory is not null)
        {
            DataService.Data.Stories.Remove(_deleteStory);
            await DataService.SaveAsync();
        }
    }

    private async void OnDataLoaded(object? sender, EventArgs e)
        => await InvokeAsync(StateHasChanged);

    private async Task OnLinkGDrive()
    {
        if (JsInterop is not null
            && _dotNetObjectRef is not null)
        {
            await JsInterop
                .DriveAuthorize(_dotNetObjectRef);
        }
    }

    private void OnOpenSelectedStory()
    {
        if (SelectedStory is not null)
        {
            NavigationManager.NavigateTo($"./story/{SelectedStory.Id}");
        }
    }

    private void OnOpenStory(Story story) => NavigationManager.NavigateTo($"./story/{story.Id}");

    private void OnDeleteStory(Story story)
    {
        _deleteStory = story;
        DeleteDialogOpen = true;
    }

    private void OnSelectRandomStory()
    {
        if (FirstRandomSelection
            && SelectedStories.Count > 0)
        {
            FirstRandomSelection = false;
        }
        SelectedStory = DataService.Data.Stories.Count == 0
            ? null
            : DataService.Data.Stories[_random.Next(DataService.Data.Stories.Count)];
        if (SelectedStory?.Id is not null)
        {
            SelectedStories.Add(SelectedStory.Id);
        }
    }

    private void OnSelectShuffledStory()
    {
        if (FirstRandomSelection
            && SelectedStories.Count > 0)
        {
            FirstRandomSelection = false;
        }
        if (SelectedStories.Count >= DataService.Data.Stories.Count)
        {
            SelectedStories.Clear();
        }
        var availableStories = DataService.Data.Stories.Where(x
            => x.Id is not null
            && !SelectedStories.Contains(x.Id))
            .ToList();
        SelectedStory = availableStories.Count == 0
            ? null
            : availableStories[_random.Next(availableStories.Count)];
        if (SelectedStory?.Id is not null)
        {
            SelectedStories.Add(SelectedStory.Id);
        }
    }
}

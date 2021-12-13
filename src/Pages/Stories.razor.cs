using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Scop.Pages;

public partial class Stories : IDisposable
{
    private Story? _deleteStory;
    private bool _disposedValue;
    private bool _loading = true;

    [Inject] private DataService? DataService { get; set; }

    private bool DeleteDialogOpen { get; set; }

    [Inject] private NavigationManager? NavigationManager { get; set; }

    [CascadingParameter] private MudTheme? Theme { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && DataService is not null)
        {
            DataService.DataLoaded += OnDataLoaded;
            await DataService.LoadAsync();
            _loading = false;
            StateHasChanged();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && DataService is not null)
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
        if (DataService is null)
        {
            return;
        }

        var story = new Story
        {
            Id = Guid.NewGuid().ToString(),
        };
        DataService.Data.Stories.Add(story);
        // do not save an empty story; wait for the user to make a real change
        OnOpenStory(story);
    }

    private void OnCancelDelete() => DeleteDialogOpen = false;

    private async Task OnConfirmDeleteAsync()
    {
        DeleteDialogOpen = false;
        if (DataService is not null && _deleteStory is not null)
        {
            DataService.Data.Stories.Remove(_deleteStory);
            await DataService.SaveAsync();
        }
    }

    private async void OnDataLoaded(object? sender, EventArgs e) => await InvokeAsync(StateHasChanged);

    private void OnOpenStory(Story story) => NavigationManager?.NavigateTo($"./story/{story.Id}");

    private void OnDeleteStory(Story story)
    {
        _deleteStory = story;
        DeleteDialogOpen = true;
    }
}

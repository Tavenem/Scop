using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Scop.Shared;

public partial class AnchorNavigator : IDisposable
{
    private bool _disposedValue;

    [Inject] private ScopJsInterop? JsInterop { get; set; }

    [Inject] private NavigationManager? NavigationManager { get; set; }

    protected override void OnInitialized()
    {
        if (NavigationManager is not null)
        {
            NavigationManager.LocationChanged += OnLocationChanged;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await ScrollToFragment();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing && NavigationManager is not null)
            {
                NavigationManager.LocationChanged -= OnLocationChanged;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        => await ScrollToFragment();

    private async Task ScrollToFragment()
    {
        if (NavigationManager is null
            || JsInterop is null)
        {
            return;
        }

        var uri = new Uri(NavigationManager.Uri, UriKind.Absolute);
        var fragment = uri.Fragment;
        if (!fragment.StartsWith('#'))
        {
            return;
        }

        var elementId = fragment[1..];
        var index = elementId.IndexOf(":~:", StringComparison.Ordinal);
        if (index > 0)
        {
            elementId = elementId[..index];
        }

        if (!string.IsNullOrEmpty(elementId))
        {
            await JsInterop.ScrollToId(elementId);
        }
    }
}

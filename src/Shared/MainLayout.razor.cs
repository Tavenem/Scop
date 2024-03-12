using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Scop.Shared;

public partial class MainLayout : IDisposable
{
    private DotNetObjectReference<MainLayout>? _dotNetObjectRef;
    private bool _disposedValue;

    [Inject] private DataService? DataService { get; set; }

    [Inject] private ScopJsInterop? JsInterop { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender
            && JsInterop is not null
            && DataService is not null)
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);

            DataService.GDriveSync = await JsInterop
                .GetDriveSignedIn(_dotNetObjectRef);
            if (DataService.GDriveSync)
            {
                StateHasChanged();
            }
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
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
        if (DataService is not null)
        {
            DataService.GDriveSync = isSignedIn;
            await InvokeAsync(StateHasChanged);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dotNetObjectRef?.Dispose();
            }

            _disposedValue = true;
        }
    }
}

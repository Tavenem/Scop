using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Scop.Pages;

namespace Scop.Services;

public class ScopJsInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./script.js").AsTask());

    private bool _disposed;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing,
    /// or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DownloadText(string filename, string text)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("downloadText", filename, text);
    }

    public async ValueTask<bool> DriveAuthorize<T>(DotNetObjectReference<T> dotNetObjectRef) where T : ComponentBase
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("driveAuthorize", dotNetObjectRef);
    }

    public async ValueTask DriveSignOut(DotNetObjectReference<ManageData> dotNetObjectRef)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("driveSignOut", dotNetObjectRef);
    }

    public async ValueTask<bool> GetDriveSignedIn()
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("getDriveSignedIn");
    }

    public async ValueTask<bool> GetDriveSignedIn<T>(DotNetObjectReference<T> dotNetObjectRef) where T : ComponentBase
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<bool>("getDriveSignedIn", dotNetObjectRef);
    }

    public async ValueTask<string?> GetDriveUser<T>(DotNetObjectReference<T> dotNetObjectRef) where T : ComponentBase
    {
        var module = await _moduleTask.Value;
        return await module.InvokeAsync<string?>("getDriveUser", dotNetObjectRef);
    }

    public async ValueTask LoadDriveData(DotNetObjectReference<DataService> dotNetObjectRef)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("loadDriveData", dotNetObjectRef);
    }

    public async ValueTask SaveDriveData(string data)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("saveDriveData", data);
    }
}

﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Scop.Pages;

namespace Scop;

public class ScopJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    private bool _disposed;

    public ScopJsInterop(IJSRuntime jsRuntime) => _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./script.js").AsTask());

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
                var module = await _moduleTask.Value.ConfigureAwait(false);
                await module.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DownloadText(string filename, string text)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("downloadText", filename, text)
            .ConfigureAwait(false);
    }

    public async ValueTask DriveAuthorize(DotNetObjectReference<ManageData> dotNetObjectRef)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("driveAuthorize", dotNetObjectRef)
            .ConfigureAwait(false);
    }

    public async ValueTask DriveSignout(DotNetObjectReference<ManageData> dotNetObjectRef)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("driveSignout", dotNetObjectRef)
            .ConfigureAwait(false);
    }

    public async ValueTask<bool> GetDriveSignedIn()
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<bool>("getDriveSignedIn")
            .ConfigureAwait(false);
    }

    public async ValueTask<bool> GetDriveSignedIn<T>(DotNetObjectReference<T> dotNetObjectRef) where T : ComponentBase
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<bool>("getDriveSignedIn", dotNetObjectRef)
            .ConfigureAwait(false);
    }

    public async ValueTask<string?> GetDriveUser<T>(DotNetObjectReference<T> dotNetObjectRef) where T : ComponentBase
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module
            .InvokeAsync<string?>("getDriveUser", dotNetObjectRef)
            .ConfigureAwait(false);
    }

    public async ValueTask LoadDriveData(DotNetObjectReference<DataService> dotNetObjectRef)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("loadDriveData", dotNetObjectRef)
            .ConfigureAwait(false);
    }

    public async ValueTask SaveDriveData(string data)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module
            .InvokeVoidAsync("saveDriveData", data)
            .ConfigureAwait(false);
    }
}

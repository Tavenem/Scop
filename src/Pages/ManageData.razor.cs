using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Scop.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Scop.Pages;

public partial class ManageData : IDisposable
{
    private bool _disposedValue;
    private DotNetObjectReference<ManageData>? _dotNetObjectRef;
    private bool _loading = true;

    [Inject] private DataService? DataService { get; set; }

    private bool DeleteLocalDialogOpen { get; set; }

    private bool UploadDialogOpen { get; set; }

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
            DataService.GDriveUserName = await JsInterop
                .GetDriveUser(_dotNetObjectRef);
            await DataService.LoadAsync();
            _loading = false;
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
                await DataService.SaveAsync();
            }
        }
    }

    private void OnCancelLocalDelete() => DeleteLocalDialogOpen = false;

    private void OnCancelUpload() => UploadDialogOpen = false;

    private void OnConfirmLocalDelete()
    {
        DeleteLocalDialogOpen = false;
        DataService?.DeleteLocal();
    }

    private void OnDeleteLocalData() => DeleteLocalDialogOpen = true;

    private async Task OnDownloadDataAsync()
    {
        if (JsInterop is not null
            && DataService is not null)
        {
            await JsInterop.DownloadText(
              "scop.json",
              JsonSerializer.Serialize(DataService.Data));
        }
    }

    private async Task OnLinkGDrive()
    {
        if (JsInterop is not null
            && _dotNetObjectRef is not null)
        {
            await JsInterop
                .DriveAuthorize(_dotNetObjectRef);
        }
    }

    private async Task OnUnlinkGDrive()
    {
        if (JsInterop is not null
            && _dotNetObjectRef is not null)
        {
            await JsInterop
                .DriveSignout(_dotNetObjectRef);
        }
    }

    private void OnUploadData() => UploadDialogOpen = true;

    private async Task OnUploadFileAsync(InputFileChangeEventArgs e)
    {
        if (DataService is not null
            && e.File.ContentType == "application/json")
        {
            var json = await new StreamReader(e.File.OpenReadStream())
                .ReadToEndAsync();
            await DataService.UploadAsync(json);
        }
        UploadDialogOpen = false;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Tavenem.Blazor.MarkdownEditor;

namespace Scop.Shared;

public partial class MainLayout : IAsyncDisposable
{
    private static readonly MudTheme _Default = new()
    {
        Palette = new()
        {
            Secondary = Colors.BlueGrey.Default,
        }
    };
    private static readonly MudTheme _Dark = new()
    {
        Palette = new()
        {
            Secondary = Colors.BlueGrey.Default,
            Black = "#27272f",
            Background = "#32333d",
            BackgroundGrey = "#27272f",
            Surface = "#373740",
            DrawerBackground = "#27272f",
            DrawerText = "rgba(255,255,255, 0.50)",
            DrawerIcon = "rgba(255,255,255, 0.50)",
            AppbarBackground = "#27272f",
            AppbarText = "rgba(255,255,255, 0.70)",
            TextPrimary = "rgba(255,255,255, 0.70)",
            TextSecondary = "rgba(255,255,255, 0.50)",
            ActionDefault = "#adadb1",
            ActionDisabled = "rgba(255,255,255, 0.26)",
            ActionDisabledBackground = "rgba(255,255,255, 0.12)",
            Divider = "rgba(255,255,255, 0.12)",
            DividerLight = "rgba(255,255,255, 0.06)",
            TableLines = "rgba(255,255,255, 0.12)",
            LinesDefault = "rgba(255,255,255, 0.12)",
            LinesInputs = "rgba(255,255,255, 0.3)",
            TextDisabled = "rgba(255,255,255, 0.2)"
        }
    };

    private MarkdownEditorTheme _currentTheme = MarkdownEditorTheme.Light;
    private bool _disposed;
    private DotNetObjectReference<MainLayout>? _dotNetObjectRef;
    private MudTheme _mudTheme = _Default;
    private MarkdownEditorTheme _preferredTheme = MarkdownEditorTheme.Light;
    private MarkdownEditorTheme _theme = MarkdownEditorTheme.Auto;

    private string? ColorSchemeListenerId { get; set; }

    [Inject] private DataService? DataService { get; set; }

    [Inject] private ScopJsInterop? JsInterop { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JsInterop is not null)
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            ColorSchemeListenerId = await JsInterop
                .AddColorSchemeListener(_dotNetObjectRef);
            _preferredTheme = await JsInterop
                .GetPreferredColorScheme();
            if (_preferredTheme != MarkdownEditorTheme.Light)
            {
                _currentTheme = _preferredTheme;
                SetTheme();
                StateHasChanged();
            }

            if (DataService is not null)
            {
                DataService.GDriveSync = await JsInterop
                    .GetDriveSignedIn(_dotNetObjectRef);
                if (DataService.GDriveSync)
                {
                    StateHasChanged();
                }
            }
        }
    }

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
            if (JsInterop is not null
                && !string.IsNullOrEmpty(ColorSchemeListenerId))
            {
                await JsInterop
                    .DisposeColorSchemeListener(ColorSchemeListenerId);
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <para>
    /// Updates the theme.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be
    /// called directly.
    /// </para>
    /// </summary>
    /// <param name="value">The updated theme.</param>
    [JSInvokable]
    public void UpdateComponentTheme(MarkdownEditorTheme value)
    {
        _preferredTheme = value;
        if (_theme == MarkdownEditorTheme.Auto)
        {
            _currentTheme = value;
            SetTheme();
            StateHasChanged();
        }
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
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleThemeAsync(bool value)
    {
        _currentTheme = value
            ? MarkdownEditorTheme.Dark
            : MarkdownEditorTheme.Light;
        if (_preferredTheme == _currentTheme)
        {
            _theme = MarkdownEditorTheme.Auto;
        }
        else
        {
            _theme = _currentTheme;
        }
        SetTheme();

        if (JsInterop is not null)
        {
            await JsInterop.SetColorScheme(_theme);
        }
    }

    private void SetTheme() => _mudTheme = _currentTheme == MarkdownEditorTheme.Dark
        ? _Dark
        : _Default;
}

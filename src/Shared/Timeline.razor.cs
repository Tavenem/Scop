using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Scop.Shared;

public partial class Timeline : IAsyncDisposable
{
    private bool _disposed;
    private DotNetObjectReference<Timeline>? _dotNetObjectRef;
    private bool _initialized;
    private bool _pendingCategories;
    private bool _pendingEvents;
    private bool _pendingNow;

    /// <summary>
    /// The categories.
    /// </summary>
    [Parameter] public List<TimelineCategory>? Categories { get; set; }

    /// <summary>
    /// Invoked when the categories change.
    /// </summary>
    [Parameter] public EventCallback<List<TimelineCategory>?> CategoriesChanged { get; set; }

    /// <summary>
    /// Invoked after anything changes.
    /// </summary>
    [Parameter] public EventCallback Change { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The events.
    /// </summary>
    [Parameter] public List<TimelineEvent>? Events { get; set; }

    /// <summary>
    /// Invoked when the events change.
    /// </summary>
    [Parameter] public EventCallback<List<TimelineEvent>?> EventsChanged { get; set; }

    /// <summary>
    /// <para>
    /// The id of the HTML element.
    /// </para>
    /// <para>
    /// Will be set to a random GUID if left unset.
    /// </para>
    /// </summary>
    [Parameter] public string Id { get; set; } = Guid.NewGuid().ToString();

    private DateTime? _now;
    /// <summary>
    /// The current time.
    /// </summary>
    [Parameter]
    public DateTime? Now
    {
        get => _now;
        set
        {
            if (value.HasValue)
            {
                var time = value.Value.TimeOfDay > TimeSpan.MinValue
                    ? value.Value.TimeOfDay
                    : _nowTime;
                _nowTime = time;
                _now = time.HasValue
                    ? value.Value.Date.Add(time.Value)
                    : value;
            }
            else
            {
                _now = value;
                _nowTime = null;
            }
        }
    }

    /// <summary>
    /// Invoked when the current time changes.
    /// </summary>
    [Parameter] public EventCallback<DateTime?> NowChanged { get; set; }

    /// <summary>
    /// Invoked after the current time changes, with the old value.
    /// </summary>
    [Parameter] public EventCallback<DateTime?> NowChange { get; set; }

    /// <summary>
    /// The currently selected event.
    /// </summary>
    [Parameter] public TimelineEvent? SelectedEvent { get; set; }

    /// <summary>
    /// Invoked when the currently selected event changes.
    /// </summary>
    [Parameter] public EventCallback<TimelineEvent?> SelectedEventChanged { get; set; }

    [Inject] private ScopJsInterop? JsInterop { get; set; }

    private string? NewCategoryName { get; set; }

    private TimeSpan? _nowTime;
    private TimeSpan? NowTime
    {
        get => _nowTime;
        set
        {
            if (value.HasValue)
            {
                _nowTime = value;
                if (_now.HasValue)
                {
                    _now = _now.Value.Date.Add(value.Value);
                }
            }
            else if (_now.HasValue)
            {
                _nowTime = TimeSpan.MinValue;
                _now = _now.Value.Date;
            }
            else
            {
                _nowTime = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (JsInterop is not null)
            {
                await JsInterop.DisposeTimeline();
            }
            _dotNetObjectRef?.Dispose();
            _dotNetObjectRef = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        List<TimelineEvent>? newEvents = null;
        List<TimelineCategory>? newCategories = null;
        DateTime? newNow = null;
        if (_initialized)
        {
            if (parameters.TryGetValue<List<TimelineCategory>>(nameof(Categories), out newCategories)
                && (Categories is null
                || Categories.Count != newCategories.Count
                || (Categories.Count > 0
                && Categories[0].Id != newCategories[0].Id)))
            {
                _pendingCategories = true;
            }
            if (parameters.TryGetValue<List<TimelineEvent>>(nameof(Events), out newEvents)
                && (Events is null
                || Events.Count != newEvents.Count
                || (Events.Count > 0
                && Events[0].Id != newEvents[0].Id)))
            {
                _pendingEvents = true;
            }
            if (parameters.TryGetValue(nameof(Now), out newNow)
                && Now != newNow)
            {
                _pendingNow = true;
            }
        }
        await base.SetParametersAsync(parameters);

        if (_pendingEvents)
        {
            _pendingEvents = false;
            await SetEventsAsync(newEvents);
        }
        if (_pendingCategories)
        {
            _pendingCategories = false;
            await SetCategoriesAsync(newCategories);
        }
        if (_pendingNow)
        {
            _pendingNow = false;
            await SetNowAsync(newNow);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && JsInterop is not null)
        {
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            await JsInterop
                .InitializeTimeline(_dotNetObjectRef, Id, Now, Events, Categories);
            _initialized = true;
        }
    }

    /// <summary>
    /// <para>
    /// Adds a new category.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The new category.</param>
    [JSInvokable]
    public async Task AddCategory(TimelineCategory? value)
    {
        if (value is null || value.Id <= 0)
        {
            return;
        }

        Categories?.RemoveAll(x => x.Id == value.Id);
        (Categories ??= new()).Add(value);
        await CategoriesChanged.InvokeAsync(Categories);
        await Change.InvokeAsync();
    }

    /// <summary>
    /// <para>
    /// Adds a new event.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The new event.</param>
    [JSInvokable]
    public async Task AddEvent(TimelineEvent? value)
    {
        if (string.IsNullOrEmpty(value?.Id))
        {
            return;
        }

        Events?.RemoveAll(x => x.Id == value.Id);
        (Events ??= new()).Add(value);
        SelectedEvent = value;
        await EventsChanged.InvokeAsync(Events);
        await SelectedEventChanged.InvokeAsync(SelectedEvent);
        await Change.InvokeAsync();
    }

    /// <summary>
    /// <para>
    /// Updates the current time.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The new current time.</param>
    [JSInvokable]
    public async Task OnNowChanged(DateTime value)
    {
        if (Now != value)
        {
            var old = Now;
            Now = value.ToLocalTime();
            await NowChanged.InvokeAsync(Now);
            await NowChange.InvokeAsync(old);
        }
    }

    /// <summary>
    /// <para>
    /// Removes a category.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The new category.</param>
    [JSInvokable]
    public async Task RemoveCategory(int value)
    {
        if (value <= 0)
        {
            return;
        }

        var numRemoved = Categories?.RemoveAll(x => x.Id == value);
        if (numRemoved > 0)
        {
            await CategoriesChanged.InvokeAsync(Categories);
            await Change.InvokeAsync();
        }
    }

    /// <summary>
    /// <para>
    /// Removes an event.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The new event.</param>
    [JSInvokable]
    public async Task RemoveEvent(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var numRemoved = Events?.RemoveAll(x => x.Id == value);

        if (SelectedEvent?.Id == value)
        {
            SelectedEvent = null;
            await SelectedEventChanged.InvokeAsync(SelectedEvent);
        }

        if (numRemoved > 0)
        {
            await EventsChanged.InvokeAsync(Events);
            await Change.InvokeAsync();
        }
    }

    /// <summary>
    /// <para>
    /// Selects an event.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The selected event.</param>
    [JSInvokable]
    public Task SelectEvent(TimelineEvent? value)
    {
        SelectedEvent = string.IsNullOrEmpty(value?.Id)
            ? null
            : Events?.Find(x => x.Id == value.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets the categories.
    /// </summary>
    /// <param name="categories">The categories.</param>
    public async Task SetCategoriesAsync(List<TimelineCategory>? categories)
    {
        if (!_initialized || JsInterop is null)
        {
            return;
        }

        await JsInterop.SetTimelineCategories(categories);
    }

    /// <summary>
    /// Sets the events.
    /// </summary>
    /// <param name="events">The events.</param>
    public async Task SetEventsAsync(List<TimelineEvent>? events)
    {
        if (!_initialized || JsInterop is null)
        {
            return;
        }

        await JsInterop.SetTimelineEvents(events);
    }

    /// <summary>
    /// Sets the current time.
    /// </summary>
    /// <param name="now">The current time.</param>
    public async Task SetNowAsync(DateTime? now)
    {
        if (!_initialized || JsInterop is null)
        {
            return;
        }

        await JsInterop.SetCurrentTime(now?.ToUniversalTime());
    }

    /// <summary>
    /// <para>
    /// Updates a category.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The altered category.</param>
    [JSInvokable]
    public async Task UpdateCategory(TimelineCategory? value)
    {
        if (value is null || value.Id <= 0)
        {
            return;
        }

        Categories?.RemoveAll(x => x.Id == value.Id);
        (Categories ??= new()).Add(value);
        await CategoriesChanged.InvokeAsync(Categories);
        await Change.InvokeAsync();
    }

    /// <summary>
    /// <para>
    /// Updates an event.
    /// </para>
    /// <para>
    /// This method is invoked by internal javascript, and is not intended to be invoked otherwise.
    /// </para>
    /// </summary>
    /// <param name="value">The altered event.</param>
    [JSInvokable]
    public async Task UpdateEvent(TimelineEvent? value)
    {
        if (string.IsNullOrEmpty(value?.Id))
        {
            return;
        }

        Events?.RemoveAll(x => x.Id == value.Id);
        (Events ??= new()).Add(value);

        if (SelectedEvent?.Id == value.Id)
        {
            SelectedEvent = Events?.Find(x => x.Id == value.Id);
            await SelectedEventChanged.InvokeAsync(SelectedEvent);
        }

        await EventsChanged.InvokeAsync(Events);
        await Change.InvokeAsync();
    }

    /// <summary>
    /// Synchronize the display with the stored values.
    /// </summary>
    public async Task UpdateSelectedEventAsync()
    {
        if (SelectedEvent is not null)
        {
            await SetEventsAsync(Events);
        }
    }

    private async Task OnAddCategoryAsync()
    {
        if (!_initialized
            || JsInterop is null
            || string.IsNullOrWhiteSpace(NewCategoryName))
        {
            return;
        }

        await JsInterop.AddTimelineCategory(NewCategoryName);

        NewCategoryName = null;
    }

    private async Task OnCategoryKeydownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnAddCategoryAsync();
        }
    }

    private async Task OnSetNowAsync()
    {
        Now = DateTime.Now;
        await SetNowAsync(Now);
    }

    private async Task OnSetNowDateAsync(DateTime? date)
    {
        Now = date;
        await SetNowAsync(Now);
    }

    private async Task OnSetNowTimeAsync(TimeSpan? time)
    {
        NowTime = time;
        await SetNowAsync(Now);
    }
}

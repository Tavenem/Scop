using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Tavenem.Blazor.MarkdownEditor;

namespace Scop.Shared;

public partial class Timeline
{
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

    private IEnumerable<TimelineEvent> DisplayedEvents
    {
        get
        {
            if (Now.HasValue)
            {
                var nowEvent = new TimelineEvent()
                {
                    IsReadonly = true,
                    Markdown = "The current time",
                    Start = Now.Value,
                    Title = "Now",
                };
                if (FilteredEvents is null)
                {
                    return new TimelineEvent[] { nowEvent };
                }
                else
                {
                    return FilteredEvents
                        .Concat(new TimelineEvent[] { nowEvent })
                        .OrderBy(x => x.Start)
                        .ThenBy(x => x.End ?? x.Start);
                }
            }
            else
            {
                return FilteredEvents?
                    .OrderBy(x => x.Start)
                    .ThenBy(x => x.End ?? x.Start)
                    ?? Enumerable.Empty<TimelineEvent>();
            }
        }
    }

    private TimelineEvent? EditedEvent { get; set; }

    private HashSet<string> EditedEventCategories { get; set; } = new();

    private DateTime? EditedEventEnd { get; set; }

    private TimeSpan? EditedEventEndTime { get; set; }

    private DateTime? EditedEventStart { get; set; }

    private TimeSpan? EditedEventStartTime { get; set; }

    private IEnumerable<TimelineEvent>? FilteredEvents => Events?
        .Where(x => SelectedCategories?.Any() != true
        || (x.Categories is not null
        && SelectedCategories?.Any(y => x.Categories.Contains(y.Id)) == true));

    [CascadingParameter] private MudTheme? MudTheme { get; set; }

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

    private IEnumerable<TimelineCategory>? SelectedCategories => SelectedCategoryChips?
        .Where(x => x.Tag is TimelineCategory)
        .Select(x => (x.Tag as TimelineCategory)!);

    private MudChip[]? SelectedCategoryChips { get; set; }

    private bool ShowEditedEventEnd { get; set; }

    private bool ShowEditedEventEndTime { get; set; }

    private bool ShowEditedEventStartTime { get; set; }

    [CascadingParameter] private MarkdownEditorTheme Theme { get; set; }

    private async Task OnAddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            return;
        }
        (Categories ??= new()).Add(new() { Name = NewCategoryName.Trim() });
        NewCategoryName = null;

        await CategoriesChanged.InvokeAsync();
        await Change.InvokeAsync();
    }

    private void OnAddEvent()
    {
        var latest = Events?.Max(x => x.End ?? x.Start) ?? Now;
        if (latest.HasValue)
        {
            if (latest.Value.TimeOfDay > TimeSpan.Zero)
            {
                latest = latest.Value.AddHours(1);
            }
            else
            {
                latest = latest.Value.AddDays(1);
            }
        }
        else
        {
            latest = DateTime.Now;
        }

        (Events ??= new()).Add(new()
        {
            Start = latest.Value,
        });
    }

    private async Task OnCategoryKeydownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnAddCategoryAsync();
        }
    }

    private Task OnChangeAsync() => Change.InvokeAsync();

    private async Task OnDeleteCategoryAsync(MudChip chip)
    {
        if (chip.Tag is TimelineCategory category)
        {
            Categories?.Remove(category);
            if (Categories?.Count == 0)
            {
                Categories = null;
            }
            await CategoriesChanged.InvokeAsync();

            if (Events is not null)
            {
                foreach (var item in Events)
                {
                    item.Categories?.Remove(category.Id);
                }
            }
            await Change.InvokeAsync();
        }
    }

    private async Task OnDeleteEventAsync(TimelineEvent item)
    {
        Events?.Remove(item);
        await Change.InvokeAsync();
    }

    private async Task OnEditEventAsync(TimelineEvent? item = null)
    {
        if (EditedEvent is not null)
        {
            EditedEvent.Categories = EditedEventCategories;
            EditedEvent.Start = EditedEventStart
                ?? (EditedEventEnd.HasValue
                    && EditedEvent.Start > EditedEventEnd
                ? EditedEventEnd.Value
                : EditedEvent.Start);
            EditedEvent.End = EditedEventEnd;
            await Change.InvokeAsync();
        }

        EditedEvent = item;
        EditedEventCategories = EditedEvent?.Categories ?? new();
        EditedEventEnd = EditedEvent?.End;
        EditedEventEndTime = EditedEvent?.End?.TimeOfDay;
        EditedEventStart = EditedEvent?.Start;
        EditedEventStartTime = EditedEvent?.Start.TimeOfDay;
        ShowEditedEventEnd = EditedEventEnd.HasValue;
        ShowEditedEventEndTime = EditedEventEndTime > TimeSpan.Zero;
        ShowEditedEventStartTime = EditedEventStartTime > TimeSpan.Zero;
    }

    private void OnEditEventEndDateChanged(DateTime? value)
    {
        EditedEventEnd = value;
        if (EditedEventEnd.HasValue
            && EditedEventEndTime.HasValue
            && EditedEventEnd.Value.TimeOfDay != EditedEventEndTime.Value)
        {
            EditedEventEnd = EditedEventEnd.Value.Date.Add(EditedEventEndTime.Value);
        }
        if (EditedEventEnd < EditedEventStart)
        {
            EditedEventStart = EditedEventEnd;
            EditedEventStartTime = EditedEventEndTime;
        }
    }

    private void OnEditEventEndTimeChanged(TimeSpan? value)
    {
        EditedEventEndTime = value;
        if (EditedEventEnd.HasValue
            && EditedEventEndTime.HasValue
            && EditedEventEnd.Value.TimeOfDay != EditedEventEndTime.Value)
        {
            EditedEventEnd = EditedEventEnd.Value.Date.Add(EditedEventEndTime.Value);
        }
        if (EditedEventEnd < EditedEventStart)
        {
            EditedEventStart = EditedEventEnd;
            EditedEventStartTime = EditedEventEndTime;
        }
    }

    private void OnEditEventStartDateChanged(DateTime? value)
    {
        EditedEventStart = value;
        if (EditedEventStart.HasValue
            && EditedEventStartTime.HasValue
            && EditedEventStart.Value.TimeOfDay != EditedEventStartTime.Value)
        {
            EditedEventStart = EditedEventStart.Value.Date.Add(EditedEventStartTime.Value);
        }
        if (EditedEventStart > EditedEventEnd)
        {
            EditedEventEnd = EditedEventStart;
            EditedEventEndTime = EditedEventStartTime;
        }
    }

    private void OnEditEventStartTimeChanged(TimeSpan? value)
    {
        EditedEventStartTime = value;
        if (EditedEventStart.HasValue
            && EditedEventStartTime.HasValue
            && EditedEventStart.Value.TimeOfDay != EditedEventStartTime.Value)
        {
            EditedEventStart = EditedEventStart.Value.Date.Add(EditedEventStartTime.Value);
        }
        if (EditedEventStart > EditedEventEnd)
        {
            EditedEventEnd = EditedEventStart;
            EditedEventEndTime = EditedEventStartTime;
        }
    }

    private async Task OnSetNowAsync()
    {
        var old = Now;
        Now = DateTime.Now;
        await SetNowAsync(old);
    }

    private async Task OnSetNowDateAsync(DateTime? date)
    {
        var old = Now;
        Now = date;
        await SetNowAsync(old);
    }

    private async Task OnSetNowTimeAsync(TimeSpan? time)
    {
        var old = Now;
        NowTime = time;
        await SetNowAsync(old);
    }

    private void OnToggleShowEditedEventEnd(bool value)
    {
        ShowEditedEventEnd = value;
        if (value)
        {
            EditedEventEnd = EditedEventStart;
            EditedEventEndTime = EditedEventStartTime;
            ShowEditedEventEndTime = EditedEventEndTime > TimeSpan.Zero;
        }
        else
        {
            EditedEventEnd = null;
            EditedEventEndTime = null;
        }
    }

    private async Task SetNowAsync(DateTime? old)
    {
        await NowChanged.InvokeAsync(Now);
        await NowChange.InvokeAsync(old);
    }
}

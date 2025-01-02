using Microsoft.AspNetCore.Components;
using Scop.Models;

namespace Scop.Shared;

public partial class ScopTimeline
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

    /// <summary>
    /// The current date and time.
    /// </summary>
    [Parameter]
    public DateTimeOffset? Now { get; set; }

    /// <summary>
    /// Invoked when the current date and time changes.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> NowChanged { get; set; }

    /// <summary>
    /// Invoked after the current date and time changes, with the old value.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> NowChange { get; set; }

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
                    return [nowEvent];
                }
                else
                {
                    return FilteredEvents
                        .Concat([nowEvent])
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

    private HashSet<string> EditedEventCategories { get; set; } = [];

    private DateTimeOffset? EditedEventEnd { get; set; }

    private DateTimeOffset? EditedEventStart { get; set; }

    private IEnumerable<TimelineEvent>? FilteredEvents => SelectedCategories.Count == 0
        ? Events
        : Events?
            .Where(x
                => x.Categories is not null
                && SelectedCategories
                    .Any(y => x.Categories.Contains(y.Id)));

    private string? NewCategoryName { get; set; }

    private List<TimelineCategory> SelectedCategories { get; set; } = [];

    private bool ShowEditedEventEnd { get; set; }

    private static MarkupString GetHtml(TimelineEvent timelineEvent)
        => new(string.IsNullOrEmpty(timelineEvent.Markdown)
        ? string.Empty
        : Markdig.Markdown.ToHtml(timelineEvent.Markdown));

    private string? GetIcon(TimelineEvent timelineEvent)
    {
        if (timelineEvent == EditedEvent)
        {
            return "done";
        }
        return timelineEvent.IsReadonly ? null : "edit";
    }

    private async Task OnAddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            return;
        }
        (Categories ??= []).Add(new() { Name = NewCategoryName.Trim() });
        NewCategoryName = null;

        await CategoriesChanged.InvokeAsync();
        await Change.InvokeAsync();
    }

    private async Task OnAddEventAsync()
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
            latest = DateTimeOffset.Now;
        }
        if (Now > latest)
        {
            latest = Now;
        }

        (Events ??= []).Add(new()
        {
            Start = latest.Value,
        });
        await EventsChanged.InvokeAsync(Events);
    }

    private async Task OnDeleteCategoryAsync(TimelineCategory category)
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

    private async Task OnDeleteEventAsync(TimelineEvent item)
    {
        Events?.Remove(item);
        await EventsChanged.InvokeAsync(Events);
        await Change.InvokeAsync();
    }

    private async Task OnEditedMarkdownChangedAsync(string? value)
    {
        if (EditedEvent is null)
        {
            return;
        }
        EditedEvent.Markdown = value;
        await Change.InvokeAsync();
    }

    private async Task OnEditEventAsync(TimelineEvent item)
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

        if (EditedEvent?.Equals(item) == true)
        {
            EditedEvent = null;
            EditedEventCategories = [];
            EditedEventEnd = null;
            EditedEventStart = null;
            ShowEditedEventEnd = false;
        }
        else
        {
            EditedEvent = item;
            EditedEventCategories = item.Categories ?? [];
            EditedEventEnd = item.End;
            EditedEventStart = item.Start;
            ShowEditedEventEnd = EditedEventEnd.HasValue;
        }
    }

    private void OnEditEventEndDateChanged(DateTimeOffset? value)
    {
        EditedEventEnd = value;
        if (EditedEventEnd < EditedEventStart)
        {
            EditedEventStart = EditedEventEnd;
        }
    }

    private void OnEditEventStartDateChanged(DateTimeOffset? value)
    {
        EditedEventStart = value;
        if (EditedEventStart > EditedEventEnd)
        {
            EditedEventEnd = EditedEventStart;
        }
    }

    private async Task OnSetNowAsync(DateTimeOffset? value)
    {
        var old = Now;
        Now = value;
        await SetNowAsync(old);
    }

    private void OnToggleShowEditedEventEnd(bool value)
    {
        ShowEditedEventEnd = value;
        EditedEventEnd = value ? EditedEventStart : null;
    }

    private async Task SetNowAsync(DateTimeOffset? old)
    {
        await NowChanged.InvokeAsync(Now);
        await NowChange.InvokeAsync(old);
    }
}

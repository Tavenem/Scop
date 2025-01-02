using System.Text.Json.Serialization;

namespace Scop.Models;

public class TimelineEvent
{
    public HashSet<string>? Categories { get; set; }

    [JsonIgnore]
    public string? DisplayTime
    {
        get
        {
            if (End.HasValue)
            {
                if (End.Value.Date == Start.Date)
                {
                    return $"{Start:f} - {End.Value:t}";
                }
                else
                {
                    return $"{Start:f} - {End.Value:f}";
                }
            }
            else
            {
                return Start.ToString("f");
            }
        }
    }

    public DateTimeOffset? End { get; set; }

    [JsonIgnore] public bool IsReadonly { get; set; }

    public string? Markdown { get; set; }

    public DateTimeOffset Start { get; set; }

    public string? Title { get; set; }
}

using System.Text.Json.Serialization;

namespace Scop;

public class TimelineEvent
{
    public string? Content { get; set; }
    [JsonIgnore] public DateTime? EffectiveEnd => End?.ToLocalTime();
    [JsonIgnore] public DateTime? EffectiveStart => Start?.ToLocalTime();
    public DateTime? End { get; set; }
    public int? Group { get; set; }
    public string? Id { get; set; }
    public string? Markdown { get; set; }
    public DateTime? Start { get; set; }
}

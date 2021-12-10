using System.Text.Json.Serialization;

namespace Scop;

public class NameData : IWeighted
{
    [JsonIgnore] public double EffectiveWeight => Weight ?? 1;
    public string? Name { get; set; }
    public double? Weight { get; set; }
}

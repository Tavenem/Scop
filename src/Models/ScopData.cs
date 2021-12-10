namespace Scop;

public class ScopData
{
    public HashSet<Ethnicity>? Ethnicities { get; set; }

    public DateTime LastSync { get; set; }

    public List<Story> Stories { get; set; } = new();

    public HashSet<Trait>? Traits { get; set; }
}

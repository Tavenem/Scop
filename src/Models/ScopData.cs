namespace Scop.Models;

public class ScopData
{
    public const int CurrentVersion = 2;

    public List<Ethnicity>? Ethnicities { get; set; }

    public List<Genre>? Genres { get; set; }

    public DateTime LastSync { get; set; }

    public List<Plot>? Plots { get; set; }

    public List<RelationshipType>? RelationshipTypes { get; set; }

    public List<Story> Stories { get; set; } = [];

    public List<Trait>? StoryTraits { get; set; }

    public List<Trait>? Traits { get; set; }

    public int Version { get; set; }
}

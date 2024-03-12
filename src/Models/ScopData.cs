using Scop.Models;

namespace Scop;

public class ScopData
{
    public HashSet<Ethnicity>? Ethnicities { get; set; }

    public List<Genre>? Genres { get; set; }

    public DateTime LastSync { get; set; }

    public List<Plot>? Plots { get; set; }

    public List<Story> Stories { get; set; } = [];

    public HashSet<Trait>? Traits { get; set; }
}

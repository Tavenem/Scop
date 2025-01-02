namespace Scop.Models;

public class Genre : IEquatable<Genre>
{
    public List<string>? Features { get; set; }

    public string? Name { get; set; }

    public List<string>? Protagonists { get; set; }

    public List<string>? ProtagonistTraits { get; set; }

    public List<string>? SecondaryCharacters { get; set; }

    public List<string>? SecondaryCharacterTraits { get; set; }

    public List<string>? Settings { get; set; }

    public List<string>? Subgenres { get; set; }

    public List<string>? Subjects { get; set; }

    public List<string>? Themes { get; set; }

    public bool Equals(Genre? other) => other is not null
        && other.Name?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true;

    public override bool Equals(object? obj) => Equals(obj as Genre);

    public override int GetHashCode() => Name?.GetHashCode() ?? 0;

    public static bool operator ==(Genre? left, Genre? right) => EqualityComparer<Genre>.Default.Equals(left, right);
    public static bool operator !=(Genre? left, Genre? right) => !(left == right);
}

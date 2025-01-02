namespace Scop.Models;

public class Plot : IEquatable<Plot>
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public bool Equals(Plot? other) => other is not null
        && other.Name?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true;

    public override bool Equals(object? obj) => Equals(obj as Plot);

    public override int GetHashCode() => Name?.GetHashCode() ?? 0;

    public static bool operator ==(Plot? left, Plot? right) => EqualityComparer<Plot>.Default.Equals(left, right);
    public static bool operator !=(Plot? left, Plot? right) => !(left == right);
}

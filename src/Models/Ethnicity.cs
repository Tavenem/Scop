using System.Text.Json.Serialization;
using Tavenem.Randomize;

namespace Scop;

public class Ethnicity : IEquatable<Ethnicity>, IJsonOnDeserialized
{
    public bool HasNames { get; set; }

    [JsonIgnore] public string[]? Hierarchy { get; set; }

    public bool IsDefault { get; set; }

    [JsonIgnore] public bool IsEditing { get; set; }

    [JsonIgnore] public string? NewEthnicityValue { get; set; }

    [JsonIgnore] public Ethnicity? Parent { get; set; }

    public string? Type { get; set; }

    public List<Ethnicity>? Types { get; set; }

    public bool UserDefined { get; set; }

    public static List<string[]> GetRandomEthnicities(List<Ethnicity> ethnicities)
    {
        if (ethnicities.Count == 0)
        {
            return [];
        }

        var paths = new List<string[]>();

        var sanityCheck = 0;
        do
        {
            var ethnicity = GetRandomEthnicity(ethnicities);
            if (ethnicity is null)
            {
                break;
            }
            paths.Add(ethnicity);

            sanityCheck++;
        } while (Randomizer.Instance.NextDouble() < 0.1 && sanityCheck < 5);

        return paths;
    }

    public bool HasUserDefined()
    {
        if (UserDefined)
        {
            return true;
        }
        if (Types is not null)
        {
            foreach (var child in Types)
            {
                if (child.HasUserDefined())
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool Equals(Ethnicity? other)
    {
        if (other is null)
        {
            return false;
        }
        return Hierarchy is null
            ? other.Hierarchy is null
            : other.Hierarchy is not null
            && Hierarchy.SequenceEqual(other.Hierarchy);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>
    /// <see langword="true" /> if the specified object is equal to the current
    /// object; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => Equals(obj as Ethnicity);

    public override int GetHashCode() => HashCode.Combine(Hierarchy);

    internal static string[]? GetRandomEthnicity(List<Ethnicity> ethnicities)
    {
        var set = Randomizer.Instance.Next(ethnicities);

        if (set?.Types?.Count > 0)
        {
            var child = GetRandomEthnicity(set.Types);
            if (child is not null)
            {
                return child;
            }
        }

        return set?.Hierarchy;
    }

    public void OnDeserialized() => Initialize();

    internal void InitializeChildren()
    {
        if (Types is not null)
        {
            foreach (var child in Types)
            {
                child.Parent = this;
                child.Initialize(Hierarchy);
            }
        }
    }

    private void Initialize(string[]? parentHierarchy = null)
    {
        var hierarchy = new string[(parentHierarchy?.Length ?? 0) + 1];
        if (parentHierarchy is not null)
        {
            Array.Copy(parentHierarchy, hierarchy, parentHierarchy.Length);
        }
        hierarchy[^1] = Type ?? "unknown";
        Hierarchy = hierarchy;

        InitializeChildren();
    }

    public static bool operator ==(Ethnicity? left, Ethnicity? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(Ethnicity? left, Ethnicity? right) => !(left == right);
}

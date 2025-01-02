using Scop.Enums;
using System.Text.Json.Serialization;
using Tavenem.Randomize;

namespace Scop.Models;

public class Trait : IEquatable<Trait>, IJsonOnDeserialized
{
    public bool CanChooseNone { get; set; }

    public List<Trait>? Children { get; set; }

    public ChoiceType ChoiceType { get; set; }

    [JsonIgnore] public double CurrentEffectiveWeight { get; set; }

    [JsonIgnore] public double EffectiveNoneWeight => NoneWeight ?? 1;

    [JsonIgnore] public double EffectiveWeight => Weight ?? 1;

    [JsonIgnore] public string[]? Hierarchy { get; set; }

    /// <summary>
    /// Whether this option is selected if no selection is made among its children.
    /// </summary>
    public bool IsChosenOnNone { get; set; }

    public List<TraitModifier>? Modifiers { get; set; }

    public string? Name { get; set; }

    /// <summary>
    /// Indicates the weight of selecting no option if <see cref="ChoiceType"/> is <see
    /// cref="ChoiceType.Single"/> or <see cref="ChoiceType.OneOrMore"/>.
    /// </summary>
    /// <remarks>Ignored if <see cref="ChoiceType"/> has any other value.</remarks>
    public double? NoneWeight { get; set; }

    [JsonIgnore] public Trait? Parent { get; set; }

    public double? Weight { get; set; }

    public bool Equals(Trait? other)
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
    public override bool Equals(object? obj) => Equals(obj as Trait);

    public override int GetHashCode() => HashCode.Combine(Hierarchy);

    public double GetWeight(TraitContainer container)
    {
        if (Modifiers is null)
        {
            return EffectiveWeight;
        }
        var modifiers = Modifiers.Where(x => x.Applies(container)).ToList();
        if (modifiers.Any(x => x.Force))
        {
            return double.PositiveInfinity;
        }
        return modifiers
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Weight)
            .FirstOrDefault()?
            .EffectiveWeight
            ?? EffectiveWeight;
    }

    public void OnDeserialized() => Initialize();

    public bool Randomize(TraitContainer container, bool forceSelect = false)
    {
        if (Children is null
            || Children.Count == 0)
        {
            if (Hierarchy is not null)
            {
                container.AddTrait(Hierarchy);
            }
            return true;
        }

        var anyChildren = false;
        switch (ChoiceType)
        {
            case ChoiceType.Category:
                foreach (var child in Children)
                {
                    var weight = child.GetWeight(container);
                    if (weight > 0)
                    {
                        anyChildren |= child.Randomize(container);
                    }
                }
                break;
            case ChoiceType.Single:
            case ChoiceType.OneOrMore:
                anyChildren = SelectAmongChildren(container);
                break;
            case ChoiceType.Multiple:
                anyChildren = SelectMultipleChildren(container);
                break;
        }
        if (forceSelect || anyChildren || IsChosenOnNone)
        {
            if (Hierarchy is not null)
            {
                container.AddTrait(Hierarchy);
            }
            return true;
        }
        return false;
    }

    public void Select(bool value, TraitContainer container)
    {
        if (Hierarchy is null || Hierarchy.Length == 0)
        {
            return;
        }
        if (!value)
        {
            container.RemoveTrait(Hierarchy);
            return;
        }

        if (Parent?.Hierarchy is not null
            && Parent.ChoiceType == ChoiceType.Single
            && Parent.Children?.Count > 1)
        {
            foreach (var child in Parent.Children)
            {
                if (child.Hierarchy is not null
                    && !child.Equals(this))
                {
                    container.RemoveTrait(child.Hierarchy);
                }
            }
        }
        Randomize(container, true);
    }

    internal void InitializeChildren()
    {
        if (Children is not null)
        {
            foreach (var child in Children)
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
        hierarchy[^1] = Name ?? "unknown";
        Hierarchy = hierarchy;

        InitializeChildren();
    }

    private bool SelectAmongChildren(TraitContainer container)
    {
        if (Hierarchy is null
            || Children is null
            || Children.Count == 0)
        {
            return false;
        }

        if (container.TraitPaths?.Any(x => x.StartsWith(Hierarchy)) == true)
        {
            return true;
        }

        foreach (var child in Children)
        {
            child.CurrentEffectiveWeight = child.GetWeight(container);
        }

        var candidates = Children
            .Where(x => x.CurrentEffectiveWeight > 0)
            .ToList();
        var forced = candidates
            .Where(x => double.IsPositiveInfinity(x.CurrentEffectiveWeight))
            .ToList();
        while (forced.Count > 0)
        {
            var child = Randomizer.Instance.Next(forced)!;
            var selected = child.Randomize(container);
            if (selected)
            {
                return true;
            }
            forced.Remove(child);
            candidates.Remove(child);
        }

        while (candidates.Count > 0)
        {
            var total = candidates.Sum(x => x.CurrentEffectiveWeight);
            if (CanChooseNone)
            {
                total += EffectiveNoneWeight;
            }

            var choice = Randomizer.Instance.NextDouble() * total;

            if (CanChooseNone)
            {
                choice -= EffectiveNoneWeight;
                if (choice <= 0)
                {
                    return false;
                }
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                choice -= candidates[i].CurrentEffectiveWeight;
                if (choice <= 0)
                {
                    var selected = candidates[i].Randomize(container);
                    if (selected)
                    {
                        return true;
                    }
                    candidates.RemoveAt(i);
                    break;
                }
            }
        }
        return false;
    }

    private bool SelectMultipleChildren(TraitContainer container)
    {
        if (Children is null
            || Children.Count == 0)
        {
            return false;
        }

        var any = false;
        foreach (var child in Children)
        {
            if (Randomizer.Instance.NextDouble() <= child.GetWeight(container))
            {
                any = child.Randomize(container);
            }
        }
        return any;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string? ToString() => Name ?? base.ToString();

    public static bool operator ==(Trait? left, Trait? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Trait? left, Trait? right) => !(left == right);
}

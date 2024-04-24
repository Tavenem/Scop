using System.Text;
using System.Text.Json.Serialization;

namespace Scop;

public abstract class TraitContainer
{
    [JsonIgnore] public string? DisplayTraits { get; set; }

    public List<string[]>? TraitPaths { get; set; }

    public void AddTrait(string[] trait)
    {
        (TraitPaths ??= []).Add(trait);
        SetDisplayTraits();
    }

    public void ClearTraits()
    {
        TraitPaths = null;
        SetDisplayTraits();
    }

    public bool HasTrait(Trait trait) => trait.Hierarchy is not null
        && TraitPaths?.Any(x => x.StartsWith(trait.Hierarchy)) == true;

    public void RemoveTrait(string[] trait)
    {
        TraitPaths?.RemoveAll(x => x.StartsWith(trait));
        SetDisplayTraits();
    }

    public void SetDisplayTraits()
    {
        if (TraitPaths is null
            || TraitPaths.Count == 0)
        {
            DisplayTraits = null;
            return;
        }

        var traits = new List<Trait>();
        foreach (var path in TraitPaths)
        {
            if (path.Length == 0)
            {
                continue;
            }
            var trait = traits
                .Find(x => string.Equals(x.Name, path[0], StringComparison.OrdinalIgnoreCase));
            if (trait is null)
            {
                trait = new()
                {
                    Name = path[0],
                };
                traits.Add(trait);
            }
            var parent = trait;
            var i = 1;
            while (path.Length > i)
            {
                parent.Children ??= [];
                trait = parent.Children
                    .Find(x => string.Equals(x.Name, path[i], StringComparison.OrdinalIgnoreCase));
                if (trait is null)
                {
                    trait = new()
                    {
                        Name = path[i],
                    };
                    parent.Children.Add(trait);
                }
                i++;
                parent = trait;
            }
        }

        if (traits.Count == 0)
        {
            DisplayTraits = null;
            return;
        }

        static void AppendTraitChildren(StringBuilder sb, List<Trait> traits)
        {
            sb.Append("<ul style=\"padding-inline-start:1rem\">");
            for (var i = 0; i < traits.Count; i++)
            {
                sb.Append("<li>");
                sb.Append(traits[i].Name);
                var childSet = traits[i].Children;
                while (childSet?.Count > 0)
                {
                    if (childSet.Count == 1)
                    {
                        sb.Append(": ")
                            .Append(childSet[0].Name);
                        childSet = childSet[0].Children;
                    }
                    else
                    {
                        AppendTraitChildren(sb, childSet!);
                        break;
                    }
                }
                sb.Append("</li>");
            }
            sb.Append("</ul>");
        }

        var sb = new StringBuilder();
        AppendTraitChildren(sb, traits);
        DisplayTraits = sb.ToString();
    }
}

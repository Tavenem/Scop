using Microsoft.AspNetCore.Components;
using Scop.Models;
using Scop.Services;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;

namespace Scop.Shared;

public partial class TraitTree
{
    [Parameter] public TraitContainer? TraitContainer { get; set; }

    [Inject, NotNull] private DataService? DataService { get; set; }

    [Inject, NotNull] private DialogService? DialogService { get; set; }

    private List<Trait>? Traits => TraitContainer is Story
        ? DataService.Data.StoryTraits
        : DataService.Data.Traits;

    public async Task OnRandomizeTraitsAsync(bool reset = true, bool deferSave = false)
    {
        if (TraitContainer is null)
        {
            return;
        }

        if (reset)
        {
            TraitContainer.ClearTraits();
        }
        if (Traits is not null)
        {
            foreach (var trait in Traits)
            {
                trait.Randomize(TraitContainer);
            }
        }
        if (!deferSave)
        {
            await DataService.SaveAsync();
        }
    }

    private async Task OnDeleteTraitAsync(Trait trait)
    {
        if (Traits?.Remove(trait) == true)
        {
            await DataService.SaveAsync();
        }
    }

    private async Task OnEditTraitAsync(Trait trait)
    {
        await DialogService.Show<TraitDialog>("Trait", new DialogParameters
        {
            { nameof(TraitDialog.Trait), trait },
        }).Result;

        await DataService.SaveAsync();
    }

    private async Task OnNewTraitAsync()
    {
        const string NewBaseName = "New trait";
        var newName = NewBaseName;
        var i = 0;
        while (Traits?.Any(x => string.Equals(
            x.Name,
            newName,
            StringComparison.OrdinalIgnoreCase)) == true)
        {
            newName = $"{NewBaseName} ({i++})";
        }

        var newTrait = new Trait
        {
            Hierarchy = [newName],
            Name = newName,
        };

        if (TraitContainer is Story)
        {
            (DataService.Data.StoryTraits ??= []).Add(newTrait);
        }
        else
        {
            (DataService.Data.Traits ??= []).Add(newTrait);
        }
        await DataService.SaveAsync();
    }

    private async Task OnNewTraitAsync(Trait parent)
    {
        const string NewBaseName = "New trait";
        var newName = NewBaseName;
        var i = 0;
        while (parent
            .Children?
            .Any(x => string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase)) == true)
        {
            newName = $"{NewBaseName} ({i++})";
        }

        var hierarchy = new string[(parent.Hierarchy?.Length ?? 0) + 1];
        if (parent.Hierarchy is not null)
        {
            Array.Copy(parent.Hierarchy, hierarchy, parent.Hierarchy.Length);
        }
        hierarchy[^1] = newName;

        var newTrait = new Trait
        {
            Hierarchy = hierarchy,
            Parent = parent,
            Name = newName,
        };

        (parent.Children ??= []).Add(newTrait);
        await DataService.SaveAsync();
    }

    private async Task OnTraitSelectAsync(bool value, Trait? trait)
    {
        if (TraitContainer is not null && trait is not null)
        {
            trait.Select(value, TraitContainer);
            await DataService.SaveAsync();
        }
    }
}
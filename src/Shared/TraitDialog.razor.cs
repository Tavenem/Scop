using Microsoft.AspNetCore.Components;
using Tavenem.Blazor.Framework;

namespace Scop.Shared;

public partial class TraitDialog
{
    [Parameter] public Trait Trait { get; set; } = new();

    [CascadingParameter] private DialogInstance? Dialog { get; set; }

    [Inject] private DialogService DialogService { get; set; } = default!;

    private void OnDeleteModifier(int index)
    {
        if (Trait.Modifiers is null)
        {
            return;
        }
        Trait.Modifiers.RemoveAt(index);
        if (Trait.Modifiers.Count == 0)
        {
            Trait.Modifiers = null;
        }
    }

    private async Task OnEditModifierAsync(TraitModifier? modifier)
    {
        var parameters = new DialogParameters();
        if (modifier is not null)
        {
            parameters.Add(nameof(TraitModifierDialog.Modifier), modifier);
        }
        var result = await DialogService
            .Show<TraitModifierDialog>("Trait Modifier", parameters)
            .Result;
        if (result?.Choice == DialogChoice.Ok
            && result.Data is TraitModifier traitModifier)
        {
            (Trait.Modifiers ??= new()).Add(traitModifier);
        }
    }
}

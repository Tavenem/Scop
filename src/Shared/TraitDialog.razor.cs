using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Scop.Shared;

public partial class TraitDialog
{
    [Parameter] public Trait Trait { get; set; } = new();

    [Inject] private IDialogService? DialogService { get; set; }

    [CascadingParameter] private MudDialogInstance? MudDialog { get; set; }

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
        if (DialogService is null)
        {
            return;
        }

        var parameters = new DialogParameters();
        if (modifier is not null)
        {
            parameters.Add(nameof(TraitModifierDialog.Modifier), modifier);
        }
        var dialog = DialogService.Show<TraitModifierDialog>("Trait Modifier", parameters);
        var newModifier = await dialog.Result;
        if (modifier is null
            && newModifier?.Data is TraitModifier traitModifier)
        {
            (Trait.Modifiers ??= new()).Add(traitModifier);
        }
    }
}

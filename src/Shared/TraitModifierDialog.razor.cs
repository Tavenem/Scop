using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Scop.Shared;

public partial class TraitModifierDialog
{
    [Parameter] public TraitModifier Modifier { get; set; } = new();

    [CascadingParameter] private MudDialogInstance? MudDialog { get; set; }

    private string? NewEthnicity { get; set; }

    private string? NewTargetPath { get; set; }

    private void OnEditEthnicity()
    {
        if (Modifier.Ethnicities is not null)
        {
            Modifier.Ethnicities.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            if (Modifier.Ethnicities.Count == 0)
            {
                Modifier.Ethnicities = null;
            }
        }
    }

    private void OnEditTargetPath()
    {
        if (Modifier.TargetPaths is not null)
        {
            Modifier.TargetPaths.RemoveAll(x => string.IsNullOrWhiteSpace(x));
            if (Modifier.TargetPaths.Count == 0)
            {
                Modifier.TargetPaths = null;
            }
        }
    }

    private void OnRemoveEthnicity(int index)
    {
        if (Modifier.Ethnicities is not null)
        {
            Modifier.Ethnicities.RemoveAt(index);
            if (Modifier.Ethnicities.Count == 0)
            {
                Modifier.Ethnicities = null;
            }
        }
    }

    private void OnRemoveTargetPath(int index)
    {
        if (Modifier.TargetPaths is not null)
        {
            Modifier.TargetPaths.RemoveAt(index);
            if (Modifier.TargetPaths.Count == 0)
            {
                Modifier.TargetPaths = null;
            }
        }
    }

    private void OnSetNewEthnicity()
    {
        if (string.IsNullOrWhiteSpace(NewEthnicity))
        {
            return;
        }

        (Modifier.Ethnicities ??= new()).Add(NewEthnicity.Trim());
        NewEthnicity = null;
    }

    private void OnSetNewTargetPath()
    {
        if (string.IsNullOrWhiteSpace(NewTargetPath))
        {
            return;
        }

        (Modifier.TargetPaths ??= new()).Add(NewTargetPath.Trim());
        NewTargetPath = null;
    }
}

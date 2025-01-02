using Microsoft.AspNetCore.Components;
using Scop.Models;
using Tavenem.Blazor.Framework;

namespace Scop.Shared;

public partial class TraitModifierDialog
{
    [Parameter] public TraitModifier Modifier { get; set; } = new();

    [CascadingParameter] private DialogInstance? Dialog { get; set; }

    private string? NewEthnicity { get; set; }

    private string? NewTargetPath { get; set; }

    private void OnEditEthnicity(int index, string? value)
    {
        if (Modifier.Ethnicities is not null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Modifier.Ethnicities.RemoveAt(index);
                if (Modifier.Ethnicities.Count == 0)
                {
                    Modifier.Ethnicities = null;
                }
            }
            else
            {
                Modifier.Ethnicities[index] = value;
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

    private void OnRemoveTargetPath(string path)
    {
        if (Modifier.TargetPaths is not null)
        {
            Modifier.TargetPaths.Remove(path);
            if (Modifier.TargetPaths.Count == 0)
            {
                Modifier.TargetPaths = null;
            }
        }
    }

    private void OnSetNewEthnicity(string? newValue)
    {
        NewEthnicity = newValue;
        if (string.IsNullOrWhiteSpace(NewEthnicity))
        {
            return;
        }

        (Modifier.Ethnicities ??= []).Add(NewEthnicity.Trim());
        NewEthnicity = null;
    }

    private void OnSetNewTargetPath(string? newValue)
    {
        NewTargetPath = newValue;
        if (string.IsNullOrWhiteSpace(NewTargetPath))
        {
            return;
        }

        (Modifier.TargetPaths ??= []).Add(NewTargetPath.Trim());
        NewTargetPath = null;
    }

    private void OnTargetPathValueChanged(string oldPath, string newPath)
    {
        if (Modifier.TargetPaths is null)
        {
            return;
        }

        var index = Modifier.TargetPaths.IndexOf(oldPath);
        if (index == -1)
        {
            return;
        }

        Modifier.TargetPaths.RemoveAt(index);
        if (!string.IsNullOrWhiteSpace(newPath))
        {
            Modifier.TargetPaths.Insert(index, newPath);
        }

        if (Modifier.TargetPaths.Count == 0)
        {
            Modifier.TargetPaths = null;
        }
    }
}

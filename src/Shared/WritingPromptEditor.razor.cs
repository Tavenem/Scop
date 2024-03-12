using Microsoft.AspNetCore.Components;
using Scop.Models;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;

namespace Scop.Shared;

public partial class WritingPromptEditor
{
    [Inject, NotNull] private DataService? DataService { get; set; }

    [CascadingParameter] private DialogInstance? Dialog { get; set; }

    private string? EditedFeature { get; set; }

    private Genre? EditedGenre { get; set; }

    private Plot? EditedPlot { get; set; }

    private string? EditedProtagonist { get; set; }

    private string? EditedProtagonistTrait { get; set; }

    private string? EditedSecondaryCharacter { get; set; }

    private string? EditedSecondaryCharacterTrait { get; set; }

    private string? EditedSetting { get; set; }

    private string? EditedSubgenre { get; set; }

    private string? EditedSubject { get; set; }

    private string? EditedTheme { get; set; }

    private bool IsFeaturesOpen { get; set; }

    private bool IsGenresOpen { get; set; }

    private bool IsPlotsOpen { get; set; }

    private bool IsProtagonistsOpen { get; set; }

    private bool IsProtagonistTraitsOpen { get; set; }

    private bool IsSecondaryCharactersOpen { get; set; }

    private bool IsSecondaryCharacterTraitsOpen { get; set; }

    private bool IsSettingsOpen { get; set; }

    private bool IsSubgenresOpen { get; set; }

    private bool IsSubjectsOpen { get; set; }

    private bool IsThemesOpen { get; set; }

    private string? NewFeature { get; set; }

    private string? NewGenre { get; set; }

    private string? NewProtagonist { get; set; }

    private string? NewProtagonistTrait { get; set; }

    private string? NewSecondaryCharacter { get; set; }

    private string? NewSecondaryCharacterTrait { get; set; }

    private string? NewSetting { get; set; }

    private string? NewSubgenre { get; set; }

    private string? NewSubject { get; set; }

    private string? NewTheme { get; set; }

    private async Task OnAddFeatureAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewFeature))
        {
            return;
        }

        var value = NewFeature.Trim();
        if (EditedGenre.Features?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Features ??= []).Add(value);

        NewFeature = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddGenreAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGenre))
        {
            return;
        }

        var value = NewGenre.Trim();
        if (DataService.Genres?.Any(x
            => x.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true) == true)
        {
            return;
        }

        NewGenre = null;

        EditedGenre = new()
        {
            Name = value,
            UserDefined = true,
        };

        await DataService.AddGenreAsync(EditedGenre);
    }

    private void OnAddPlot() => EditedPlot = new();

    private async Task OnAddProtagonistAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewProtagonist))
        {
            return;
        }

        var value = NewProtagonist.Trim();
        if (EditedGenre.Protagonists?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Protagonists ??= []).Add(value);

        NewProtagonist = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddProtagonistTraitAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewProtagonistTrait))
        {
            return;
        }

        var value = NewProtagonistTrait.Trim();
        if (EditedGenre.ProtagonistTraits?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.ProtagonistTraits ??= []).Add(value);

        NewProtagonistTrait = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddSecondaryCharacterAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewSecondaryCharacter))
        {
            return;
        }

        var value = NewSecondaryCharacter.Trim();
        if (EditedGenre.SecondaryCharacters?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.SecondaryCharacters ??= []).Add(value);

        NewSecondaryCharacter = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddSecondaryCharacterTraitAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewSecondaryCharacterTrait))
        {
            return;
        }

        var value = NewSecondaryCharacterTrait.Trim();
        if (EditedGenre.SecondaryCharacterTraits?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.SecondaryCharacterTraits ??= []).Add(value);

        NewSecondaryCharacterTrait = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddSettingAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewSetting))
        {
            return;
        }

        var value = NewSetting.Trim();
        if (EditedGenre.Settings?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Settings ??= []).Add(value);

        NewSetting = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddSubgenreAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewSubgenre))
        {
            return;
        }

        var value = NewSubgenre.Trim();
        if (EditedGenre.Subgenres?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Subgenres ??= []).Add(value);

        NewSubgenre = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddSubjectAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewSubject))
        {
            return;
        }

        var value = NewSubject.Trim();
        if (EditedGenre.Subjects?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Subjects ??= []).Add(value);

        NewSubject = null;

        await UpdateGenreAsync();
    }

    private async Task OnAddThemeAsync()
    {
        if (EditedGenre is null
            || string.IsNullOrWhiteSpace(NewTheme))
        {
            return;
        }

        var value = NewTheme.Trim();
        if (EditedGenre.Themes?.Contains(value) == true)
        {
            return;
        }

        (EditedGenre.Themes ??= []).Add(value);

        NewTheme = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeFeatureAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedFeature)
            || EditedGenre?.Features?.Contains(EditedFeature) != true
            || EditedGenre.Features.Contains(value))
        {
            return;
        }

        EditedGenre.Features.Remove(EditedFeature);
        EditedGenre.Features.Add(value);

        EditedFeature = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeGenreAsync(string? value)
    {
        if (EditedGenre is not null)
        {
            await DataService.EditGenreAsync(EditedGenre, value);
        }
    }

    private async Task OnChangeProtagonistAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedProtagonist)
            || EditedGenre?.Protagonists?.Contains(EditedProtagonist) != true
            || EditedGenre.Protagonists.Contains(value))
        {
            return;
        }

        EditedGenre.Protagonists.Remove(EditedProtagonist);
        EditedGenre.Protagonists.Add(value);

        EditedProtagonist = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeProtagonistTraitAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedProtagonistTrait)
            || EditedGenre?.ProtagonistTraits?.Contains(EditedProtagonistTrait) != true
            || EditedGenre.ProtagonistTraits.Contains(value))
        {
            return;
        }

        EditedGenre.ProtagonistTraits.Remove(EditedProtagonistTrait);
        EditedGenre.ProtagonistTraits.Add(value);

        EditedProtagonistTrait = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeSecondaryCharacterAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedSecondaryCharacter)
            || EditedGenre?.SecondaryCharacters?.Contains(EditedSecondaryCharacter) != true
            || EditedGenre.SecondaryCharacters.Contains(value))
        {
            return;
        }

        EditedGenre.SecondaryCharacters.Remove(EditedSecondaryCharacter);
        EditedGenre.SecondaryCharacters.Add(value);

        EditedSecondaryCharacter = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeSecondaryCharacterTraitAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedSecondaryCharacterTrait)
            || EditedGenre?.SecondaryCharacterTraits?.Contains(EditedSecondaryCharacterTrait) != true
            || EditedGenre.SecondaryCharacterTraits.Contains(value))
        {
            return;
        }

        EditedGenre.SecondaryCharacterTraits.Remove(EditedSecondaryCharacterTrait);
        EditedGenre.SecondaryCharacterTraits.Add(value);

        EditedSecondaryCharacterTrait = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeSettingAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedSetting)
            || EditedGenre?.Settings?.Contains(EditedSetting) != true
            || EditedGenre.Settings.Contains(value))
        {
            return;
        }

        EditedGenre.Settings.Remove(EditedSetting);
        EditedGenre.Settings.Add(value);

        EditedSetting = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeSubgenreAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedSubgenre)
            || EditedGenre?.Subgenres?.Contains(EditedSubgenre) != true
            || EditedGenre.Subgenres.Contains(value))
        {
            return;
        }

        EditedGenre.Subgenres.Remove(EditedSubgenre);
        EditedGenre.Subgenres.Add(value);

        EditedSubgenre = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeSubjectAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedSubject)
            || EditedGenre?.Subjects?.Contains(EditedSubject) != true
            || EditedGenre.Subjects.Contains(value))
        {
            return;
        }

        EditedGenre.Subjects.Remove(EditedSubject);
        EditedGenre.Subjects.Add(value);

        EditedSubject = null;

        await UpdateGenreAsync();
    }

    private async Task OnChangeThemeAsync(string? value)
    {
        value = value?.Trim();

        if (string.IsNullOrEmpty(value)
            || string.IsNullOrEmpty(EditedTheme)
            || EditedGenre?.Themes?.Contains(EditedTheme) != true
            || EditedGenre.Themes.Contains(value))
        {
            return;
        }

        EditedGenre.Themes.Remove(EditedTheme);
        EditedGenre.Themes.Add(value);

        EditedTheme = null;

        await UpdateGenreAsync();
    }

    private async Task OnDeleteFeatureAsync(string value)
    {
        if (EditedGenre?.Features?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private Task OnDeleteGenreAsync(Genre value)
        => DataService.RemoveGenreAsync(value);

    private Task OnDeletePlotAsync(Plot value)
        => DataService.RemovePlotAsync(value);

    private async Task OnDeleteProtagonistAsync(string value)
    {
        if (EditedGenre?.Protagonists?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteProtagonistTraitAsync(string value)
    {
        if (EditedGenre?.ProtagonistTraits?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteSecondaryCharacterAsync(string value)
    {
        if (EditedGenre?.SecondaryCharacters?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteSecondaryCharacterTraitAsync(string value)
    {
        if (EditedGenre?.SecondaryCharacterTraits?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteSettingAsync(string value)
    {
        if (EditedGenre?.Settings?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteSubgenreAsync(string value)
    {
        if (EditedGenre?.Subgenres?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteSubjectAsync(string value)
    {
        if (EditedGenre?.Subjects?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnDeleteThemeAsync(string value)
    {
        if (EditedGenre?.Themes?.Remove(value) == true)
        {
            await UpdateGenreAsync();
        }
    }

    private async Task OnPlotDescriptionChangeAsync(Plot plot, string? value)
    {
        plot.Description = value?.Trim();
        await DataService.EditPlotAsync(plot);
    }

    private Task OnPlotNameChangeAsync(Plot plot, string? value)
        => DataService.EditPlotAsync(plot, value);

    private async Task UpdateGenreAsync()
    {
        if (EditedGenre is not null)
        {
            await DataService.EditGenreAsync(EditedGenre);
        }
    }
}
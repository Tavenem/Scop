using Microsoft.AspNetCore.Components;
using Scop.Models;
using Scop.Services;
using System.Diagnostics.CodeAnalysis;
using Tavenem.Blazor.Framework;
using Tavenem.Randomize;

namespace Scop.Shared;

public partial class WritingPromptDialog
{
    [Parameter] public WritingPrompt WritingPrompt { get; set; } = new();

    [Inject, NotNull] private DataService? DataService { get; set; }

    [CascadingParameter] private DialogInstance? Dialog { get; set; }

    [Inject, NotNull] private DialogService? DialogService { get; set; }

    private int EditingFeatureIndex { get; set; } = -1;

    private int EditingProtagonistTraitIndex { get; set; } = -1;

    private int EditingSecondaryCharacterIndex { get; set; } = -1;

    private int EditingSecondaryCharacterForTraitIndex { get; set; } = -1;

    private int EditingSecondaryCharacterTraitIndex { get; set; } = -1;

    private int EditingSettingIndex { get; set; } = -1;

    private int EditingSubjectIndex { get; set; } = -1;

    private int EditingThemeIndex { get; set; } = -1;

    private Genre? Genre { get; set; }

    private bool HasPrompt => !string.IsNullOrEmpty(WritingPrompt.Genre)
        || !string.IsNullOrEmpty(WritingPrompt.Plot?.Name);

    private bool IsEditingGenre { get; set; }

    private bool IsEditingProtagonist { get; set; }

    private bool IsEditingSubgenre { get; set; }

    private bool IsFeaturesLocked { get; set; }

    private bool IsGenreLocked { get; set; }

    private bool IsPlotLocked { get; set; }

    private bool IsProtagonistLocked { get; set; }

    private bool IsProtagonistTraitsLocked { get; set; }

    private bool IsSecondaryCharactersLocked { get; set; }

    private bool IsSettingsLocked { get; set; }

    private bool IsSubgenreLocked { get; set; }

    private bool IsSubjectsLocked { get; set; }

    private bool IsThemesLocked { get; set; }

    private SecondaryCharacter? GetRandomSecondaryCharacter(bool unique = false)
    {
        if (Genre is null
            || !(Genre?.SecondaryCharacters?.Count > 0))
        {
            return null;
        }

        var choices = unique && WritingPrompt.SecondaryCharacters is not null
            ? [.. Genre
                .SecondaryCharacters
                .Except(WritingPrompt
                    .SecondaryCharacters
                    .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                    .Select(x => x.Description)
                    .OfType<string>())]
            : Genre.SecondaryCharacters;

        if (choices.Count == 0)
        {
            return null;
        }

        var description = Randomizer.Instance.Next(choices);
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var character = new SecondaryCharacter { Description = description };

        var trait = Randomizer.Instance.Next(Genre.SecondaryCharacterTraits);
        if (!string.IsNullOrWhiteSpace(trait))
        {
            character.Traits = [trait];
        }

        return character;
    }

    private void OnAddFeature(IEnumerable<string>? except)
    {
        EditingFeatureIndex = -1;

        if (!(Genre?.Features?.Count > 0))
        {
            return;
        }

        var choices = WritingPrompt.Features is not null
            ? [.. Genre
                .Features
                .Except(WritingPrompt
                    .Features
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Features;

        if (except is not null)
        {
            choices = [.. choices.Except(except)];
        }

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.Features ??= []).Add(value);
        }
    }

    private void OnAddFeature() => OnAddFeature(null);

    private void OnAddProtagonistTrait()
    {
        EditingProtagonistTraitIndex = -1;
        if (!(Genre?.ProtagonistTraits?.Count > 0))
        {
            return;
        }

        var choices = WritingPrompt.ProtagonistTraits is not null
            ? [.. Genre
                .ProtagonistTraits
                .Except(WritingPrompt
                    .ProtagonistTraits
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.ProtagonistTraits;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.ProtagonistTraits ??= []).Add(value);
        }
    }

    private void OnAddSecondaryCharacter()
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
        var character = GetRandomSecondaryCharacter(true);
        if (character is not null)
        {
            (WritingPrompt.SecondaryCharacters ??= []).Add(character);
        }
    }

    private void OnAddSecondaryCharacterTrait(int index)
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
        if (!(Genre?.SecondaryCharacterTraits?.Count > 0)
            || WritingPrompt.SecondaryCharacters is null
            || WritingPrompt.SecondaryCharacters.Count <= index)
        {
            return;
        }

        var choices = WritingPrompt.SecondaryCharacters[index].Traits is not null
            ? [.. Genre
                .SecondaryCharacterTraits
                .Except(WritingPrompt
                    .SecondaryCharacters[index]!
                    .Traits!
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.SecondaryCharacterTraits;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.SecondaryCharacters[index].Traits ??= []).Add(value);
        }
    }

    private void OnAddSetting()
    {
        EditingSettingIndex = -1;
        if (!(Genre?.Settings?.Count > 0))
        {
            return;
        }

        var choices = WritingPrompt.Settings is not null
            ? [.. Genre
                .Settings
                .Except(WritingPrompt
                    .Settings
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Settings;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.Settings ??= []).Add(value);
        }
    }

    private void OnAddSubject()
    {
        EditingSubjectIndex = -1;
        if (!(Genre?.Subjects?.Count > 0))
        {
            return;
        }

        var choices = WritingPrompt.Subjects is not null
            ? [.. Genre
                .Subjects
                .Except(WritingPrompt
                    .Subjects
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Subjects;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.Subjects ??= []).Add(value);
        }
    }

    private void OnAddTheme()
    {
        EditingThemeIndex = -1;
        if (!(Genre?.Themes?.Count > 0))
        {
            return;
        }

        var choices = WritingPrompt.Themes is not null
            ? [.. Genre
                .Themes
                .Except(WritingPrompt
                    .Themes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Themes;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            (WritingPrompt.Themes ??= []).Add(value);
        }
    }

    private void OnDeleteFeature(int index)
    {
        EditingFeatureIndex = -1;
        if (WritingPrompt?.Features?.Count > index)
        {
            WritingPrompt.Features.RemoveAt(index);
        }
    }

    private void OnDeleteProtagonistTrait(int index)
    {
        EditingProtagonistTraitIndex = -1;
        if (WritingPrompt?.ProtagonistTraits?.Count > index)
        {
            WritingPrompt.ProtagonistTraits.RemoveAt(index);
        }
    }

    private void OnDeleteSecondaryCharacter(int index)
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
        if (WritingPrompt?.SecondaryCharacters?.Count > index)
        {
            WritingPrompt.SecondaryCharacters.RemoveAt(index);
        }
    }

    private void OnDeleteSecondaryCharacterTrait(int characterIndex, int traitIndex)
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
        if (WritingPrompt?.SecondaryCharacters?.Count > characterIndex
            && WritingPrompt.SecondaryCharacters[characterIndex].Traits?.Count > traitIndex)
        {
            WritingPrompt.SecondaryCharacters[characterIndex].Traits!.RemoveAt(traitIndex);
        }
    }

    private void OnDeleteSetting(int index)
    {
        EditingSettingIndex = -1;
        if (WritingPrompt?.Settings?.Count > index)
        {
            WritingPrompt.Settings.RemoveAt(index);
        }
    }

    private void OnDeleteSubject(int index)
    {
        EditingSubjectIndex = -1;
        if (WritingPrompt?.Subjects?.Count > index)
        {
            WritingPrompt.Subjects.RemoveAt(index);
        }
    }

    private void OnDeleteTheme(int index)
    {
        EditingThemeIndex = -1;
        if (WritingPrompt?.Themes?.Count > index)
        {
            WritingPrompt.Themes.RemoveAt(index);
        }
    }

    private void OnEditPrompts() => DialogService.Show<WritingPromptEditor>("Writing Prompts");

    private void OnEditSecondaryCharacter(int index)
    {
        if (index == EditingSecondaryCharacterIndex)
        {
            return;
        }
        EditingSecondaryCharacterTraitIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterIndex = index;
    }

    private void OnEditSecondaryCharacterTrait(int index, int index2)
    {
        if (index == EditingSecondaryCharacterForTraitIndex
            && index2 == EditingSecondaryCharacterTraitIndex)
        {
            return;
        }
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = index;
        EditingSecondaryCharacterTraitIndex = index2;
    }

    private void OnEndSecondaryCharacterEdit()
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
    }

    private void OnLockGenre()
    {
        IsEditingGenre = false;
        IsGenreLocked = !IsGenreLocked;
        if (!IsGenreLocked)
        {
            IsSubgenreLocked = false;
        }
    }

    private void OnLockSubgenre()
    {
        IsEditingSubgenre = false;
        IsSubgenreLocked = !IsSubgenreLocked;
        if (IsSubgenreLocked)
        {
            IsEditingGenre = false;
            IsGenreLocked = true;
        }
    }

    private void OnRandomize()
    {
        if (IsGenreLocked)
        {
            OnUpdateGenre();
        }
        else
        {
            OnRandomizeGenre();
        }

        if (!IsPlotLocked)
        {
            OnRandomizePlot();
        }
    }

    private void OnRandomizeGenre(bool force = false)
    {
        IsEditingGenre = false;
        Genre? genre;
        do
        {
            genre = Randomizer.Instance.Next(DataService.Data.Genres);
        } while (force
            && genre is not null
            && DataService.Data.Genres?.Count > 1
            && genre.Name?.Equals(WritingPrompt.Genre) == true);
        Genre = genre;
        OnSelectGenre();
    }

    private void OnRandomizeFeatures(bool force = false)
    {
        EditingFeatureIndex = -1;
        if (Genre is null
            || !(Genre?.Features?.Count > 0))
        {
            return;
        }

        var except = force
            ? WritingPrompt.Features
            : null;

        WritingPrompt.Features?.Clear();
        OnAddFeature(except);
        if (WritingPrompt.Features?.Count > 0)
        {
            OnAddFeature(except);
        }
    }

    private void OnRandomizePlot(bool force = false)
    {
        Plot? value;
        do
        {
            value = Randomizer.Instance.Next(DataService.Data.Plots);
        } while (force
            && !string.IsNullOrWhiteSpace(value?.Name)
            && value.Equals(WritingPrompt.Plot));
        WritingPrompt.Plot = value;
    }

    private void OnRandomizeProtagonist(bool force = false)
    {
        IsEditingProtagonist = false;
        if (Genre is null
            || !(Genre?.Protagonists?.Count > 0))
        {
            return;
        }
        string? value;
        do
        {
            value = Randomizer.Instance.Next(Genre.Protagonists);
        } while (force
            && !string.IsNullOrWhiteSpace(value)
            && value.Equals(WritingPrompt.Protagonist));
        WritingPrompt.Protagonist = value;
    }

    private void OnRandomizeProtagonistTraits(bool force = false)
    {
        EditingProtagonistTraitIndex = -1;
        if (Genre is null
            || !(Genre?.ProtagonistTraits?.Count > 0))
        {
            return;
        }

        var choices = force && WritingPrompt.ProtagonistTraits is not null
            ? [.. Genre
                .ProtagonistTraits
                .Except(WritingPrompt
                    .ProtagonistTraits
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.ProtagonistTraits;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            WritingPrompt.ProtagonistTraits = [value];
        }
    }

    private void OnRandomizeSecondaryCharacters(bool force = false)
    {
        EditingSecondaryCharacterIndex = -1;
        EditingSecondaryCharacterForTraitIndex = -1;
        EditingSecondaryCharacterTraitIndex = -1;
        var character = GetRandomSecondaryCharacter(force);
        if (character is not null)
        {
            WritingPrompt.SecondaryCharacters = [character];
        }
    }

    private void OnRandomizeSubgenre(bool force = false)
    {
        IsEditingSubgenre = false;
        if (Genre is null
            || !(Genre?.Subgenres?.Count > 0))
        {
            return;
        }
        string? value;
        do
        {
            value = Randomizer.Instance.Next(Genre.Subgenres);
        } while (force
            && !string.IsNullOrWhiteSpace(value)
            && value.Equals(WritingPrompt.Subgenre));
        WritingPrompt.Subgenre = value;
    }

    private void OnRandomizeSettings(bool force = false)
    {
        EditingSettingIndex = -1;
        if (Genre is null
            || !(Genre?.Settings?.Count > 0))
        {
            return;
        }

        var choices = force && WritingPrompt.Settings is not null
            ? [.. Genre
                .Settings
                .Except(WritingPrompt
                    .Settings
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Settings;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            WritingPrompt.Settings = [value];
        }
    }

    private void OnRandomizeSubjects(bool force = false)
    {
        EditingSubjectIndex = -1;
        if (Genre is null
            || !(Genre?.Subjects?.Count > 0))
        {
            return;
        }

        var choices = force && WritingPrompt.Subjects is not null
            ? [.. Genre
                .Subjects
                .Except(WritingPrompt
                    .Subjects
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Subjects;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            WritingPrompt.Subjects = [value];
        }
    }

    private void OnRandomizeThemes(bool force = false)
    {
        EditingThemeIndex = -1;
        if (Genre is null
            || !(Genre?.Themes?.Count > 0))
        {
            return;
        }

        var choices = force && WritingPrompt.Themes is not null
            ? [.. Genre
                .Themes
                .Except(WritingPrompt
                    .Themes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .OfType<string>())]
            : Genre.Themes;

        if (choices.Count == 0)
        {
            return;
        }

        var value = Randomizer.Instance.Next(choices);
        if (!string.IsNullOrWhiteSpace(value))
        {
            WritingPrompt.Themes = [value];
        }
    }

    private void OnSelectGenre()
    {
        if (WritingPrompt.Genre?.Equals(Genre?.Name) == true)
        {
            return;
        }

        WritingPrompt.Genre = Genre?.Name;

        OnUpdateGenre();
    }

    private void OnSelectGenreName()
    {
        IsEditingGenre = false;

        if (WritingPrompt.Genre?.Equals(Genre?.Name) == true)
        {
            return;
        }

        Genre = DataService
            .Data
            .Genres?
            .Find(x
                => x.Name?.Equals(
                    WritingPrompt.Genre,
                    StringComparison.OrdinalIgnoreCase) == true);

        OnUpdateGenre();
    }

    private void OnUpdateGenre()
    {
        if (Genre is null)
        {
            return;
        }

        if (!IsSubgenreLocked)
        {
            OnRandomizeSubgenre();
        }

        if (!IsFeaturesLocked)
        {
            OnRandomizeFeatures();
        }

        if (!IsProtagonistLocked)
        {
            OnRandomizeProtagonist();
        }

        if (!IsProtagonistTraitsLocked)
        {
            OnRandomizeProtagonistTraits();
        }

        if (!IsSecondaryCharactersLocked)
        {
            OnRandomizeSecondaryCharacters();
        }

        if (!IsSettingsLocked)
        {
            OnRandomizeSettings();
        }

        if (!IsSubjectsLocked)
        {
            OnRandomizeSubjects();
        }

        if (!IsThemesLocked)
        {
            OnRandomizeThemes();
        }
    }
}
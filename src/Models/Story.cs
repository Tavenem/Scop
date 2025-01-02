using Scop.Interfaces;
using System.Text.Json.Serialization;

namespace Scop.Models;

public class Story : TraitContainer
{
    public List<TimelineCategory>? EventCategories { get; set; }

    [JsonIgnore] public string DisplayName => Name ?? "New Story";

    public List<TimelineEvent>? Events { get; set; }

    public string? Id { get; set; }

    [JsonIgnore] public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

    public string? Name { get; set; }

    public List<INote>? Notes { get; set; }

    public DateTimeOffset? Now { get; set; }

    public WritingPrompt? WritingPrompt { get; set; }

    public IEnumerable<Character> AllCharacters()
    {
        if (Notes is not null)
        {
            foreach (var note in Notes)
            {
                foreach (var child in note.AllCharacters())
                {
                    yield return child;
                }
            }
        }
    }

    public Character? FindCharacter(string id)
    {
        if (Notes is not null)
        {
            foreach (var child in Notes.OfType<Character>())
            {
                var found = child.FindCharacter(id);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    public void Initialize(ScopData data)
    {
        if (Notes is not null)
        {
            foreach (var note in Notes)
            {
                note.Initialize();
            }
        }
        ResetCharacterRelationshipMaps(data);
    }

    public void ResetCharacterRelationshipMaps(ScopData data)
        => Character.SetRelationshipMaps(data, [.. AllCharacters()]);
}

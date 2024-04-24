using Scop.Models;

namespace Scop;

public class Story : TraitContainer
{
    public List<TimelineCategory>? EventCategories { get; set; }

    public string DisplayName => Name ?? "New Story";

    public List<TimelineEvent>? Events { get; set; }

    public string? Id { get; set; }

    public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

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

    public void Initialize()
    {
        if (Notes is not null)
        {
            foreach (var note in Notes)
            {
                note.Initialize();
                note.LoadCharacters(this);
            }
        }
        ResetCharacterRelationshipMaps();
    }

    public void ResetCharacterRelationshipMaps()
        => Character.SetRelationshipMaps(AllCharacters().ToList());
}

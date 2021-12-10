namespace Scop;

public class Story
{
    public List<TimelineCategory>? EventCategories { get; set; }

    public string DisplayName => Name ?? "New Story";

    public List<TimelineEvent>? Events { get; set; }

    public string? Id { get; set; }

    public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

    public string? Name { get; set; }

    public List<INote>? Notes { get; set; }

    public DateTime? Now { get; set; }

    public IEnumerable<Character> AllCharacters()
    {
        if (Notes is not null)
        {
            foreach (var note in Notes.OfType<Character>())
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

    public void ResetCharacterRelationshipMaps()
    {
        if (Notes is null)
        {
            return;
        }
        foreach (var character in Notes.OfType<Character>())
        {
            character.ResetRelationshipMap(this);
        }
    }
}

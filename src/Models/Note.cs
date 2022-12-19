using System.Text.Json.Serialization;

namespace Scop;

public class Note : INote
{
    public string? Content { get; set; }

    [JsonIgnore] public string DisplayName => Name ?? Type;

    [JsonIgnore] public int IconIndex => 0;

    public bool IsExpanded { get; set; }

    [JsonIgnore] public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

    public string? Name { get; set; }

    [JsonIgnore] public string? NewNoteValue { get; set; }

    public List<INote>? Notes { get; set; }

    [JsonIgnore] public INote? Parent { get; set; }

    [JsonIgnore] public string Type => "Note";

    [JsonPropertyOrder(-1)] public virtual NoteTypeDiscriminator TypeDiscriminator => NoteTypeDiscriminator.Note;

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

    public void Initialize()
    {
        if (Notes is not null)
        {
            foreach (var child in Notes)
            {
                child.Parent = this;
                child.Initialize();
            }
        }
    }

    public void LoadCharacters(Story story)
    {
        if (Notes is not null)
        {
            foreach (var child in Notes)
            {
                child.LoadCharacters(story);
            }
        }
    }
}

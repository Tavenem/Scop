using System.Text.Json.Serialization;
using Tavenem.Blazor.Framework;

namespace Scop;

public class Note : INote
{
    public string? Content { get; set; }

    [JsonIgnore] public string DisplayName => Name ?? Type;

    [JsonIgnore] public int IconIndex => 0;

    [JsonIgnore] public bool IsUnnamed => string.IsNullOrWhiteSpace(Name);

    [JsonIgnore] public ElementList<INote>? List { get; set; }

    public string? Name { get; set; }

    public List<INote>? Notes { get; set; }

    [JsonIgnore] public INote? Parent { get; set; }

    [JsonIgnore] public string Type => "Note";

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

    public override string ToString() => DisplayName;
}

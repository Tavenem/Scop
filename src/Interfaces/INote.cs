using System.Text.Json.Serialization;
using Tavenem.Blazor.Framework;

namespace Scop;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeDiscriminator")]
[JsonDerivedType(typeof(Note), (int)NoteTypeDiscriminator.Note)]
[JsonDerivedType(typeof(Character), (int)NoteTypeDiscriminator.Character)]
public interface INote
{
    string? Content { get; set; }

    string DisplayName { get; }

    int IconIndex { get; }

    bool IsExpanded { get; set; }

    bool IsUnnamed { get; }

    ElementList<INote>? List { get; set; }

    string? Name { get; set; }

    string? NewNoteValue { get; set; }

    List<INote>? Notes { get; set; }

    INote? Parent { get; set; }

    string Type { get; }

    IEnumerable<Character> AllCharacters();

    void Initialize();

    void LoadCharacters(Story story);
}

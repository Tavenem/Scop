using System.Text.Json.Serialization;

namespace Scop;

[JsonDerivedType(typeof(Note), nameof(TypeDiscriminator))]
[JsonDerivedType(typeof(Character), nameof(TypeDiscriminator))]
public interface INote
{
    string? Content { get; set; }

    string DisplayName { get; }

    int IconIndex { get; }

    bool IsExpanded { get; set; }

    bool IsUnnamed { get; }

    string? Name { get; set; }

    string? NewNoteValue { get; set; }

    List<INote>? Notes { get; set; }

    INote? Parent { get; set; }

    string Type { get; }

    NoteTypeDiscriminator TypeDiscriminator { get; }

    IEnumerable<Character> AllCharacters();

    void Initialize();

    void LoadCharacters(Story story);
}

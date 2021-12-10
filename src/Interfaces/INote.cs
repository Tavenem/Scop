using System.Text.Json.Serialization;

namespace Scop;

[JsonConverter(typeof(NoteConverter))]
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

    string Type { get; }

    NoteTypeDiscriminator TypeDiscriminator { get; }
}

using System.Text.Json.Serialization;
using Tavenem.Blazor.Framework;

namespace Scop;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "TypeDiscriminator")]
[JsonDerivedType(typeof(Note), (int)NoteTypeDiscriminator.Note)]
[JsonDerivedType(typeof(Character), (int)NoteTypeDiscriminator.Character)]
public interface INote
{
    string? Content { get; set; }

    [JsonIgnore] string DisplayName { get; }

    [JsonIgnore] int IconIndex { get; }

    [JsonIgnore] bool IsUnnamed { get; }

    [JsonIgnore] ElementList<INote>? List { get; set; }

    string? Name { get; set; }

    List<INote>? Notes { get; set; }

    [JsonIgnore] INote? Parent { get; set; }

    [JsonIgnore] string Type { get; }

    IEnumerable<Character> AllCharacters();

    void Initialize();

    void LoadCharacters(Story story);
}

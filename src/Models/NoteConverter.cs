using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scop;

public class NoteConverter : JsonConverter<INote>
{
    public override bool CanConvert(Type typeToConvert)
        => typeof(INote).IsAssignableFrom(typeToConvert);

    public override INote? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var clone = reader;

        if (clone.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        clone.Read();
        if (clone.TokenType != JsonTokenType.PropertyName)
        {
            throw new JsonException();
        }

        var propertyName = clone.GetString();
        if (propertyName != (options.PropertyNamingPolicy?.ConvertName(nameof(Note.TypeDiscriminator)) ?? nameof(Note.TypeDiscriminator)))
        {
            throw new JsonException();
        }

        clone.Read();
        if (clone.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        var discriminator = (NoteTypeDiscriminator)clone.GetInt32();
        return discriminator switch
        {
            NoteTypeDiscriminator.Character => JsonSerializer.Deserialize<Character>(ref reader),
            _ => JsonSerializer.Deserialize<Note>(ref reader),
        };
    }

    public override void Write(Utf8JsonWriter writer, INote value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

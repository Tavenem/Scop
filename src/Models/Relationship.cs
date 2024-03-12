using System.Text;
using System.Text.Json.Serialization;

namespace Scop;

public class Relationship : IJsonOnDeserialized
{
    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(RelativeName ?? Relative?.DisplayName);
            if (sb.Length > 0)
            {
                sb.Append(": ");
            }
            sb.Append(RelationshipName ?? Type);
            return sb.Length > 0
                ? sb.ToString()
                : "unknown";
        }
    }

    [JsonIgnore] public string? EditedInverseType { get; set; }

    [JsonIgnore] public string? EditedRelationshipName { get; set; }

    [JsonIgnore] public string? EditedRelativeName { get; set; }

    [JsonIgnore] public string? EditedType { get; set; }

    public string? Id { get; set; }

    public string? InverseType { get; set; }

    public string? RelationshipName { get; set; }

    [JsonIgnore] public Character? Relative { get; set; }

    public string? RelativeName { get; set; }

    [JsonIgnore] public bool Synthetic { get; set; }

    public string? Type { get; set; }

    public void OnDeserialized()
    {
        EditedInverseType = InverseType;
        EditedRelationshipName = RelationshipName;
        EditedRelativeName = RelativeName;
        EditedType = Type;
    }
}

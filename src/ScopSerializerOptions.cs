using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Tavenem.DataStorage;

namespace Scop;

public static class ScopSerializerOptions
{
    public static JsonSerializerOptions Instance { get; }

    static ScopSerializerOptions()
    {
        Instance = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Instance.TypeInfoResolverChain.Add(ScopSerializerContext.Default.WithAddedModifier(static typeInfo =>
        {
            if (typeInfo.Type == typeof(IIdItem))
            {
                typeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
                {
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                };
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(typeof(LocalData), nameof(LocalData)));
            }
        }));
    }
}

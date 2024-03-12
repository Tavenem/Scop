using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Scop;

[JsonSerializable(typeof(IIdItem))]
[JsonSerializable(typeof(LocalData))]
public partial class ScopSerializerContext : JsonSerializerContext;

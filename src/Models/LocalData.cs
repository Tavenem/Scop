using Tavenem.DataStorage;

namespace Scop.Models;

public class LocalData : IIdItem
{
    internal const string IdValue = "data";

    public string Id { get; set; } = IdValue;

    public ScopData? Data { get; set; }

    public bool Equals(IIdItem? other) => other is LocalData
        && Id?.Equals(Id) == true;
}

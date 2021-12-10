namespace Scop;

public static class Extensions
{
    private static readonly Random _random = new();

    public static string GetDescription(this Pronouns pronouns) => pronouns switch
    {
        Pronouns.SheHer => "She/Her",
        Pronouns.HeHim => "He/Him",
        Pronouns.TheyThem => "They/Them",
        Pronouns.It => "It",
        _ => "Other",
    };

    public static T? GetRandom<T>(this List<T>? items) where T : IWeighted
    {
        if (items is null)
        {
            return default;
        }

        var total = items.Sum(x => x.EffectiveWeight);
        var selection = _random.NextDouble() * total;
        var i = -1;
        do
        {
            i++;
            selection -= items[i].EffectiveWeight;
        } while (selection > 0 && i < items.Count);
        return i < items.Count
            ? items[i]
            : items.LastOrDefault();
    }

    public static bool StartsWith<T>(this IEnumerable<T> candidate, IEnumerable<T> target) where T : IEquatable<T>
    {
        var candidateEnumerator = candidate.GetEnumerator();
        var targetEnumerator = target.GetEnumerator();

        while (candidateEnumerator.MoveNext()
            && targetEnumerator.MoveNext())
        {
            if (!candidateEnumerator.Current.Equals(targetEnumerator.Current))
            {
                return false;
            }
        }
        return !targetEnumerator.MoveNext();
    }

    public static string ToTitle(this string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Trim());
}

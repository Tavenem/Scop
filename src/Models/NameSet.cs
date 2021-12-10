namespace Scop;

public class NameSet
{
    public List<NameData>? FemaleNames { get; set; }
    public List<NameData>? MaleNames { get; set; }
    public List<NameData>? Surnames { get; set; }

    public List<NameData> GetNamesForGender(NameGender gender)
    {
        var names = new List<NameData>();
        if (FemaleNames is not null && gender.HasFlag(NameGender.Female))
        {
            names.AddRange(FemaleNames);
        }
        if (MaleNames is not null && gender.HasFlag(NameGender.Male))
        {
            names.AddRange(MaleNames);
        }
        return names;
    }

    public string? GetRandomName(NameGender gender)
        => GetNamesForGender(gender).GetRandom()?.Name;

    public string? GetRandomSurname() => Surnames.GetRandom()?.Name;
}

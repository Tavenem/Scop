namespace Scop;

[Flags]
public enum NameGender
{
    None = 0,
    Female = 1 << 0,
    Male = 1 << 1,
    Both = Female | Male,
}

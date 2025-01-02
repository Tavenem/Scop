namespace Scop.Models;

/// <summary>
/// A surname.
/// </summary>
/// <param name="Name">The name</param>
/// <param name="IsMatronymic">Whether the name is inherited from the maternal line.</param>
/// <param name="IsSpousal">Whether the name is adopted from a spouse.</param>
public readonly record struct Surname(string? Name, bool IsMatronymic = false, bool IsSpousal = false) : ISpanFormattable
{
    /// <summary>
    /// Returns the <see cref="Name"/> property.
    /// </summary>
    /// <returns>The <see cref="Name"/> property.</returns>
    public override string? ToString() => Name;

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => Name ?? string.Empty;

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (Name is null)
        {
            charsWritten = 0;
            return true;
        }

        if (destination.Length < Name.Length)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = Name.Length;
        Name.AsSpan().CopyTo(destination);
        return true;
    }
}

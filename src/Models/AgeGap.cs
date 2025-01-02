namespace Scop.Models;

/// <summary>
/// <para>
/// The typical age difference between two associated characters, as a range of minimum and maximum
/// differences, along with the mean difference.
/// </para>
/// <para>
/// Any value may be negative, to indicate that the relative is younger.
/// </para>
/// <para>
/// Any value may be <see langword="null"/>, to indicate that there is no typical value.
/// </para>
/// </summary>
public readonly record struct AgeGap(int? Min, int? Mean, int? Max);

using Scop.Enums;
using System.Text.Json.Serialization;

namespace Scop.Models;

public class TraitModifier
{
    [JsonIgnore] public double EffectiveWeight => Weight ?? 1;

    /// <summary>
    /// A collection of semicolon-delimited strings indicating the full paths of
    /// the ethnicities referenced by this modifier. Any referenced ethnicity may be
    /// active for this modifier to take effect. If <see langword="null"/> or
    /// empty, this modifier is always active.
    /// </summary>
    /// <remarks>
    /// The presence of any child ethnicities below the indicated path is irrelevant.
    /// </remarks>
    /// <example>
    /// The string "root;child;leaf" indicates that this modifier will take
    /// effect when the ethnicity with the name "leaf", which is the child of
    /// "child", which is the child of "root", which is a top-level option, is
    /// selected.
    /// </example>
    public List<string>? Ethnicities { get; set; }

    /// <summary>
    /// If true, causes the trait to be automatically selected when
    /// this modifier is active.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// <para>
    /// Indicates the gender affected by this modifier.
    /// </para>
    /// <para>
    /// When <see cref="NameGender.None"/> any gender is affected.
    /// </para>
    /// </summary>
    public NameGender Gender { get; set; }

    /// <summary>
    /// Indicates the maximum age at which this modifier takes effect.
    /// </summary>
    public int? MaxAge { get; set; }

    /// <summary>
    /// Indicates the minimum age at which this modifier takes effect.
    /// </summary>
    public int MinAge { get; set; }

    /// <summary>
    /// The relative priority of this modifier among all those affecting a trait.
    /// Highest wins.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// A collection of semicolon-delimited strings indicating the full paths of
    /// the traits referenced by this modifier. Any referenced option may be
    /// active for this modifier to take effect. If <see langword="null"/> or
    /// empty, this modifier is always active.
    /// </summary>
    /// <example>
    /// The string "root;child;leaf" indicates that this modifier will take
    /// effect when the trait with the name "leaf", which is the child of
    /// "child", which is the child of "root", which is a top-level option, is
    /// selected.
    /// </example>
    public List<string>? TargetPaths { get; set; }

    public double? Weight { get; set; }

    public bool Applies(TraitContainer container)
    {
        if (Gender != NameGender.None)
        {
            if (container is Character character)
            {
                var gender = character.GetNameGender();
                if ((gender & Gender) == NameGender.None)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (MinAge > 0 || MaxAge.HasValue)
        {
            if (container is Character character)
            {
                var age = character.DisplayAgeYears;
                if (!age.HasValue)
                {
                    return false;
                }
                if (MaxAge.HasValue)
                {
                    if (MinAge <= MaxAge)
                    {
                        if (age < MinAge
                            || age > MaxAge)
                        {
                            return false;
                        }
                    }
                    else if (age < MinAge && age > MaxAge)
                    {
                        return false;
                    }
                }
                else if (age < MinAge)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (Ethnicities?.Count > 0)
        {
            if (container is Character character)
            {
                if (character.EthnicityPaths is null)
                {
                    return false;
                }

                foreach (var ethnicity in Ethnicities)
                {
                    var path = ethnicity.Split(';');
                    if (!character.EthnicityPaths.Any(x => x.StartsWith(path)))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
        }

        if (TargetPaths?.Count > 0)
        {
            if (container.TraitPaths is null)
            {
                return false;
            }

            foreach (var traitPath in TargetPaths)
            {
                var path = traitPath.Split(';');
                if (!container.TraitPaths.Any(x => x.StartsWith(path)))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

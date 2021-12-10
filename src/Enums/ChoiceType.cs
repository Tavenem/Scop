namespace Scop;

/// <summary>
/// Indicates the action taken when an option is selected, and how many children may be selected at once.
/// </summary>
public enum ChoiceType
{
    /// <summary>
    /// Indicates that all children with non-zero weights are selected on selection, and any
    /// number may be selected at once (by hand). This is the default option.
    /// </summary>
    Category = 0,

    /// <summary>
    /// Indicates that nothing happens on selection, but any number may be selected at once (by hand).
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Indicates that one child option is selected at random, and at most one may be selected at once.
    /// </summary>
    Single = 2,

    /// <summary>
    /// Indicates that one child option is selected at random, but any number may be
    /// selected at once (by hand).
    /// </summary>
    OneOrMore = 3,

    /// <summary>
    /// Indicates that each child option has an independent chance to be selected, and any number
    /// may be selected at once.
    /// </summary>
    Multiple = 4,
}

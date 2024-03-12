namespace Scop.Models;

public class WritingPrompt
{
    public List<string>? Features { get; set; }

    public string? Genre { get; set; }

    public Plot? Plot { get; set; }

    public string? Protagonist { get; set; }

    public List<string>? ProtagonistTraits { get; set; }

    public List<SecondaryCharacter>? SecondaryCharacters { get; set; }

    public List<string>? Settings { get; set; }

    public string? Subgenre { get; set; }

    public List<string>? Subjects { get; set; }

    public List<string>? Themes { get; set; }
}

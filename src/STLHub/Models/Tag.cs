namespace STLHub.Models;

/// <summary>
/// Represents a user-defined tag that can be associated with 3D objects.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The tag's name; also what assistive technology announces for its list item.</summary>
    public override string ToString() => Name;
}

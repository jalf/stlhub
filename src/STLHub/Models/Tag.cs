namespace STLHub.Models;

/// <summary>
/// Represents a user-defined tag that can be associated with 3D objects.
/// </summary>
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True when the tag was just added to the selected object, so only its chip animates in;
    /// not stored in the database.
    /// </summary>
    public bool IsJustAdded { get; set; }

    /// <summary>The tag's name; also what assistive technology announces for its list item.</summary>
    public override string ToString() => Name;
}

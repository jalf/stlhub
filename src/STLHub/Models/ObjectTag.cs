namespace STLHub.Models;

/// <summary>
/// Join entity representing the many-to-many relationship between objects and tags.
/// </summary>
public class ObjectTag
{
    public int ObjectId { get; set; }
    public int TagId { get; set; }
}

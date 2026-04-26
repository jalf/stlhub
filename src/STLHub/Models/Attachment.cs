namespace STLHub.Models;

/// <summary>
/// Represents a file attachment (image, document, etc.) linked to a 3D object.
/// </summary>
public class Attachment
{
    public int Id { get; set; }
    public int ObjectId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

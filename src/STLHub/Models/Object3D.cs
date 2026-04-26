using System;

namespace STLHub.Models;

/// <summary>
/// Represents a 3D object stored in the library (STL, 3MF, OBJ).
/// </summary>
public class Object3D
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MainFilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

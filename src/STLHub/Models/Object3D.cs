
using System;

namespace STLHub.Models;

/// <summary>
/// Represents a 3D object in the STLHub application, including metadata and file paths.
/// </summary>
public class Object3D
{
    // Optional category name for display purposes (not stored in DB)
    public string? CategoryName { get; set; }
    public string RelativeFilePath { get; set; } = string.Empty;

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

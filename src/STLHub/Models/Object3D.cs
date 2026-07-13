using System;

namespace STLHub.Models;

/// <summary>
/// Represents a 3D object in the STLHub application, including metadata and file paths.
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

    // Derived at read time, not persisted.

    /// <summary>Repo-relative form of <see cref="MainFilePath"/>, populated on read.</summary>
    public string RelativeFilePath { get; set; } = string.Empty;

    /// <summary>Category name resolved for display; not stored in the database.</summary>
    public string? CategoryName { get; set; }
}

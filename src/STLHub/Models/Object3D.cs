using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STLHub.Models;

/// <summary>
/// Represents a 3D object in the STLHub application, including metadata and file paths.
/// <see cref="Name"/> and <see cref="Description"/> raise change notifications so an edit
/// shows on the object's card without reloading the list.
/// </summary>
public class Object3D : ObservableObject
{
    private string _name = string.Empty;
    private string _description = string.Empty;

    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

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

    /// <summary>The object's name; also what assistive technology announces for its list item.</summary>
    public override string ToString() => Name;
}

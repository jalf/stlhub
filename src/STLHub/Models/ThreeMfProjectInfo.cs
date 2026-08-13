namespace STLHub.Models;

/// <summary>
/// Transient project metadata extracted from a <c>.3mf</c> archive.
/// Read on demand when an object is selected and never persisted to the database.
/// A <c>null</c> field means the value was absent in the file; empty strings are
/// normalized to <c>null</c> during extraction.
/// </summary>
public sealed record ThreeMfProjectInfo
{
    /// <summary>Model name from the standard <c>Title</c> metadata entry.</summary>
    public string? Title { get; init; }

    /// <summary>Author/creator from the standard <c>Designer</c> metadata entry.</summary>
    public string? Designer { get; init; }

    /// <summary>Project description from the standard <c>Description</c> metadata entry.</summary>
    public string? Description { get; init; }

    /// <summary>Raw creation date string from the standard <c>CreationDate</c> metadata entry, displayed as-is.</summary>
    public string? CreationDate { get; init; }

    /// <summary>Print profile name, from the slicer-specific configuration file.</summary>
    public string? ProfileName { get; init; }

    /// <summary>Layer height as reported by the slicer.</summary>
    public string? LayerHeight { get; init; }

    /// <summary>Wall (perimeter) count as reported by the slicer.</summary>
    public string? WallCount { get; init; }

    /// <summary>Infill density as reported by the slicer.</summary>
    public string? InfillPercent { get; init; }

    /// <summary>Profile creator name; absent in most slicers.</summary>
    public string? ProfileAuthor { get; init; }

    /// <summary>True when at least one field was extracted; drives visibility of the whole section.</summary>
    public bool HasAnyMetadata =>
        Title is not null || Designer is not null || Description is not null || CreationDate is not null ||
        HasAnyProfileData;

    /// <summary>True when at least one print profile field was extracted; drives visibility of the profile sub-section.</summary>
    public bool HasAnyProfileData =>
        ProfileName is not null || LayerHeight is not null || WallCount is not null ||
        InfillPercent is not null || ProfileAuthor is not null;
}

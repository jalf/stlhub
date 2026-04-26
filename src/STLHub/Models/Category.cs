namespace STLHub.Models;

/// <summary>
/// Represents a folder/category for organizing 3D objects in a tree hierarchy.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

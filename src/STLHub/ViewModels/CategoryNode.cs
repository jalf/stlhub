using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using STLHub.Models;

namespace STLHub.ViewModels;

/// <summary>
/// View model node representing a category in the sidebar tree.
/// Wraps a <see cref="Category"/> model and exposes editable properties.
/// </summary>
public partial class CategoryNode : ObservableObject
{
    [ObservableProperty]
    private Category _category;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<CategoryNode> Children { get; } = new();

    public CategoryNode(Category category)
    {
        _category = category;
        _name = category.Name;
    }

    partial void OnNameChanged(string value)
    {
        _category.Name = value;
    }
}

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

    private string _nameBeforeEdit = string.Empty;

    /// <summary>Enters inline rename mode, remembering the current name so it can be restored.</summary>
    public void BeginEdit()
    {
        _nameBeforeEdit = Name;
        IsEditing = true;
    }

    /// <summary>Leaves rename mode and restores the name the category had before editing.</summary>
    public void CancelEdit()
    {
        Name = _nameBeforeEdit;
        IsEditing = false;
    }

    /// <summary>
    /// Leaves rename mode, trimming the new name. A blank name is rejected and the previous one
    /// restored. Returns true when the name actually changed and needs saving.
    /// </summary>
    public bool EndEdit()
    {
        IsEditing = false;
        var trimmed = Name.Trim();
        if (trimmed.Length == 0)
        {
            Name = _nameBeforeEdit;
            return false;
        }
        Name = trimmed;
        return trimmed != _nameBeforeEdit;
    }

    /// <summary>The category's name; also what assistive technology announces for its tree item.</summary>
    public override string ToString() => Name;
}

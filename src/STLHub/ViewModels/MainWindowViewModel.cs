using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STLHub.Data;
using STLHub.Models;
using STLHub.Services;

namespace STLHub.ViewModels;

/// <summary>
/// Supported grid view sizes for the object card display.
/// </summary>
public enum ViewSize { Small, Medium, Large }

/// <summary>
/// Sort order options for the object list.
/// </summary>
public enum SortOrder { DateDesc, DateAsc, NameAsc, NameDesc }

/// <summary>
/// Represents a labeled sort option for the UI ComboBox.
/// </summary>
public record SortOption(string Label, SortOrder Value);

/// <summary>
/// Main application view model. Manages the object library, categories, search,
/// sorting, theme, and import operations.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private ObjectRepository? _repository;
    private LibraryManager? _libraryManager;

    /// <summary>Callback set by the View to display warning dialogs.</summary>
    public Func<string, string, Task>? ShowWarningAsync { get; set; }

    /// <summary>Callback set by the View to display confirmation dialogs.</summary>
    public Func<string, string, Task<bool>>? ShowConfirmAsync { get; set; }

    /// <summary>Callback set by the View to open a folder picker dialog.</summary>
    public Func<Task<string?>>? PickFolderAsync { get; set; }

    /// <summary>Callback invoked when the repository changes, to persist settings.</summary>
    public Action<string>? OnRepositoryChanged { get; set; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _currentRepositoryName = string.Empty;

    [ObservableProperty]
    private Object3D? _selectedObject;

    [ObservableProperty]
    private ViewSize _currentViewSize = ViewSize.Medium;

    public List<SortOption> SortOptions { get; } =
    [
        new("Data (mais recente)", SortOrder.DateDesc),
        new("Data (mais antigo)", SortOrder.DateAsc),
        new("Nome (A → Z)", SortOrder.NameAsc),
        new("Nome (Z → A)", SortOrder.NameDesc),
    ];

    [ObservableProperty]
    private SortOption _selectedSortOption = null!;

    [ObservableProperty]
    private SortOrder _currentSortOrder = SortOrder.DateDesc;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private string _statusText = "Pronto";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Callback to apply the theme at the application level.</summary>
    public Action<bool>? ApplyTheme { get; set; }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ApplyTheme?.Invoke(value);
    }

    partial void OnCurrentSortOrderChanged(SortOrder value)
    {
        var match = SortOptions.FirstOrDefault(o => o.Value == value);
        if (match is not null && match != SelectedSortOption)
            SelectedSortOption = match;
        LoadItems(SearchText);
    }

    partial void OnCurrentViewSizeChanged(ViewSize value)
    {
        OnPropertyChanged(nameof(CardWidth));
        OnPropertyChanged(nameof(CardHeight));
        OnPropertyChanged(nameof(ThumbnailHeight));
        OnPropertyChanged(nameof(CardFontSize));
        OnPropertyChanged(nameof(SmallViewOpacity));
        OnPropertyChanged(nameof(MediumViewOpacity));
        OnPropertyChanged(nameof(LargeViewOpacity));
    }

    public double SmallViewOpacity => CurrentViewSize == ViewSize.Small ? 1.0 : 0.35;
    public double MediumViewOpacity => CurrentViewSize == ViewSize.Medium ? 1.0 : 0.35;
    public double LargeViewOpacity => CurrentViewSize == ViewSize.Large ? 1.0 : 0.35;

    public int CardWidth => CurrentViewSize switch
    {
        ViewSize.Small => 100,
        ViewSize.Large => 220,
        _ => 150
    };

    public int CardHeight => CurrentViewSize switch
    {
        ViewSize.Small => 115,
        ViewSize.Large => 240,
        _ => 165
    };

    public int ThumbnailHeight => CurrentViewSize switch
    {
        ViewSize.Small => 75,
        ViewSize.Large => 170,
        _ => 110
    };

    public int CardFontSize => CurrentViewSize switch
    {
        ViewSize.Small => 10,
        ViewSize.Large => 14,
        _ => 12
    };

    [RelayCommand]
    private void SetViewSize(string size)
    {
        CurrentViewSize = size switch
        {
            "Small" => ViewSize.Small,
            "Large" => ViewSize.Large,
            _ => ViewSize.Medium
        };
    }

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        if (value is not null)
            CurrentSortOrder = value.Value;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    public ObservableCollection<Object3D> Items { get; } = new();
    public ObservableCollection<Attachment> Attachments { get; } = new();
    public ObservableCollection<Tag> ObjectTags { get; } = new();
    public ObservableCollection<CategoryNode> Categories { get; } = new();

    [ObservableProperty]
    private string _newTagName = string.Empty;

    [ObservableProperty]
    private CategoryNode? _selectedCategory;

    public MainWindowViewModel(ObjectRepository repository, LibraryManager libraryManager)
    {
        _repository = repository;
        _libraryManager = libraryManager;
        _selectedSortOption = SortOptions[0];
        LoadCategories();
        LoadItems();
    }

    public MainWindowViewModel()
    {
        // Design time constructor
        _selectedSortOption = SortOptions[0];
    }

    public async Task SwitchRepository(string repoPath)
    {
        IsBusy = true;
        StatusText = "Abrindo repositório...";

        try
        {
            Directory.CreateDirectory(repoPath);

            string dbPath = Path.Combine(repoPath, "stlhub.db");
            string libraryPath = Path.Combine(repoPath, "Library");

            await Task.Run(() =>
            {
                var dbInitializer = new DatabaseInitializer(dbPath);
                dbInitializer.Initialize();
            });

            _repository = new ObjectRepository(dbPath);
            _libraryManager = new LibraryManager(libraryPath, _repository);

            CurrentRepositoryName = repoPath;
            SelectedObject = null;
            SelectedCategory = null;
            SearchText = string.Empty;

            LoadCategories();
            LoadItems();

            OnRepositoryChanged?.Invoke(repoPath);
            StatusText = $"Repositório aberto — {Items.Count} objeto(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao abrir repositório: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenRepository()
    {
        if (PickFolderAsync == null) return;
        var folder = await PickFolderAsync();
        if (!string.IsNullOrWhiteSpace(folder))
        {
            await SwitchRepository(folder);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        LoadItems(value);
    }

    partial void OnSelectedObjectChanged(Object3D? value)
    {
        LoadAttachments();
        LoadTags();
    }

    partial void OnSelectedCategoryChanged(CategoryNode? value)
    {
        OnPropertyChanged(nameof(IsAllObjectsSelected));
        LoadItems(SearchText);
    }

    public bool IsAllObjectsSelected => SelectedCategory == null;

    public void LoadCategories()
    {
        Categories.Clear();
        if (_repository == null) return;

        var allCats = _repository.GetAllCategories().ToList();
        var nodeLookup = new Dictionary<int, CategoryNode>();
        
        foreach (var cat in allCats)
        {
            nodeLookup[cat.Id] = new CategoryNode(cat);
        }

        foreach (var node in nodeLookup.Values)
        {
            if (node.Category.ParentCategoryId.HasValue && nodeLookup.TryGetValue(node.Category.ParentCategoryId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                Categories.Add(node);
            }
        }
    }

    public void LoadItems(string searchTerm = "")
    {
        if (_repository == null) return;
        
        Items.Clear();
        var results = _repository.SearchObjects(searchTerm, SelectedCategory?.Category.Id);
        var sorted = CurrentSortOrder switch
        {
            SortOrder.DateAsc => results.OrderBy(o => o.CreatedAt),
            SortOrder.NameAsc => results.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase),
            SortOrder.NameDesc => results.OrderByDescending(o => o.Name, StringComparer.OrdinalIgnoreCase),
            _ => results.OrderByDescending(o => o.CreatedAt)
        };
        foreach (var item in sorted)
        {
            Items.Add(item);
        }
    }

    public void LoadAttachments()
    {
        Attachments.Clear();
        if (_repository == null || SelectedObject == null) return;

        var results = _repository.GetAttachments(SelectedObject.Id);
        foreach (var att in results)
        {
            Attachments.Add(att);
        }
    }

    public void LoadTags()
    {
        ObjectTags.Clear();
        if (_repository == null || SelectedObject == null) return;

        var results = _repository.GetTagsForObject(SelectedObject.Id);
        foreach (var tag in results)
        {
            ObjectTags.Add(tag);
        }
    }

    public async Task ImportFiles(string[] filePaths)
    {
        if (_libraryManager == null) return;

        IsBusy = true;
        int total = filePaths.Length;
        int imported = 0;

        var categoryId = SelectedCategory?.Category.Id;

        try
        {
            foreach (var path in filePaths)
            {
                imported++;
                StatusText = $"Importando {imported}/{total}: {Path.GetFileName(path)}";
                await Task.Run(() => _libraryManager.ImportFile(path, categoryId));
            }
            LoadItems(SearchText);
            StatusText = $"{imported} objeto(s) importado(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro na importação: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public (int objectsImported, int attachmentsImported) RunImportFolder(
        string folderPath,
        Action<string>? onProgress = null,
        Action<int, int>? onCounts = null,
        CancellationToken cancellationToken = default)
    {
        if (_libraryManager == null) return (0, 0);
        var parentCategoryId = SelectedCategory?.Category.Id;
        return _libraryManager.ImportFolder(folderPath, parentCategoryId,
            onProgress, onCounts, cancellationToken);
    }

    public async Task ImportAttachments(string[] filePaths)
    {
        if (_libraryManager == null || SelectedObject == null) return;

        IsBusy = true;
        int total = filePaths.Length;
        int imported = 0;

        try
        {
            foreach (var path in filePaths)
            {
                imported++;
                StatusText = $"Anexando {imported}/{total}: {Path.GetFileName(path)}";
                await Task.Run(() => _libraryManager.ImportAttachment(SelectedObject.Id, path));
            }
            LoadAttachments();
            StatusText = $"{imported} arquivo(s) anexado(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao anexar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SaveMetadata()
    {
        if (SelectedObject != null && _repository != null)
        {
            _repository.UpdateObject(SelectedObject);
            // Optionally, we could load items here, but to keep selection active we rely on TwoWay binding
        }
    }

    [RelayCommand]
    private void RemoveAttachment(Attachment? attachment)
    {
        if (attachment != null && _libraryManager != null)
        {
            _libraryManager.DeleteAttachment(attachment);
            Attachments.Remove(attachment);
        }
    }

    [RelayCommand]
    private void OpenFileInSystem(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTagName) || SelectedObject == null || _repository == null) return;
        
        var tag = _repository.AddOrGetTag(NewTagName);
        _repository.AddTagToObject(SelectedObject.Id, tag.Id);
        
        NewTagName = string.Empty;
        LoadTags();
    }

    [RelayCommand]
    private void RemoveTag(Tag? tag)
    {
        if (tag != null && SelectedObject != null && _repository != null)
        {
            _repository.RemoveTagFromObject(SelectedObject.Id, tag.Id);
            LoadTags();
        }
    }

    [RelayCommand]
    private void AddRootCategory()
    {
        var cat = new Category { Name = "Nova Categoria Raiz" };
        _repository?.AddCategory(cat);
        LoadCategories();
    }

    [RelayCommand]
    private void AddSubCategory(CategoryNode? parent)
    {
        if (parent == null) return;
        var cat = new Category { Name = "Nova Subcategoria", ParentCategoryId = parent.Category.Id };
        _repository?.AddCategory(cat);
        LoadCategories();
    }

    [RelayCommand]
    private async Task DeleteCategory(CategoryNode? node)
    {
        if (node == null || _repository == null) return;

        // Conta objetos na própria pasta e recursivamente nas subpastas
        int count = CountObjectsRecursive(node);
        if (count > 0)
        {
            if (ShowWarningAsync != null)
            {
                await ShowWarningAsync(
                    "Pasta com objetos",
                    $"A pasta \"{node.Name}\" contém {count} objeto(s) 3D.\n\nMova ou remova os objetos antes de excluir a pasta.");
            }
            return;
        }

        _repository.DeleteCategory(node.Category.Id);
        LoadCategories();
        LoadItems(SearchText);
    }

    private int CountObjectsRecursive(CategoryNode node)
    {
        if (_repository == null) return 0;
        int count = _repository.CountObjectsInCategory(node.Category.Id);
        foreach (var child in node.Children)
            count += CountObjectsRecursive(child);
        return count;
    }

    [RelayCommand]
    private void RenameCategory(CategoryNode? node)
    {
        if (node == null || _repository == null) return;
        node.IsEditing = true;
    }

    public void CommitRenameCategory(CategoryNode node)
    {
        if (_repository == null) return;
        node.IsEditing = false;
        node.Category.Name = node.Name;
        _repository.UpdateCategory(node.Category);
    }

    public void MoveObjectToCategory(Object3D obj, CategoryNode? node)
    {
        if (_repository != null)
        {
            _repository.UpdateObjectCategory(obj.Id, node?.Category.Id);
            LoadItems(SearchText);
        }
    }

    [RelayCommand]
    private async Task DeleteObject(Object3D? obj)
    {
        if (obj == null || _libraryManager == null) return;

        bool confirmed = false;
        if (ShowConfirmAsync != null)
        {
            confirmed = await ShowConfirmAsync(
                "Excluir Objeto",
                $"Tem certeza que deseja excluir \"{obj.Name}\"?\n\nEssa ação não pode ser desfeita.");
        }
        if (!confirmed) return;

        IsBusy = true;
        StatusText = $"Excluindo \"{obj.Name}\"...";

        try
        {
            if (SelectedObject?.Id == obj.Id)
                SelectedObject = null;

            await Task.Run(() => _libraryManager.DeleteObject(obj));
            LoadItems(SearchText);
            StatusText = "Objeto excluído";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao excluir: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegenerateThumbnail(Object3D? obj)
    {
        if (obj == null || _libraryManager == null) return;

        IsBusy = true;
        StatusText = $"Regenerando thumbnail de \"{obj.Name}\"...";

        try
        {
            string newPath = await Task.Run(() => _libraryManager.RegenerateThumbnail(obj));
            obj.ThumbnailPath = newPath;

            LoadItems(SearchText);

            if (SelectedObject?.Id == obj.Id)
            {
                SelectedObject = null;
                SelectedObject = obj;
            }
            StatusText = "Thumbnail regenerado";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao regenerar thumbnail: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

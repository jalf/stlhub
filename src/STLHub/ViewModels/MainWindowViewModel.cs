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
using STLHub.Converters;
using STLHub.Data;
using STLHub.Models;
using STLHub.Services;

namespace STLHub.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Clears the search text box.
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }
    // ...existing code...

    /// <summary>
    /// Retorna o nome da categoria para um objeto dado seu CategoryId.
    /// </summary>
    public string? GetCategoryName(int? categoryId)
    {
        if (!categoryId.HasValue) return null;
        var cat = Categories.Select(n => FindCategoryById(n, categoryId.Value)).FirstOrDefault(c => c != null);
        return cat?.Name;
    }

    private CategoryNode? FindCategoryById(CategoryNode node, int id)
    {
        if (node.Category.Id == id) return node;
        foreach (var child in node.Children)
        {
            var found = FindCategoryById(child, id);
            if (found != null) return found;
        }
        return null;
    }

    // ...existing code...
}
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
/// Represents a labeled theme option for the UI ComboBox.
/// </summary>
public record ThemeOption(string Label, string Key);

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

    public List<ThemeOption> ThemeOptions { get; } =
    [
        new("Escuro", "Dark"),
        new("Claro", "Light"),
        new("Nord", "Nord"),
        new("Dracula", "Dracula"),
        new("Solarizado Escuro", "SolarizedDark"),
        new("Solarizado Claro", "SolarizedLight"),
    ];

    [ObservableProperty]
    private ThemeOption _selectedThemeOption = null!;

    [ObservableProperty]
    private string _statusText = "Pronto";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Callback to apply the theme at the application level.</summary>
    public Action<string>? ApplyTheme { get; set; }

    partial void OnSelectedThemeOptionChanged(ThemeOption value)
    {
        if (value is not null)
            ApplyTheme?.Invoke(value.Key);
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
        ViewSize.Small => 120,
        ViewSize.Large => 264,
        _ => 180
    };

    public int CardHeight => CurrentViewSize switch
    {
        ViewSize.Small => 138,
        ViewSize.Large => 288,
        _ => 198
    };

    public int ThumbnailHeight => CurrentViewSize switch
    {
        ViewSize.Small => 90,
        ViewSize.Large => 204,
        _ => 132
    };

    public int CardFontSize => CurrentViewSize switch
    {
        ViewSize.Small => 12,
        ViewSize.Large => 17,
        _ => 14
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

    public string CurrentThemeKey => SelectedThemeOption?.Key ?? "Dark";

    public ObservableCollection<Object3D> Items { get; } = new();
    public ObservableCollection<Attachment> Attachments { get; } = new();
    public ObservableCollection<Tag> ObjectTags { get; } = new();
    public ObservableCollection<CategoryNode> Categories { get; } = new();
    public ObservableCollection<Tag> AllTags { get; } = new();

    [ObservableProperty]
    private Tag? _selectedTag;

    [ObservableProperty]
    private string _newTagName = string.Empty;

    [ObservableProperty]
    private CategoryNode? _selectedCategory;

    public MainWindowViewModel(ObjectRepository repository, LibraryManager libraryManager)
    {
        _repository = repository;
        _libraryManager = libraryManager;
        _selectedSortOption = SortOptions[0];
        _selectedThemeOption = ThemeOptions[0];
        LoadCategories();
        LoadAllTags();
        LoadItems();
    }

    public MainWindowViewModel()
    {
        // Design time constructor
        _selectedSortOption = SortOptions[0];
        _selectedThemeOption = ThemeOptions[0];
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
            SelectedTag = null;
            SearchText = string.Empty;

            LoadCategories();
            LoadAllTags();
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
        if (value != null) SelectedTag = null;
        LoadItems(SearchText);
    }

    partial void OnSelectedTagChanged(Tag? value)
    {
        OnPropertyChanged(nameof(IsAllObjectsSelected));
        if (value != null) SelectedCategory = null;
        LoadItems(SearchText);
    }

    public bool IsAllObjectsSelected => SelectedCategory == null && SelectedTag == null;

    public void LoadCategories(int? selectCategoryId = null)
    {
        var expandedIds = new HashSet<int>();
        CollectExpandedIds(Categories, expandedIds);

        Categories.Clear();
        if (_repository == null) return;

        var allCats = _repository.GetAllCategories().ToList();
        var nodeLookup = new Dictionary<int, CategoryNode>();
        
        foreach (var cat in allCats)
        {
            var node = new CategoryNode(cat);
            if (expandedIds.Contains(cat.Id))
                node.IsExpanded = true;
            nodeLookup[cat.Id] = node;
        }

        foreach (var node in nodeLookup.Values.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase))
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

        if (selectCategoryId.HasValue && nodeLookup.TryGetValue(selectCategoryId.Value, out var targetNode))
        {
            var current = targetNode;
            while (current.Category.ParentCategoryId.HasValue &&
                   nodeLookup.TryGetValue(current.Category.ParentCategoryId.Value, out var parentNode))
            {
                parentNode.IsExpanded = true;
                current = parentNode;
            }
            SelectedCategory = targetNode;
        }
    }

    private static void CollectExpandedIds(IEnumerable<CategoryNode> nodes, HashSet<int> expandedIds)
    {
        foreach (var node in nodes)
        {
            if (node.IsExpanded)
                expandedIds.Add(node.Category.Id);
            CollectExpandedIds(node.Children, expandedIds);
        }
    }

    public void LoadItems(string searchTerm = "")
    {
        if (_repository == null) return;

        Items.Clear();

        IEnumerable<Object3D> results;
        if (SelectedCategory != null)
        {
            // Collect all IDs from the selected category and its children
            var categoryIds = new List<int>();
            CollectCategoryAndChildrenIds(SelectedCategory, categoryIds);
            results = _repository.GetAllObjects()
                .Where(o => o.CategoryId.HasValue && categoryIds.Contains(o.CategoryId.Value));
            // Apply search filter if needed
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                results = results.Where(o =>
                    (o.Name?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (o.Description?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }
        }
        else
        {
            results = _repository.SearchObjects(searchTerm, null, SelectedTag?.Id);
        }

        var sorted = CurrentSortOrder switch
        {
            SortOrder.DateAsc => results.OrderBy(o => o.CreatedAt),
            SortOrder.NameAsc => results.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase),
            SortOrder.NameDesc => results.OrderByDescending(o => o.Name, StringComparer.OrdinalIgnoreCase),
            _ => results.OrderByDescending(o => o.CreatedAt)
        };
        foreach (var item in sorted)
        {
            // Preencher CategoryName
            item.CategoryName = GetCategoryName(item.CategoryId);
            Items.Add(item);
        }
    }

    // Recursively collect all IDs from a category and its children
    private void CollectCategoryAndChildrenIds(CategoryNode node, List<int> ids)
    {
        ids.Add(node.Category.Id);
        foreach (var child in node.Children)
            CollectCategoryAndChildrenIds(child, ids);
    }

    public void LoadAllTags()
    {
        AllTags.Clear();
        if (_repository == null) return;

        foreach (var tag in _repository.GetAllTags())
        {
            AllTags.Add(tag);
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

    public IEnumerable<string> GetImageAttachmentPaths(int objectId)
    {
        if (_repository == null) return [];
        return _repository.GetAttachments(objectId)
            .Where(a => LocalImagePathConverter.SupportedExtensions.Contains(Path.GetExtension(a.FilePath))
                     && File.Exists(a.FilePath))
            .Select(a => a.FilePath);
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

    public async Task<List<Object3D>> ImportFiles(string[] filePaths)
    {
        var importedObjects = new List<Object3D>();
        if (_libraryManager == null) return importedObjects;

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
                var obj = await Task.Run(() => _libraryManager.ImportFile(path, categoryId));
                if (obj != null) importedObjects.Add(obj);
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

        return importedObjects;
    }

    public (int objectsImported, int attachmentsImported, int? createdCategoryId) RunImportFolder(
        string folderPath,
        Action<string>? onProgress = null,
        Action<int, int>? onCounts = null,
        CancellationToken cancellationToken = default)
    {
        if (_libraryManager == null) return (0, 0, null);
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

    public async Task ImportAttachmentsToCategory(string[] filePaths, int categoryId)
    {
        if (_libraryManager == null || _repository == null) return;

        var objects = _repository.GetAllObjects(categoryId: categoryId).ToList();
        if (objects.Count == 0) return;

        IsBusy = true;
        int total = filePaths.Length;
        int imported = 0;

        try
        {
            foreach (var path in filePaths)
            {
                imported++;
                StatusText = $"Anexando {imported}/{total}: {Path.GetFileName(path)} a {objects.Count} objeto(s)";
                foreach (var obj in objects)
                {
                    await Task.Run(() => _libraryManager.ImportAttachment(obj.Id, path));
                }
            }
            LoadAttachments();
            StatusText = $"{imported} arquivo(s) anexado(s) a {objects.Count} objeto(s)";
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

    public async Task ImportAttachmentsToObjects(string[] filePaths, List<Object3D> objects)
    {
        if (_libraryManager == null || objects.Count == 0) return;

        IsBusy = true;
        int total = filePaths.Length;
        int imported = 0;

        try
        {
            foreach (var path in filePaths)
            {
                imported++;
                StatusText = $"Anexando {imported}/{total}: {Path.GetFileName(path)} a {objects.Count} objeto(s)";
                foreach (var obj in objects)
                {
                    await Task.Run(() => _libraryManager.ImportAttachment(obj.Id, path));
                }
            }
            LoadAttachments();
            StatusText = $"{imported} arquivo(s) anexado(s) a {objects.Count} objeto(s)";
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
        LoadAllTags();
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
    private void DeleteTagGlobal(Tag? tag)
    {
        if (tag == null || _repository == null) return;

        _repository.DeleteTag(tag.Id);

        if (SelectedTag?.Id == tag.Id)
            SelectedTag = null;

        LoadAllTags();
        LoadTags();
        LoadItems(SearchText);
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
        if (node == null || _repository == null || _libraryManager == null) return;

        int count = CountObjectsRecursive(node);
        string message = count > 0
            ? $"A pasta \"{node.Name}\" contém {count} objeto(s) 3D.\nOs objetos e seus arquivos serão excluídos permanentemente.\n\nDeseja excluir mesmo assim?"
            : $"Deseja excluir a pasta \"{node.Name}\"?";

        if (ShowConfirmAsync != null)
        {
            bool confirmed = await ShowConfirmAsync("Excluir pasta", message);
            if (!confirmed) return;
        }

        var parentId = node.Category.ParentCategoryId;
        await Task.Run(() => DeleteObjectsRecursive(node));
        _repository.DeleteCategory(node.Category.Id);
        LoadCategories();
        SelectedCategory = parentId.HasValue ? FindCategoryNode(Categories, parentId.Value) : null;
        LoadItems(SearchText);
    }

    private void DeleteObjectsRecursive(CategoryNode node)
    {
        if (_repository == null || _libraryManager == null) return;
        var objects = _repository.GetAllObjects(categoryId: node.Category.Id);
        foreach (var obj in objects)
            _libraryManager.DeleteObject(obj);
        foreach (var child in node.Children)
            DeleteObjectsRecursive(child);
    }

    private int CountObjectsRecursive(CategoryNode node)
    {
        if (_repository == null) return 0;
        int count = _repository.CountObjectsInCategory(node.Category.Id);
        foreach (var child in node.Children)
            count += CountObjectsRecursive(child);
        return count;
    }

    private CategoryNode? FindCategoryNode(IEnumerable<CategoryNode> nodes, int categoryId)
    {
        foreach (var node in nodes)
        {
            if (node.Category.Id == categoryId) return node;
            var found = FindCategoryNode(node.Children, categoryId);
            if (found != null) return found;
        }
        return null;
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

    public void MoveCategoryToParent(CategoryNode source, CategoryNode target)
    {
        if (_repository == null) return;
        if (source.Category.ParentCategoryId == target.Category.Id) return;

        source.Category.ParentCategoryId = target.Category.Id;
        _repository.UpdateCategory(source.Category);
        LoadCategories();
    }

    public void MoveCategoryToRoot(CategoryNode source)
    {
        if (_repository == null) return;
        if (source.Category.ParentCategoryId == null) return;

        source.Category.ParentCategoryId = null;
        _repository.UpdateCategory(source.Category);
        LoadCategories();
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

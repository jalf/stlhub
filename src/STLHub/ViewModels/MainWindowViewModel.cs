using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STLHub.Converters;
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
/// Represents a labeled theme option for the UI ComboBox.
/// </summary>
public record ThemeOption(string Label, string Key);

/// <summary>
/// A confirmation the view model asks the view to show.
/// </summary>
/// <param name="Title">Dialog heading.</param>
/// <param name="Message">What will happen, in plain language.</param>
/// <param name="ConfirmLabel">Label of the confirming button; names the action ("Excluir", "Anexar").</param>
/// <param name="IsDestructive">Destructive confirmations use the danger color and never confirm on Enter.</param>
public record ConfirmRequest(string Title, string Message, string ConfirmLabel, bool IsDestructive = true);

/// <summary>
/// What dropping the files currently dragged over the window would do.
/// </summary>
public enum FileDropKind
{
    /// <summary>No file drag in progress, or the drag cannot be dropped.</summary>
    None,
    /// <summary>3D files become objects.</summary>
    ImportFiles,
    /// <summary>Folder trees become categories with their objects.</summary>
    ImportFolders,
    /// <summary>Non-3D files become attachments of the selected category's objects.</summary>
    Attach,
}

/// <summary>
/// Which placeholder the gallery shows when it has no cards.
/// </summary>
public enum GalleryEmptyState
{
    /// <summary>Cards are shown; no placeholder.</summary>
    None,
    /// <summary>A query is taking long enough to deserve an indicator.</summary>
    Loading,
    /// <summary>The library itself has no objects.</summary>
    EmptyLibrary,
    /// <summary>The library has objects, but the search and/or filters match none.</summary>
    NoResults,
}

/// <summary>
/// Main application view model. Manages the object library, categories, search,
/// sorting, theme, and import operations.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private ObjectRepository? _repository;
    private LibraryManager? _libraryManager;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _projectInfoCts;
    private Dictionary<int, CategoryNode> _categoryLookup = new();

    /// <summary>Callback set by the View to display warning dialogs.</summary>
    public Func<string, string, Task>? ShowWarningAsync { get; set; }

    /// <summary>Callback set by the View to display confirmation dialogs.</summary>
    public Func<ConfirmRequest, Task<bool>>? ShowConfirmAsync { get; set; }

    /// <summary>
    /// Callback set by the View to play an object's exit before it is deleted; completes when
    /// the card has left.
    /// </summary>
    public Func<Object3D, Task>? AnimateRemovalAsync { get; set; }

    /// <summary>Callback set by the View to open a folder picker dialog.</summary>
    public Func<Task<string?>>? PickFolderAsync { get; set; }

    /// <summary>Callback invoked when the repository changes, to persist settings.</summary>
    public Action<string>? OnRepositoryChanged { get; set; }

    /// <summary>Callback set by the View to focus and select the active category rename TextBox.</summary>
    public Action? FocusCategoryEditBox { get; set; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _currentRepositoryName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProjectInfo3mfVisible))]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private Object3D? _selectedObject;

    /// <summary>Metadata extracted from the selected <c>.3mf</c> file; <c>null</c> for other file types.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProjectInfo3mfVisible))]
    [NotifyPropertyChangedFor(nameof(HasPlainDescription3mf))]
    [NotifyPropertyChangedFor(nameof(HasRichDescription3mf))]
    private ThreeMfProjectInfo? _projectInfo3mf;

    /// <summary>
    /// Cached because the two properties below are read by bindings, and detecting markup
    /// decodes and scans the whole description. Refreshed when the metadata changes.
    /// </summary>
    private bool _descriptionHasMarkup;

    partial void OnProjectInfo3mfChanged(ThreeMfProjectInfo? value) =>
        _descriptionHasMarkup = HtmlDescriptionParser.HasMarkup(value?.Description);

    /// <summary>
    /// True when the description carries no HTML and can be shown directly in the panel.
    /// </summary>
    public bool HasPlainDescription3mf =>
        !string.IsNullOrWhiteSpace(ProjectInfo3mf?.Description) && !_descriptionHasMarkup;

    /// <summary>
    /// True when the description contains HTML and therefore needs the description viewer.
    /// </summary>
    public bool HasRichDescription3mf => _descriptionHasMarkup;

    /// <summary>True while 3MF metadata extraction is in flight for the current selection.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProjectInfo3mfVisible))]
    private bool _isProjectInfo3mfLoading;

    /// <summary>
    /// True when the "Informações 3MF" section should be shown: a <c>.3mf</c> file is
    /// selected and metadata is either loading or was successfully extracted (FR-009).
    /// </summary>
    public bool IsProjectInfo3mfVisible =>
        SelectedObject?.FileType.Equals(".3mf", StringComparison.OrdinalIgnoreCase) == true &&
        (IsProjectInfo3mfLoading || ProjectInfo3mf?.HasAnyMetadata == true);

    private string _originalName = string.Empty;
    private string _originalDescription = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    [NotifyCanExecuteChangedFor(nameof(SaveMetadataCommand))]
    private string _editedName = string.Empty;

    [ObservableProperty]
    private string _editedDescription = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveMetadataCommand))]
    private bool _hasUnsavedChanges;

    /// <summary>True when the name field of the selected object has been cleared.</summary>
    public bool IsNameInvalid => SelectedObject != null && string.IsNullOrWhiteSpace(EditedName);

    partial void OnEditedNameChanged(string value) =>
        HasUnsavedChanges = value != _originalName || EditedDescription != _originalDescription;

    partial void OnEditedDescriptionChanged(string value) =>
        HasUnsavedChanges = EditedName != _originalName || value != _originalDescription;

    /// <summary>True briefly after "Salvar Alterações" succeeded, so the button confirms the save.</summary>
    [ObservableProperty]
    private bool _justSaved;

    private CancellationTokenSource? _justSavedCts;

    // A new edit means the confirmation no longer describes the fields.
    partial void OnHasUnsavedChangesChanged(bool value)
    {
        if (value) ClearJustSaved();
    }

    private async void ShowJustSaved()
    {
        _justSavedCts?.Cancel();
        var cts = _justSavedCts = new CancellationTokenSource();
        JustSaved = true;
        try { await Task.Delay(1400, cts.Token); }
        catch (OperationCanceledException) { return; }
        JustSaved = false;
    }

    private void ClearJustSaved()
    {
        _justSavedCts?.Cancel();
        JustSaved = false;
    }

    /// <summary>What dropping the files being dragged over the window would do.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFileDropActive))]
    [NotifyPropertyChangedFor(nameof(FileDropTitle))]
    [NotifyPropertyChangedFor(nameof(FileDropHint))]
    private FileDropKind _fileDropKind;

    /// <summary>True while files the app can take are dragged over the window.</summary>
    public bool IsFileDropActive => FileDropKind != FileDropKind.None;

    /// <summary>Headline of the drop overlay: the action the drop performs.</summary>
    public string FileDropTitle => FileDropKind == FileDropKind.Attach ? "Solte para anexar" : "Solte para importar";

    /// <summary>Where the dropped files will end up.</summary>
    public string FileDropHint => FileDropKind switch
    {
        FileDropKind.Attach when SelectedCategory != null =>
            $"Os arquivos serão anexados a todos os objetos de “{SelectedCategory.Name}”.",
        FileDropKind.ImportFolders when SelectedCategory != null =>
            $"As pastas viram subcategorias de “{SelectedCategory.Name}”.",
        FileDropKind.ImportFolders => "As pastas viram categorias da biblioteca.",
        FileDropKind.ImportFiles when SelectedCategory != null =>
            $"Os modelos entram em “{SelectedCategory.Name}”.",
        FileDropKind.ImportFiles => "Os modelos entram na biblioteca, sem categoria.",
        _ => string.Empty,
    };

    // ── Print-in reveal ──────────────────────────────────────────────────────
    // Cards whose thumbnail was just made reveal it once, bottom-up, the first time they show.

    private int? _revealAfterId;
    private readonly HashSet<int> _revealPending = [];
    private readonly HashSet<int> _revealed = [];

    /// <summary>
    /// Marks the start of an import: objects created from now on play the print-in reveal the
    /// first time they are listed.
    /// </summary>
    public void BeginImportSession()
    {
        if (_repository == null) return;
        _revealed.Clear();
        // Animation only: a failed lookup must never block the import itself.
        try { _revealAfterId = _repository.GetMaxObjectId(); }
        catch { _revealAfterId = null; }
    }

    private void ResetFreshPrints()
    {
        _revealAfterId = null;
        _revealPending.Clear();
        _revealed.Clear();
    }

    private void MarkFreshPrint(Object3D item)
    {
        if (_revealed.Contains(item.Id)) return;
        var isFresh = _revealPending.Remove(item.Id) || (_revealAfterId is int after && item.Id > after);
        if (!isFresh) return;
        item.IsFreshPrint = true;
        _revealed.Add(item.Id);
    }

    [ObservableProperty]
    private ViewSize _currentViewSize = ViewSize.Medium;

    /// <summary>
    /// When true, selecting a category also lists objects from its sub-categories.
    /// When false (the default), only objects directly in the selected category are shown.
    /// </summary>
    [ObservableProperty]
    private bool _includeSubcategories;

    partial void OnIncludeSubcategoriesChanged(bool value) => LoadItems(SearchText);

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
    private string _statusText = "Pronto.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActivityVisible))]
    private bool _isBusy;

    /// <summary>True while the object list is being queried.</summary>
    [ObservableProperty]
    private bool _isLoadingItems;

    /// <summary>True once a query has been running long enough to show a loading indicator.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActivityVisible))]
    private bool _isSlowLoading;

    /// <summary>Total number of objects in the library, regardless of search and filters.</summary>
    [ObservableProperty]
    private int _libraryObjectCount;

    /// <summary>True when the status bar activity indicator should run.</summary>
    public bool IsActivityVisible => IsBusy || IsSlowLoading;

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
        OnPropertyChanged(nameof(IsSmallView));
        OnPropertyChanged(nameof(IsMediumView));
        OnPropertyChanged(nameof(IsLargeView));
    }

    /// <summary>Two-way state of the "small" view-size toggle.</summary>
    public bool IsSmallView
    {
        get => CurrentViewSize == ViewSize.Small;
        set { if (value) CurrentViewSize = ViewSize.Small; }
    }

    /// <summary>Two-way state of the "medium" view-size toggle.</summary>
    public bool IsMediumView
    {
        get => CurrentViewSize == ViewSize.Medium;
        set { if (value) CurrentViewSize = ViewSize.Medium; }
    }

    /// <summary>Two-way state of the "large" view-size toggle.</summary>
    public bool IsLargeView
    {
        get => CurrentViewSize == ViewSize.Large;
        set { if (value) CurrentViewSize = ViewSize.Large; }
    }

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
        Items.CollectionChanged += (_, _) => RaiseGalleryStateChanged();
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

    // ── Gallery state: empty, no results, loading, active filters ────────────

    /// <summary>Which placeholder the gallery shows, if any.</summary>
    public GalleryEmptyState EmptyState =>
        Items.Count > 0 ? GalleryEmptyState.None
        : IsLoadingItems ? (IsSlowLoading ? GalleryEmptyState.Loading : GalleryEmptyState.None)
        : LibraryObjectCount == 0 ? GalleryEmptyState.EmptyLibrary
        : GalleryEmptyState.NoResults;

    /// <summary>True when any gallery placeholder is visible.</summary>
    public bool IsEmptyStateVisible => EmptyState != GalleryEmptyState.None;

    /// <summary>True for the "library has no objects yet" placeholder.</summary>
    public bool IsLibraryEmpty => EmptyState == GalleryEmptyState.EmptyLibrary;

    /// <summary>True for the "nothing matches" placeholder.</summary>
    public bool IsNoResults => EmptyState == GalleryEmptyState.NoResults;

    /// <summary>True for the slow-query placeholder.</summary>
    public bool IsLoadingPlaceholder => EmptyState == GalleryEmptyState.Loading;

    /// <summary>True when a search term is active.</summary>
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>True when a category or tag narrows the list.</summary>
    public bool HasActiveFilters => SelectedCategory != null || SelectedTag != null;

    /// <summary>Full path of the selected category, e.g. "Oficina › Suportes".</summary>
    public string? ActiveCategoryLabel => SelectedCategory is { } node ? GetCategoryPath(node) : null;

    /// <summary>Name of the selected tag filter.</summary>
    public string? ActiveTagLabel => SelectedTag?.Name;

    /// <summary>Heading of the "nothing matches" placeholder.</summary>
    public string NoResultsTitle
    {
        get
        {
            if (HasSearchText) return $"Nenhum resultado para “{SearchText.Trim()}”";
            return (SelectedCategory, SelectedTag) switch
            {
                ({ } c, { } t) => $"Nenhum objeto em “{c.Name}” com a tag “{t.Name}”",
                ({ } c, null) => $"Nenhum objeto em “{c.Name}”",
                (null, { } t) => $"Nenhum objeto com a tag “{t.Name}”",
                _ => "Nenhum objeto",
            };
        }
    }

    /// <summary>Guidance under the "nothing matches" heading.</summary>
    public string NoResultsMessage
    {
        get
        {
            if (HasSearchText && HasActiveFilters)
                return "A busca considera só os filtros ativos. Tente buscar em todos os objetos.";
            if (HasSearchText)
                return "Confira a grafia ou tente outra palavra do nome ou da descrição.";
            if (SelectedCategory is { Children.Count: > 0 } && !IncludeSubcategories && SelectedTag == null)
                return "Os objetos podem estar nas subcategorias. Marque “Incluir subpastas” para vê-los aqui.";
            if (SelectedCategory != null && SelectedTag == null)
                return "Arraste arquivos 3D até aqui para importá-los nesta categoria.";
            return "Nenhum objeto combina com os filtros ativos.";
        }
    }

    /// <summary>Status bar count, e.g. "16 objetos" or "4 de 230 objetos" while filtered.</summary>
    public string ItemCountText =>
        (HasSearchText || HasActiveFilters) && LibraryObjectCount > Items.Count
            ? $"{Items.Count} de {Plural.Objects(LibraryObjectCount)}"
            : Plural.Objects(Items.Count);

    private void RaiseGalleryStateChanged()
    {
        OnPropertyChanged(nameof(EmptyState));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
        OnPropertyChanged(nameof(IsLibraryEmpty));
        OnPropertyChanged(nameof(IsNoResults));
        OnPropertyChanged(nameof(IsLoadingPlaceholder));
        OnPropertyChanged(nameof(HasSearchText));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ActiveCategoryLabel));
        OnPropertyChanged(nameof(ActiveTagLabel));
        OnPropertyChanged(nameof(NoResultsTitle));
        OnPropertyChanged(nameof(NoResultsMessage));
        OnPropertyChanged(nameof(ItemCountText));
    }

    partial void OnIsLoadingItemsChanged(bool value) => RaiseGalleryStateChanged();
    partial void OnIsSlowLoadingChanged(bool value) => RaiseGalleryStateChanged();
    partial void OnLibraryObjectCountChanged(int value) => RaiseGalleryStateChanged();

    /// <summary>Clears the category and tag filters (the "Todos os Objetos" view).</summary>
    [RelayCommand]
    private void ShowAllObjects()
    {
        SelectedCategory = null;
        SelectedTag = null;
    }

    /// <summary>Removes the category filter, keeping any tag filter.</summary>
    [RelayCommand]
    private void ClearCategoryFilter() => SelectedCategory = null;

    /// <summary>Removes the tag filter, keeping any category filter.</summary>
    [RelayCommand]
    private void ClearTagFilter() => SelectedTag = null;

    private string GetCategoryPath(CategoryNode node)
    {
        var names = new List<string> { node.Name };
        var current = node.Category;
        var guard = 0;
        while (current.ParentCategoryId is int parentId &&
               _categoryLookup.TryGetValue(parentId, out var parent) && guard++ < 32)
        {
            names.Insert(0, parent.Name);
            current = parent.Category;
        }
        return string.Join(" › ", names);
    }

    // ── Repository ───────────────────────────────────────────────────────────

    public async Task SwitchRepository(string repoPath)
    {
        // Edits belong to the library being left; save them before its repository is replaced.
        CommitPendingEdits();

        IsBusy = true;
        StatusText = "Abrindo biblioteca...";

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

            _repository = new ObjectRepository(dbPath, repoPath);
            _libraryManager = new LibraryManager(libraryPath, _repository);
            ResetFreshPrints();

            CurrentRepositoryName = repoPath;
            SelectedObject = null;
            SelectedCategory = null;
            SelectedTag = null;
            SearchText = string.Empty;

            LoadCategories();
            LoadAllTags();
            LoadItems();

            OnRepositoryChanged?.Invoke(repoPath);
            StatusText = "Pronto.";
        }
        catch (Exception ex)
        {
            StatusText = "Não foi possível abrir a biblioteca.";
            if (ShowWarningAsync != null)
                await ShowWarningAsync("Não foi possível abrir a biblioteca",
                    $"{repoPath}\n\n{ErrorMessages.Describe(ex)}");
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

    /// <summary>Clears the search text box.</summary>
    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    // ── Selection and metadata editing ───────────────────────────────────────

    partial void OnSelectedObjectChanging(Object3D? oldValue, Object3D? newValue)
    {
        // Moving to another object (or clearing the selection) keeps the edits made so far.
        if (oldValue != null && !ReferenceEquals(oldValue, newValue) && HasUnsavedChanges)
            SaveEdits(oldValue);
    }

    partial void OnSelectedObjectChanged(Object3D? value)
    {
        _originalName = value?.Name ?? string.Empty;
        _originalDescription = value?.Description ?? string.Empty;
        EditedName = _originalName;
        EditedDescription = _originalDescription;
        HasUnsavedChanges = false;
        ClearJustSaved();
        LoadAttachments();
        LoadTags();
        LoadProjectInfo(value);
    }

    /// <summary>
    /// Saves unsaved name/description edits of the selected object, if any. Called before the
    /// window closes or the library changes, so edits are never silently dropped.
    /// </summary>
    public void CommitPendingEdits()
    {
        if (SelectedObject != null && HasUnsavedChanges)
            SaveEdits(SelectedObject);
    }

    private bool CanSaveMetadata() => HasUnsavedChanges && !string.IsNullOrWhiteSpace(EditedName);

    [RelayCommand(CanExecute = nameof(CanSaveMetadata))]
    private void SaveMetadata()
    {
        if (SelectedObject != null && SaveEdits(SelectedObject))
            ShowJustSaved();
    }

    /// <summary>
    /// Writes the edited name and description to <paramref name="target"/>. A blank name is
    /// never persisted; the object keeps its previous name. Returns false when the write failed.
    /// </summary>
    private bool SaveEdits(Object3D target)
    {
        if (_repository == null) return false;

        var name = EditedName.Trim();
        var blankNameDiscarded = name.Length == 0;
        if (blankNameDiscarded) name = _originalName;
        var description = EditedDescription;

        var previousName = target.Name;
        var previousDescription = target.Description;

        // Nothing left to write (e.g. the only edit was a cleared name): don't claim a save.
        if (name == previousName && description == previousDescription)
        {
            if (ReferenceEquals(target, SelectedObject) && EditedName != name) EditedName = name;
            HasUnsavedChanges = false;
            if (blankNameDiscarded)
                StatusText = $"O nome vazio foi descartado; “{name}” manteve o nome.";
            return true;
        }

        try
        {
            target.Name = name;
            target.Description = description;
            _repository.UpdateObject(target);
        }
        catch (Exception ex)
        {
            target.Name = previousName;
            target.Description = previousDescription;
            StatusText = $"Não foi possível salvar “{name}”. {ErrorMessages.Describe(ex)}";
            return false;
        }

        if (ReferenceEquals(target, SelectedObject))
        {
            _originalName = name;
            _originalDescription = description;
            if (EditedName != name) EditedName = name;
        }
        HasUnsavedChanges = false;
        StatusText = blankNameDiscarded
            ? $"Alterações em “{name}” salvas. O nome vazio foi descartado."
            : $"Alterações em “{name}” salvas.";
        return true;
    }

    /// <summary>
    /// Starts (or clears) the asynchronous 3MF metadata load for the newly selected object.
    /// Any in-flight load for a previous selection is cancelled first.
    /// </summary>
    private void LoadProjectInfo(Object3D? value)
    {
        _projectInfoCts?.Cancel();
        ProjectInfo3mf = null;
        IsProjectInfo3mfLoading = false;

        if (value?.FileType.Equals(".3mf", StringComparison.OrdinalIgnoreCase) != true) return;

        _projectInfoCts = new CancellationTokenSource();
        IsProjectInfo3mfLoading = true;
        _ = LoadProjectInfoAsync(value.MainFilePath, _projectInfoCts.Token);
    }

    private async Task LoadProjectInfoAsync(string filePath, CancellationToken ct)
    {
        var info = await ThreeMfMetadataReader.ReadAsync(filePath, ct);
        if (ct.IsCancellationRequested) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (ct.IsCancellationRequested) return;
            ProjectInfo3mf = info;
            IsProjectInfo3mfLoading = false;
        });
    }

    // ── Filters and loading ──────────────────────────────────────────────────

    partial void OnSelectedCategoryChanged(CategoryNode? value)
    {
        OnPropertyChanged(nameof(IsAllObjectsSelected));
        LoadItems(SearchText);
    }

    partial void OnSelectedTagChanged(Tag? value)
    {
        OnPropertyChanged(nameof(IsAllObjectsSelected));
        LoadItems(SearchText);
    }

    public bool IsAllObjectsSelected => SelectedCategory == null && SelectedTag == null;

    public void LoadCategories(int? selectCategoryId = null)
    {
        var expandedIds = new HashSet<int>();
        CollectExpandedIds(Categories, expandedIds);

        Categories.Clear();
        _categoryLookup = new Dictionary<int, CategoryNode>();
        if (_repository == null) return;

        List<Category> allCats;
        try
        {
            allCats = _repository.GetAllCategories().ToList();
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível carregar as categorias. {ErrorMessages.Describe(ex)}";
            return;
        }

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

        _categoryLookup = nodeLookup;

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
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        _ = LoadItemsAsync(searchTerm, _loadCts.Token);
    }

    private async Task LoadItemsAsync(string searchTerm, CancellationToken ct)
    {
        if (_repository == null) return;

        // Capture all UI-thread state before going async
        List<int>? categoryIds = null;
        if (SelectedCategory != null)
        {
            categoryIds = [];
            if (IncludeSubcategories)
                CollectCategoryAndChildrenIds(SelectedCategory, categoryIds);
            else
                categoryIds.Add(SelectedCategory.Category.Id);
        }
        var tagId = SelectedTag?.Id;
        var sortOrder = CurrentSortOrder;
        var categoryMap = BuildCategoryNameMap();
        var repo = _repository;

        Items.Clear();
        IsLoadingItems = true;

        // Only an unusually slow query earns a visible indicator; quick ones would just flicker.
        _ = Task.Delay(400, ct).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
        {
            if (!ct.IsCancellationRequested && IsLoadingItems) IsSlowLoading = true;
        }), TaskContinuationOptions.OnlyOnRanToCompletion);

        int libraryCount;
        List<Object3D> items;
        try
        {
            (libraryCount, items) = await Task.Run(() =>
            {
                var count = repo.CountObjects();
                var results = repo.SearchObjects(searchTerm, categoryIds, tagId);

                IEnumerable<Object3D> sorted = sortOrder switch
                {
                    SortOrder.DateAsc => results.OrderBy(o => o.CreatedAt),
                    SortOrder.NameAsc => results.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase),
                    SortOrder.NameDesc => results.OrderByDescending(o => o.Name, StringComparer.OrdinalIgnoreCase),
                    _ => results.OrderByDescending(o => o.CreatedAt)
                };
                return (count, sorted.ToList());
            });
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested) return;
            IsSlowLoading = false;
            IsLoadingItems = false;
            StatusText = $"Não foi possível carregar os objetos. {ErrorMessages.Describe(ex)}";
            return;
        }

        if (ct.IsCancellationRequested) return;

        LibraryObjectCount = libraryCount;
        foreach (var item in items)
        {
            item.CategoryName = item.CategoryId.HasValue && categoryMap.TryGetValue(item.CategoryId.Value, out var catName) ? catName : null;
            MarkFreshPrint(item);
            Items.Add(item);
        }
        IsSlowLoading = false;
        IsLoadingItems = false;
    }

    private Dictionary<int, string> BuildCategoryNameMap()
    {
        var map = new Dictionary<int, string>();
        foreach (var node in Categories)
            FlattenCategoryNode(node, map);
        return map;
    }

    private static void FlattenCategoryNode(CategoryNode node, Dictionary<int, string> map)
    {
        map[node.Category.Id] = node.Name;
        foreach (var child in node.Children)
            FlattenCategoryNode(child, map);
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

    /// <summary>
    /// Reloads the selected object's tags. <paramref name="justAddedTagId"/> marks the tag that
    /// was just added, so only its chip animates in.
    /// </summary>
    public void LoadTags(int? justAddedTagId = null)
    {
        ObjectTags.Clear();
        if (_repository == null || SelectedObject == null) return;

        var results = _repository.GetTagsForObject(SelectedObject.Id);
        foreach (var tag in results)
        {
            tag.IsJustAdded = tag.Id == justAddedTagId;
            ObjectTags.Add(tag);
        }
    }

    // ── Import ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Imports 3D files into the selected category and returns only the objects that were new
    /// to the library. Files already catalogued are skipped and reported in the status bar.
    /// </summary>
    public async Task<List<Object3D>> ImportFiles(string[] filePaths)
    {
        var importedObjects = new List<Object3D>();
        if (_libraryManager == null) return importedObjects;

        BeginImportSession();
        IsBusy = true;
        int total = filePaths.Length;
        int processed = 0;
        int duplicates = 0;
        int missing = 0;
        Object3D? lastDuplicate = null;
        string? current = null;

        var categoryId = SelectedCategory?.Category.Id;

        try
        {
            foreach (var path in filePaths)
            {
                processed++;
                current = Path.GetFileName(path);
                StatusText = $"Importando {processed}/{total}: {current}";
                var result = await Task.Run(() => _libraryManager.ImportFile(path, categoryId));
                if (result.Object == null) missing++;
                else if (result.IsDuplicate) { duplicates++; lastDuplicate = result.Object; }
                else importedObjects.Add(result.Object);
            }
            LoadItems(SearchText);
            StatusText = DescribeImport(importedObjects.Count, duplicates, missing, lastDuplicate);
        }
        catch (Exception ex)
        {
            LoadItems(SearchText);
            var done = importedObjects.Count > 0
                ? $" {Plural.Count(importedObjects.Count, "objeto importado", "objetos importados")} antes do erro."
                : string.Empty;
            StatusText = $"Importação interrompida em “{current}”: {ErrorMessages.Describe(ex)}{done}";
        }
        finally
        {
            IsBusy = false;
        }

        return importedObjects;
    }

    private static string DescribeImport(int imported, int duplicates, int missing, Object3D? lastDuplicate)
    {
        var parts = new List<string>();
        if (imported > 0)
            parts.Add($"{Plural.Count(imported, "objeto importado", "objetos importados")}.");
        if (duplicates == 1 && imported == 0 && lastDuplicate != null)
            parts.Add($"Esse arquivo já está na biblioteca como “{lastDuplicate.Name}”.");
        else if (duplicates > 0)
            parts.Add($"{Plural.Count(duplicates, "arquivo já estava", "arquivos já estavam")} na biblioteca e {(duplicates == 1 ? "foi ignorado" : "foram ignorados")}.");
        if (missing > 0)
            parts.Add($"{Plural.Count(missing, "arquivo não foi encontrado", "arquivos não foram encontrados")}.");
        return parts.Count > 0 ? string.Join(" ", parts) : "Nenhum objeto importado.";
    }

    /// <summary>Phrase for the end of a folder import, shared by the dialog and the status bar.</summary>
    public static string DescribeFolderImport(FolderImportResult result)
    {
        var text = $"{Plural.Count(result.ObjectsImported, "objeto", "objetos")} e " +
                   $"{Plural.Count(result.AttachmentsImported, "anexo", "anexos")} importados.";
        if (result.DuplicatesSkipped > 0)
            text += $" {Plural.Count(result.DuplicatesSkipped, "arquivo já estava", "arquivos já estavam")} na biblioteca e " +
                    $"{(result.DuplicatesSkipped == 1 ? "foi ignorado" : "foram ignorados")}.";
        return text;
    }

    public FolderImportResult RunImportFolder(
        string folderPath,
        Action<string>? onProgress = null,
        Action<FolderImportResult>? onCounts = null,
        CancellationToken cancellationToken = default)
    {
        if (_libraryManager == null) return new FolderImportResult(0, 0, 0, null);
        var parentCategoryId = SelectedCategory?.Category.Id;
        return _libraryManager.ImportFolder(folderPath, parentCategoryId,
            onProgress, onCounts, cancellationToken);
    }

    /// <summary>Attaches the given files to the currently selected object.</summary>
    public Task ImportAttachments(string[] filePaths) =>
        SelectedObject is { } obj
            ? ImportAttachmentsTo(filePaths, [obj], string.Empty)
            : Task.CompletedTask;

    /// <summary>Attaches the given files to every object in the specified category.</summary>
    public Task ImportAttachmentsToCategory(string[] filePaths, int categoryId)
    {
        if (_repository == null) return Task.CompletedTask;
        var objects = _repository.GetAllObjects(categoryId: categoryId).ToList();
        return ImportAttachmentsToObjects(filePaths, objects);
    }

    /// <summary>Attaches the given files to each of the specified objects.</summary>
    public Task ImportAttachmentsToObjects(string[] filePaths, List<Object3D> objects) =>
        ImportAttachmentsTo(filePaths, objects, $" a {Plural.Objects(objects.Count)}");

    private async Task ImportAttachmentsTo(string[] filePaths, IReadOnlyList<Object3D> objects, string targetSuffix)
    {
        if (_libraryManager == null || objects.Count == 0) return;

        IsBusy = true;
        int total = filePaths.Length;
        int attached = 0;
        string? current = null;

        try
        {
            foreach (var path in filePaths)
            {
                current = Path.GetFileName(path);
                StatusText = $"Anexando {attached + 1}/{total}: {current}{targetSuffix}";
                foreach (var obj in objects)
                {
                    await Task.Run(() => _libraryManager.ImportAttachment(obj.Id, path));
                }
                attached++;
            }
            StatusText = $"{Plural.Count(attached, "arquivo anexado", "arquivos anexados")}{targetSuffix}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível anexar “{current}”. {ErrorMessages.Describe(ex)}";
        }
        finally
        {
            LoadAttachments();
            IsBusy = false;
        }
    }

    // ── Attachments and files ────────────────────────────────────────────────

    [RelayCommand]
    private async Task RemoveAttachment(Attachment? attachment)
    {
        if (attachment == null || _libraryManager == null || _repository == null) return;

        var kind = attachment.Type.TrimStart('.').ToUpperInvariant();
        bool shared = _repository.CountAttachmentsByFilePath(attachment.FilePath) > 1;
        var message = shared
            ? $"Remover este anexo {kind} do objeto?\n\nOutros objetos continuam com o mesmo arquivo."
            : $"Remover este anexo {kind} do objeto?\n\nO arquivo será apagado da pasta da biblioteca.";

        if (ShowConfirmAsync == null ||
            !await ShowConfirmAsync(new ConfirmRequest("Remover anexo", message, "Remover")))
            return;

        try
        {
            _libraryManager.DeleteAttachment(attachment);
            Attachments.Remove(attachment);
            StatusText = "Anexo removido.";
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível remover o anexo. {ErrorMessages.Describe(ex)}";
        }
    }

    [RelayCommand]
    private async Task OpenFileInSystem(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        if (!File.Exists(filePath))
        {
            if (ShowWarningAsync != null)
                await ShowWarningAsync("Arquivo não encontrado",
                    $"“{Path.GetFileName(filePath)}” não está mais na pasta da biblioteca. " +
                    "Ele pode ter sido movido ou apagado fora do STLHub.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            var extension = Path.GetExtension(filePath);
            if (ShowWarningAsync != null)
                await ShowWarningAsync("Não foi possível abrir o arquivo",
                    $"{ErrorMessages.Describe(ex)}\n\nAssocie um programa aos arquivos {extension} " +
                    "(por exemplo, o seu fatiador) e tente de novo.");
        }
    }

    // ── Tags ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddTag()
    {
        var name = NewTagName.Trim();
        if (name.Length == 0 || SelectedObject == null || _repository == null) return;

        // Reuse an existing tag that differs only in case ("pla" → "PLA").
        var existing = AllTags.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null && ObjectTags.Any(t => t.Id == existing.Id))
        {
            StatusText = $"Este objeto já tem a tag “{existing.Name}”.";
            NewTagName = string.Empty;
            return;
        }

        int tagId;
        try
        {
            var tag = existing ?? _repository.AddOrGetTag(name);
            _repository.AddTagToObject(SelectedObject.Id, tag.Id);
            tagId = tag.Id;
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível adicionar a tag “{name}”. {ErrorMessages.Describe(ex)}";
            return;
        }

        NewTagName = string.Empty;
        LoadTags(tagId);
        LoadAllTags();
    }

    [RelayCommand]
    private void RemoveTag(Tag? tag)
    {
        if (tag == null || SelectedObject == null || _repository == null) return;

        try
        {
            _repository.RemoveTagFromObject(SelectedObject.Id, tag.Id);
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível remover a tag. {ErrorMessages.Describe(ex)}";
            return;
        }
        LoadTags();
        if (SelectedTag?.Id == tag.Id)
            LoadItems(SearchText);
    }

    [RelayCommand]
    private async Task DeleteTagGlobal(Tag? tag)
    {
        if (tag == null || _repository == null) return;

        int count = _repository.CountObjectsWithTag(tag.Id);
        var message = count > 0
            ? $"A tag “{tag.Name}” será removida de {Plural.Objects(count)}. Os objetos não serão apagados."
            : $"A tag “{tag.Name}” não está em nenhum objeto e será apagada.";

        if (ShowConfirmAsync == null ||
            !await ShowConfirmAsync(new ConfirmRequest("Excluir tag", message, "Excluir tag")))
            return;

        try
        {
            _repository.DeleteTag(tag.Id);
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível excluir a tag. {ErrorMessages.Describe(ex)}";
            return;
        }

        if (SelectedTag?.Id == tag.Id)
            SelectedTag = null;

        LoadAllTags();
        LoadTags();
        LoadItems(SearchText);
        StatusText = $"Tag “{tag.Name}” excluída.";
    }

    // ── Categories ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddRootCategory() => AddCategoryUnder(null);

    [RelayCommand]
    private void AddSubCategory(CategoryNode? parent)
    {
        if (parent != null) AddCategoryUnder(parent);
    }

    private void AddCategoryUnder(CategoryNode? parent)
    {
        if (_repository == null) return;

        var cat = new Category { Name = "Nova Categoria", ParentCategoryId = parent?.Category.Id };
        try
        {
            _repository.AddCategory(cat);
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível criar a categoria. {ErrorMessages.Describe(ex)}";
            return;
        }

        LoadCategories(cat.Id);
        if (FindCategoryNode(Categories, cat.Id) is { } node)
        {
            node.BeginEdit();
            FocusCategoryEditBox?.Invoke();
        }
    }

    [RelayCommand]
    private async Task DeleteCategory(CategoryNode? node)
    {
        if (node == null || _repository == null || _libraryManager == null) return;

        int count = CountObjectsRecursive(node);
        var scope = node.Children.Count > 0 ? $"“{node.Name}” e suas subcategorias contêm" : $"“{node.Name}” contém";
        string message = count > 0
            ? $"{scope} {Plural.Objects(count)}.\n\nOs objetos e seus arquivos serão apagados da biblioteca permanentemente."
            : $"A categoria “{node.Name}” está vazia e será removida.";

        if (ShowConfirmAsync == null ||
            !await ShowConfirmAsync(new ConfirmRequest("Excluir categoria", message, "Excluir")))
            return;

        IsBusy = true;
        StatusText = $"Excluindo “{node.Name}”...";
        var parentId = node.Category.ParentCategoryId;
        try
        {
            await Task.Run(() => DeleteObjectsRecursive(node));
            _repository.DeleteCategory(node.Category.Id);
            StatusText = $"Categoria “{node.Name}” excluída.";
        }
        catch (Exception ex)
        {
            StatusText = $"A exclusão de “{node.Name}” não terminou. {ErrorMessages.Describe(ex)}";
        }
        finally
        {
            IsBusy = false;
        }

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
        node.BeginEdit();
    }

    /// <summary>
    /// Ends an inline rename and saves it. A blank name restores the previous one; calling this
    /// when the node is not being edited (e.g. after Esc) does nothing.
    /// </summary>
    public void CommitRenameCategory(CategoryNode node)
    {
        if (_repository == null || !node.IsEditing) return;
        if (!node.EndEdit()) return;

        try
        {
            _repository.UpdateCategory(node.Category);
            OnPropertyChanged(nameof(ActiveCategoryLabel));
            OnPropertyChanged(nameof(NoResultsTitle));
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível renomear a categoria. {ErrorMessages.Describe(ex)}";
            LoadCategories();
        }
    }

    public void MoveObjectToCategory(Object3D obj, CategoryNode? node)
    {
        if (_repository == null) return;
        try
        {
            _repository.UpdateObjectCategory(obj.Id, node?.Category.Id);
            StatusText = node != null ? $"“{obj.Name}” movido para “{node.Name}”." : $"“{obj.Name}” agora está sem categoria.";
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível mover “{obj.Name}”. {ErrorMessages.Describe(ex)}";
        }
        LoadItems(SearchText);
    }

    public void MoveCategoryToParent(CategoryNode source, CategoryNode target)
    {
        if (_repository == null) return;
        if (source.Category.ParentCategoryId == target.Category.Id) return;

        var previousParent = source.Category.ParentCategoryId;
        source.Category.ParentCategoryId = target.Category.Id;
        try
        {
            _repository.UpdateCategory(source.Category);
        }
        catch (Exception ex)
        {
            source.Category.ParentCategoryId = previousParent;
            StatusText = $"Não foi possível mover a categoria. {ErrorMessages.Describe(ex)}";
        }
        LoadCategories();
    }

    public void MoveCategoryToRoot(CategoryNode source)
    {
        if (_repository == null) return;
        if (source.Category.ParentCategoryId == null) return;

        var previousParent = source.Category.ParentCategoryId;
        source.Category.ParentCategoryId = null;
        try
        {
            _repository.UpdateCategory(source.Category);
        }
        catch (Exception ex)
        {
            source.Category.ParentCategoryId = previousParent;
            StatusText = $"Não foi possível mover a categoria. {ErrorMessages.Describe(ex)}";
        }
        LoadCategories();
    }

    // ── Objects ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteObject(Object3D? obj)
    {
        if (obj == null || _libraryManager == null) return;

        if (ShowConfirmAsync == null ||
            !await ShowConfirmAsync(new ConfirmRequest(
                "Excluir objeto",
                $"Excluir “{obj.Name}”?\n\nO arquivo e seus anexos serão apagados da biblioteca. Essa ação não pode ser desfeita.",
                "Excluir")))
            return;

        if (AnimateRemovalAsync != null)
            await AnimateRemovalAsync(obj);

        IsBusy = true;
        StatusText = $"Excluindo “{obj.Name}”...";

        try
        {
            if (SelectedObject?.Id == obj.Id)
                SelectedObject = null;

            await Task.Run(() => _libraryManager.DeleteObject(obj));
            StatusText = $"“{obj.Name}” excluído.";
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível excluir “{obj.Name}”. {ErrorMessages.Describe(ex)}";
        }
        finally
        {
            IsBusy = false;
            LoadItems(SearchText);
        }
    }

    [RelayCommand]
    private async Task RegenerateThumbnail(Object3D? obj)
    {
        if (obj == null || _libraryManager == null) return;

        IsBusy = true;
        StatusText = $"Refazendo a miniatura de “{obj.Name}”...";

        try
        {
            string newPath = await Task.Run(() => _libraryManager.RegenerateThumbnail(obj));
            obj.ThumbnailPath = newPath;

            _revealed.Remove(obj.Id);
            _revealPending.Add(obj.Id);
            LoadItems(SearchText);

            if (SelectedObject?.Id == obj.Id)
            {
                SelectedObject = null;
                SelectedObject = obj;
            }
            StatusText = $"Miniatura de “{obj.Name}” refeita.";
        }
        catch (Exception ex)
        {
            StatusText = $"Não foi possível refazer a miniatura. {ErrorMessages.Describe(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

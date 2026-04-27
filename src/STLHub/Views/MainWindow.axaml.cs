using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using STLHub.Models;
using STLHub.Services;
using STLHub.ViewModels;

namespace STLHub.Views;

/// <summary>
/// Main application window. Manages drag-and-drop import, file dialogs,
/// category tree interactions, and status bar tooltip hints.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".stl", ".3mf", ".obj" };

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);

        // Wire up View callbacks once DataContext is available
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ShowWarningAsync = async (title, message) =>
                {
                    var dialog = new WarningDialog(title, message);
                    await dialog.ShowDialog(this);
                };
                vm.ShowConfirmAsync = async (title, message) =>
                {
                    var dialog = new ConfirmationDialog(title, message);
                    await dialog.ShowDialog(this);
                    return dialog.Result;
                };
                vm.PickFolderAsync = async () =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return null;
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                    {
                        Title = "Selecionar pasta do repositório",
                        AllowMultiple = false
                    });
                    return folders.FirstOrDefault()?.TryGetLocalPath();
                };

                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(vm.IsAllObjectsSelected))
                        UpdateAllObjectsHighlight(vm.IsAllObjectsSelected);
                };
                UpdateAllObjectsHighlight(vm.IsAllObjectsSelected);
            }
        };
    }

    private void UpdateAllObjectsHighlight(bool isActive)
    {
        var border = this.FindControl<Border>("AllObjectsBorder");
        var label = this.FindControl<TextBlock>("AllObjectsLabel");
        if (border == null) return;

        if (isActive && this.TryFindResource("SidebarActiveBg", ActualThemeVariant, out var res) && res is Color color)
        {
            border.Background = new SolidColorBrush(color);
            if (label != null) label.FontWeight = FontWeight.SemiBold;
        }
        else
        {
            border.Background = Brushes.Transparent;
            if (label != null) label.FontWeight = FontWeight.Normal;
        }
    }

    public void ApplySettings(UserSettings settings)
    {
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        if (!double.IsNaN(settings.WindowX) && !double.IsNaN(settings.WindowY))
        {
            Position = new PixelPoint((int)settings.WindowX, (int)settings.WindowY);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        if (settings.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null && settings.SidebarWidth >= 180 && settings.SidebarWidth <= 500)
        {
            mainGrid.ColumnDefinitions[0].Width = new GridLength(settings.SidebarWidth);
        }
    }

    public UserSettings CaptureSettings()
    {
        var settings = new UserSettings
        {
            WindowWidth = Width,
            WindowHeight = Height,
            WindowX = Position.X,
            WindowY = Position.Y,
            IsMaximized = WindowState == WindowState.Maximized
        };

        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null)
        {
            settings.SidebarWidth = mainGrid.ColumnDefinitions[0].ActualWidth;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            settings.ViewSize = vm.CurrentViewSize.ToString();
            settings.SortOrder = vm.CurrentSortOrder.ToString();
            settings.Theme = vm.CurrentThemeKey;
        }

        // Repository path is persisted via the OnRepositoryChanged callback
        var savedSettings = UserSettings.Load();
        settings.LastRepositoryPath = savedSettings.LastRepositoryPath;
        settings.RecentRepositories = savedSettings.RecentRepositories;

        return settings;
    }

    private async void About_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new AboutDialog();
        await dialog.ShowDialog(this);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
        bool hasValidItem = paths.Any(p =>
            Directory.Exists(p!) ||
            (File.Exists(p!) && AllowedExtensions.Contains(Path.GetExtension(p!))));

        e.DragEffects = hasValidItem ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files != null && DataContext is MainWindowViewModel vm)
        {
            var paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
            if (paths.Length == 0) return;

            var folders = paths.Where(p => Directory.Exists(p!)).ToArray();
            var filePaths = paths.Where(p => File.Exists(p!) && AllowedExtensions.Contains(Path.GetExtension(p!))).ToArray();

            if (folders.Length > 0)
            {
                await ImportFoldersWithDialog(vm, folders!);
            }

            if (filePaths.Length > 0)
            {
                await vm.ImportFiles(filePaths!);
            }
        }
    }

    private async Task ImportFoldersWithDialog(MainWindowViewModel vm, string?[] folders)
    {
        var dialog = new ImportProgressDialog();
        var dialogTask = dialog.ShowDialog(this);

        _ = Task.Run(() =>
        {
            int totalObjects = 0;
            int totalAttachments = 0;
            int? lastCreatedCategoryId = null;

            try
            {
                foreach (var folder in folders)
                {
                    if (folder == null) continue;
                    dialog.CancellationToken.ThrowIfCancellationRequested();

                    var result = vm.RunImportFolder(
                        folder,
                        dialog.UpdateStatus,
                        (obj, att) => dialog.UpdateCounts(totalObjects + obj, totalAttachments + att),
                        dialog.CancellationToken);

                    totalObjects += result.objectsImported;
                    totalAttachments += result.attachmentsImported;
                    lastCreatedCategoryId = result.createdCategoryId ?? lastCreatedCategoryId;
                }

                if (dialog.CancellationToken.IsCancellationRequested)
                    dialog.SetCancelled(totalObjects, totalAttachments);
                else
                    dialog.SetFinished(totalObjects, totalAttachments);
            }
            catch (OperationCanceledException)
            {
                dialog.SetCancelled(totalObjects, totalAttachments);
            }
            catch (Exception ex)
            {
                dialog.SetError(ex.Message);
            }

            Dispatcher.UIThread.Post(() =>
            {
                vm.LoadCategories(lastCreatedCategoryId);
                vm.LoadItems(vm.SearchText);
            });
        });

        await dialogTask;
    }

    private async void AttachFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar Anexos",
            AllowMultiple = true
        });

        if (files != null && files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var paths = files.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
            if (paths.Length > 0)
            {
                await vm.ImportAttachments(paths!);
            }
        }
    }

    private void Object3D_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is Object3D obj)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenFileInSystemCommand.Execute(obj.MainFilePath);
            }
        }
    }

    private void Attachment_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is Attachment att)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenFileInSystemCommand.Execute(att.FilePath);
            }
        }
    }

    private void NewTagTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel vm)
        {
            vm.AddTagCommand.Execute(null);
        }
    }

    private Point? _dragStartPoint;
    private bool _isDragging;
    private PointerPressedEventArgs? _dragStartEvent;
    private Object3D? _draggedObject;
    private CategoryNode? _draggedCategory;

    private void Object3D_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (point.Properties.IsLeftButtonPressed)
        {
            _dragStartEvent = e;
            _dragStartPoint = point.Position;
            _isDragging = false;
            if (sender is Control control && control.DataContext is Object3D obj)
            {
                _draggedObject = obj;
            }
        }
    }

    private async void Object3D_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartPoint.HasValue && !_isDragging && _dragStartEvent != null && _draggedObject != null)
        {
            var point = e.GetCurrentPoint(sender as Control);
            if (point.Properties.IsLeftButtonPressed)
            {
                var diff = point.Position - _dragStartPoint.Value;
                if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
                {
                    _isDragging = true;
                    var dragData = new DataTransfer();
                    await DragDrop.DoDragDropAsync(_dragStartEvent, dragData, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragStartEvent = null;
                    _draggedObject = null;
                }
            }
        }
    }

    private void Category_DragOver(object? sender, DragEventArgs e)
    {
        if (_draggedCategory != null)
        {
            var targetNode = (sender as Control)?.DataContext as CategoryNode;
            if (targetNode == null || targetNode == _draggedCategory || IsDescendantOf(targetNode, _draggedCategory))
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
        }
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Category_Drop(object? sender, DragEventArgs e)
    {
        var categoryNode = (sender as Control)?.DataContext as CategoryNode;

        if (_draggedCategory != null && categoryNode != null && DataContext is MainWindowViewModel vm)
        {
            if (categoryNode != _draggedCategory && !IsDescendantOf(categoryNode, _draggedCategory))
            {
                vm.MoveCategoryToParent(_draggedCategory, categoryNode);
                e.Handled = true;
            }
        }
        else if (_draggedObject != null && categoryNode != null && DataContext is MainWindowViewModel vm2)
        {
            vm2.MoveObjectToCategory(_draggedObject, categoryNode);
            e.Handled = true;
        }
    }

    private void Category_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (point.Properties.IsLeftButtonPressed)
        {
            _dragStartEvent = e;
            _dragStartPoint = point.Position;
            _isDragging = false;
            if (sender is Control control && control.DataContext is CategoryNode node)
            {
                _draggedCategory = node;
                _draggedObject = null;
            }
        }
    }

    private async void Category_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStartPoint.HasValue && !_isDragging && _dragStartEvent != null && _draggedCategory != null)
        {
            var point = e.GetCurrentPoint(sender as Control);
            if (point.Properties.IsLeftButtonPressed)
            {
                var diff = point.Position - _dragStartPoint.Value;
                if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
                {
                    _isDragging = true;
                    var dragData = new DataTransfer();
                    await DragDrop.DoDragDropAsync(_dragStartEvent, dragData, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragStartEvent = null;
                    _draggedCategory = null;
                }
            }
        }
    }

    private void RootCategory_DragOver(object? sender, DragEventArgs e)
    {
        if (_draggedCategory != null && _draggedCategory.Category.ParentCategoryId != null)
            e.DragEffects = DragDropEffects.Move;
        else
            e.DragEffects = DragDropEffects.None;
        e.Handled = true;
    }

    private void RootCategory_Drop(object? sender, DragEventArgs e)
    {
        if (_draggedCategory != null && DataContext is MainWindowViewModel vm)
        {
            vm.MoveCategoryToRoot(_draggedCategory);
            e.Handled = true;
        }
    }

    private static bool IsDescendantOf(CategoryNode potentialChild, CategoryNode potentialParent)
    {
        foreach (var child in potentialParent.Children)
        {
            if (child == potentialChild) return true;
            if (IsDescendantOf(potentialChild, child)) return true;
        }
        return false;
    }

    private void AllObjects_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedCategory = null;
            vm.SelectedTag = null;
        }
    }

    private void AddRootCategory_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.AddRootCategoryCommand.Execute(null);
        }
    }

    private void CategoryLabel_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is CategoryNode node)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RenameCategoryCommand.Execute(node);
                Dispatcher.UIThread.Post(() =>
                {
                    var parent = control.Parent as Grid;
                    var textBox = parent?.FindControl<TextBox>("CategoryRenameBox");
                    textBox?.Focus();
                    textBox?.SelectAll();
                }, DispatcherPriority.Input);
            }
        }
        e.Handled = true;
    }

    private void CategoryTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox &&
            textBox.DataContext is CategoryNode node &&
            DataContext is MainWindowViewModel vm)
        {
            if (e.Key == Key.Enter)
            {
                vm.CommitRenameCategory(node);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                node.IsEditing = false;
                e.Handled = true;
            }
        }
    }

    private void CategoryTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox &&
            textBox.DataContext is CategoryNode node &&
            DataContext is MainWindowViewModel vm)
        {
            vm.CommitRenameCategory(node);
        }
    }

    private void DetailThumbnail_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.SelectedObject != null)
        {
            var path = vm.SelectedObject.ThumbnailPath;
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                var viewer = new ImageViewerWindow(path, vm.SelectedObject.Name);
                viewer.Show(this);
            }
        }
        e.Handled = true;
    }

    // Status bar hint support: shows tooltip text in the status bar on hover

    private string? _savedStatusText;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RegisterHintHandlers(this);
    }

    private void RegisterHintHandlers(Control root)
    {
        foreach (var child in GetAllVisualDescendants(root))
        {
            if (child is Control control && ToolTip.GetTip(control) is string tip && !string.IsNullOrEmpty(tip))
            {
                control.PointerEntered += HintPointerEntered;
                control.PointerExited += HintPointerExited;
            }
        }
    }

    private static IEnumerable<Control> GetAllVisualDescendants(Control root)
    {
        var stack = new Stack<Control>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.GetVisualChildren())
            {
                if (child is Control c)
                    stack.Push(c);
            }
        }
    }

    private void HintPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control control &&
            ToolTip.GetTip(control) is string tip &&
            DataContext is MainWindowViewModel vm &&
            !vm.IsBusy)
        {
            _savedStatusText = vm.StatusText;
            vm.StatusText = tip;
        }
    }

    private void HintPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && !vm.IsBusy && _savedStatusText != null)
        {
            vm.StatusText = _savedStatusText;
            _savedStatusText = null;
        }
    }
}
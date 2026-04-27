using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using STLHub.ViewModels;
using STLHub.Views;
using STLHub.Data;
using STLHub.Services;
using System.IO;
using System;
using System.Linq;

namespace STLHub;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = UserSettings.Load();

            // Resolve repository path from saved settings or use default
            string repoPath = settings.LastRepositoryPath;
            if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath))
            {
                repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "STLHub", "DefaultLibrary");
            }
            Directory.CreateDirectory(repoPath);

            string dbPath = Path.Combine(repoPath, "stlhub.db");
            string libraryPath = Path.Combine(repoPath, "Library");
            
            var dbInitializer = new DatabaseInitializer(dbPath);
            dbInitializer.Initialize();

            var repository = new ObjectRepository(dbPath);
            var libraryManager = new LibraryManager(libraryPath, repository);

            var viewSize = Enum.TryParse<ViewSize>(settings.ViewSize, out var vs) ? vs : ViewSize.Medium;
            var sortOrder = Enum.TryParse<SortOrder>(settings.SortOrder, out var so) ? so : SortOrder.DateDesc;
            var themeKey = settings.Theme ?? "Dark";

            // Apply saved theme
            Application.Current!.RequestedThemeVariant = AppThemes.GetVariant(themeKey);

            var vm = new MainWindowViewModel(repository, libraryManager)
            {
                CurrentViewSize = viewSize,
                CurrentSortOrder = sortOrder,
                CurrentRepositoryName = repoPath,
            };

            // Set the theme option without triggering ApplyTheme (callback not set yet)
            var themeMatch = vm.ThemeOptions.FirstOrDefault(t => t.Key == themeKey) ?? vm.ThemeOptions[0];
            vm.SelectedThemeOption = themeMatch;

            vm.ApplyTheme = (key) =>
            {
                Application.Current!.RequestedThemeVariant = AppThemes.GetVariant(key);
            };

            vm.OnRepositoryChanged = (newRepoPath) =>
            {
                var s = UserSettings.Load();
                s.AddRecentRepository(newRepoPath);
                s.Save();
            };

            // Register initial repo
            settings.AddRecentRepository(repoPath);
            settings.Save();

            var mainWindow = new MainWindow
            {
                DataContext = vm,
            };
            mainWindow.ApplySettings(settings);

            mainWindow.Closing += (_, _) =>
            {
                var currentSettings = mainWindow.CaptureSettings();
                currentSettings.Save();
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
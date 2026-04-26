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
            var isDark = settings.Theme != "Light";

            // Apply saved theme
            Application.Current!.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;

            var vm = new MainWindowViewModel(repository, libraryManager)
            {
                CurrentViewSize = viewSize,
                CurrentSortOrder = sortOrder,
                CurrentRepositoryName = repoPath,
                IsDarkTheme = isDark
            };

            vm.ApplyTheme = (dark) =>
            {
                Application.Current!.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
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
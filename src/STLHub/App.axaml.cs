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
using System.Threading.Tasks;

namespace STLHub;

public partial class App : Application
{
    public override void Initialize()
    {
        // Names the macOS application menu ("STLHub", "Hide STLHub", "Quit STLHub")
        // when the app runs unbundled; the .app bundle's CFBundleName covers the rest.
        Name = "STLHub";
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Opens the About window from the macOS application menu item declared in
    /// <c>App.axaml</c>.
    /// </summary>
    private void OnAboutMenuClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
            _ = new AboutDialog().ShowDialog(owner);
    }

    /// <summary>
    /// How long startup waits for a repository located on an external volume to
    /// respond before falling back to the default library. External drives may be
    /// unplugged, asleep, or still spinning up, and a blocking filesystem call would
    /// otherwise freeze the app before its window ever appears.
    /// </summary>
    private static readonly TimeSpan ExternalRepositoryOpenTimeout = TimeSpan.FromSeconds(5);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = UserSettings.Load();

            string defaultRepoPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "STLHub", "DefaultLibrary");

            // Resolve the repository from saved settings, falling back to the default
            // library when the saved location is missing or — for external drives —
            // does not respond within the timeout.
            string repoPath = settings.LastRepositoryPath;
            string? startupWarning = null;

            if (string.IsNullOrWhiteSpace(repoPath) || !TryPrepareRepository(repoPath))
            {
                if (!string.IsNullOrWhiteSpace(repoPath) && !PathEquals(repoPath, defaultRepoPath))
                {
                    startupWarning =
                        $"Não foi possível abrir a biblioteca em:\n{repoPath}\n\n" +
                        "A unidade pode estar desconectada, adormecida ou ainda inicializando. " +
                        "A biblioteca padrão foi aberta no lugar — use \"Abrir repositório\" para tentar novamente.";
                }

                repoPath = defaultRepoPath;
                Directory.CreateDirectory(repoPath);
                new DatabaseInitializer(Path.Combine(repoPath, "stlhub.db")).Initialize();
            }

            string dbPath = Path.Combine(repoPath, "stlhub.db");
            string libraryPath = Path.Combine(repoPath, "Library");

            var repository = new ObjectRepository(dbPath, repoPath);
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
                IncludeSubcategories = settings.IncludeSubcategories,
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

            if (startupWarning != null)
            {
                EventHandler? showStartupWarning = null;
                showStartupWarning = async (_, _) =>
                {
                    mainWindow.Opened -= showStartupWarning;
                    if (vm.ShowWarningAsync is { } showWarning)
                        await showWarning("Biblioteca indisponível", startupWarning);
                };
                mainWindow.Opened += showStartupWarning;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Ensures the repository directory exists and its database schema is initialized.
    /// For repositories on external volumes the work runs under
    /// <see cref="ExternalRepositoryOpenTimeout"/>; if the drive does not respond in
    /// time the method returns <c>false</c> so the caller can fall back to the default
    /// library instead of freezing on blocked I/O. A timed-out probe is abandoned and
    /// completes harmlessly in the background if the drive wakes up later.
    /// </summary>
    private static bool TryPrepareRepository(string repoPath)
    {
        string dbPath = Path.Combine(repoPath, "stlhub.db");

        if (!IsExternalVolumePath(repoPath))
        {
            if (!Directory.Exists(repoPath))
                return false;
            new DatabaseInitializer(dbPath).Initialize();
            return true;
        }

        var probe = Task.Run(() =>
        {
            if (!Directory.Exists(repoPath))
                return false;
            new DatabaseInitializer(dbPath).Initialize();
            return true;
        });

        try
        {
            return probe.Wait(ExternalRepositoryOpenTimeout) && probe.Result;
        }
        catch (AggregateException)
        {
            return false;
        }
    }

    /// <summary>
    /// True when <paramref name="path"/> lives on a macOS external mount point under
    /// <c>/Volumes/</c>, whose availability cannot be assumed at startup.
    /// </summary>
    private static bool IsExternalVolumePath(string path)
        => OperatingSystem.IsMacOS()
           && path.StartsWith("/Volumes/", StringComparison.Ordinal);

    /// <summary>Compares two filesystem paths, ignoring trailing separators and case.</summary>
    private static bool PathEquals(string a, string b)
        => string.Equals(
            a.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            b.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
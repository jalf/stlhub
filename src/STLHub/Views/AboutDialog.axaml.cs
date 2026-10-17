using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using STLHub.Controls;
using STLHub.Services;

namespace STLHub.Views;

/// <summary>
/// The About dialog: emblem, name and version, what the app does, the repository link and
/// the license.
/// </summary>
public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();

        // Same gate as the main window: the emblem's print-in reveal yields to the OS setting.
        if (MotionPreference.IsReducedMotionRequested())
            EmblemReveal.IsVisible = false;
        else
            Opened += async (_, _) => await PrintEmblemAsync();

        var informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (informational != null)
        {
            // Strip source commit hash appended by the SDK (e.g. "0.2.0+abc123")
            var plusIndex = informational.IndexOf('+');
            if (plusIndex > 0) informational = informational[..plusIndex];
            VersionText.Text = $"v{informational}";
        }
        else
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "dev";
        }
    }

    /// <summary>The gallery's print-in reveal, started once the window is on screen.</summary>
    private static readonly Animation EmblemPrint = new()
    {
        Duration = TimeSpan.FromMilliseconds(650),
        Easing = new CubicEaseOut(),
        Children =
        {
            new KeyFrame { Cue = new Cue(0), Setters = { new Setter(PrintReveal.ProgressProperty, 0d) } },
            new KeyFrame { Cue = new Cue(1), Setters = { new Setter(PrintReveal.ProgressProperty, 1d) } },
        },
    };

    private async Task PrintEmblemAsync()
    {
        try
        {
            // A beat for the window to settle on screen before printing starts.
            await Task.Delay(120);
            // The cover must never outlive the reveal: if the animation clock stalls, the
            // emblem still shows after a short wait.
            await Task.WhenAny(EmblemPrint.RunAsync(EmblemReveal), Task.Delay(1500));
        }
        finally { EmblemReveal.IsVisible = false; }
    }

    private void GitHubLink_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var url = "https://github.com/jalf/stlhub";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace STLHub.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
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

using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace STLHub.Views;

/// <summary>
/// Simple window for viewing an image at full size (e.g. thumbnail preview).
/// </summary>
public partial class ImageViewerWindow : Window
{
    public ImageViewerWindow()
    {
        InitializeComponent();
    }

    public ImageViewerWindow(string imagePath, string title) : this()
    {
        Title = title;

        try
        {
            if (File.Exists(imagePath))
                ViewerImage.Source = new Bitmap(imagePath);
        }
        catch (Exception)
        {
            // An unreadable or truncated image leaves the viewer empty rather than crashing it.
        }
    }

    /// <summary>Esc closes the viewer.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}

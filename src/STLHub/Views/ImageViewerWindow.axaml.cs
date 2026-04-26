using System.IO;
using Avalonia.Controls;
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

        if (File.Exists(imagePath))
        {
            ViewerImage.Source = new Bitmap(imagePath);
        }
    }
}

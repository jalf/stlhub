using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace STLHub.Converters;

/// <summary>
/// Converts a local file path string to an Avalonia <see cref="Bitmap"/> for display in Image controls.
/// </summary>
public class LocalImagePathConverter : IValueConverter
{
    // Avalonia only decodes the first frame of GIF/WEBP, which is fine for thumbnails.
    public static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try
            {
                if (SupportedExtensions.Contains(Path.GetExtension(path)))
                    return new Bitmap(path);
            }
            catch { }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

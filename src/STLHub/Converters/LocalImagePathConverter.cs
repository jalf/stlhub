using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace STLHub.Converters;

/// <summary>
/// Converts a local file path string to an Avalonia <see cref="Bitmap"/> for display in Image controls.
/// Supports PNG, JPG, JPEG, and BMP formats. Returns null for missing or unsupported files.
/// </summary>
public class LocalImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && File.Exists(path))
        {
            try
            {
                var ext = Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".webp")
                {
                    // GIF/WEBP: Avalonia suporta apenas o primeiro frame, mas para thumbnail está ok
                    return new Bitmap(path);
                }
            }
            catch
            {
                // ignored
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace STLHub.Converters;

/// <summary>
/// Maps an attachment's file extension to one of the app's vector icons (a
/// <see cref="Avalonia.Media.Geometry"/> resource defined in <c>App.axaml</c>), for use as
/// <c>PathIcon.Data</c>. Unknown extensions get the generic file icon.
/// </summary>
public class FileTypeIconConverter : IValueConverter
{
    private static readonly Dictionary<string, string> IconKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf",  "IconDocument" }, { ".txt", "IconDocument" }, { ".md",  "IconDocument" },
        { ".doc",  "IconDocument" }, { ".docx", "IconDocument" }, { ".log", "IconDocument" },
        { ".xls",  "IconDocument" }, { ".xlsx", "IconDocument" }, { ".csv", "IconDocument" },
        { ".ppt",  "IconDocument" }, { ".pptx", "IconDocument" },
        { ".zip",  "IconArchive" }, { ".rar", "IconArchive" }, { ".7z", "IconArchive" },
        { ".tar",  "IconArchive" }, { ".gz",  "IconArchive" },
        { ".png",  "IconImage" }, { ".jpg", "IconImage" }, { ".jpeg", "IconImage" }, { ".bmp", "IconImage" },
        { ".gif",  "IconImage" }, { ".webp", "IconImage" }, { ".svg", "IconImage" },
        { ".mp4",  "IconVideo" }, { ".avi", "IconVideo" }, { ".mkv", "IconVideo" }, { ".mov", "IconVideo" },
        { ".mp3",  "IconAudio" }, { ".wav", "IconAudio" }, { ".flac", "IconAudio" }, { ".ogg", "IconAudio" },
        { ".html", "IconCode" }, { ".htm", "IconCode" }, { ".json", "IconCode" }, { ".xml", "IconCode" },
        { ".yaml", "IconCode" }, { ".yml", "IconCode" }, { ".ini", "IconCode" }, { ".cfg", "IconCode" },
        { ".py",   "IconCode" }, { ".js",  "IconCode" }, { ".cs",  "IconCode" }, { ".cpp", "IconCode" },
        { ".c",    "IconCode" }, { ".h",   "IconCode" },
        { ".stl",  "IconModel" }, { ".3mf", "IconModel" }, { ".obj", "IconModel" },
        { ".step", "IconModel" }, { ".stp", "IconModel" },
        { ".gcode", "IconPrinter" }, { ".bgcode", "IconPrinter" },
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value is string ext && IconKeys.TryGetValue(ext, out var k) ? k : "IconFile";
        return Application.Current?.TryFindResource(key, out var geometry) == true ? geometry : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

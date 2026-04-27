using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace STLHub.Converters;

public class FileTypeIconConverter : IValueConverter
{
    private static readonly Dictionary<string, string> IconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf",  "📄" },
        { ".txt",  "📝" },
        { ".doc",  "📃" },
        { ".docx", "📃" },
        { ".xls",  "📊" },
        { ".xlsx", "📊" },
        { ".csv",  "📊" },
        { ".ppt",  "📽️" },
        { ".pptx", "📽️" },
        { ".zip",  "📦" },
        { ".rar",  "📦" },
        { ".7z",   "📦" },
        { ".tar",  "📦" },
        { ".gz",   "📦" },
        { ".png",  "🖼️" },
        { ".jpg",  "🖼️" },
        { ".jpeg", "🖼️" },
        { ".bmp",  "🖼️" },
        { ".gif",  "🖼️" },
        { ".webp", "🖼️" },
        { ".svg",  "🖼️" },
        { ".mp4",  "🎬" },
        { ".avi",  "🎬" },
        { ".mkv",  "🎬" },
        { ".mov",  "🎬" },
        { ".mp3",  "🎵" },
        { ".wav",  "🎵" },
        { ".flac", "🎵" },
        { ".ogg",  "🎵" },
        { ".html", "🌐" },
        { ".htm",  "🌐" },
        { ".json", "🔧" },
        { ".xml",  "🔧" },
        { ".yaml", "🔧" },
        { ".yml",  "🔧" },
        { ".ini",  "🔧" },
        { ".cfg",  "🔧" },
        { ".stl",  "🧊" },
        { ".3mf",  "🧊" },
        { ".obj",  "🧊" },
        { ".step", "🧊" },
        { ".stp",  "🧊" },
        { ".gcode","🖨️" },
        { ".py",   "💻" },
        { ".js",   "💻" },
        { ".cs",   "💻" },
        { ".cpp",  "💻" },
        { ".c",    "💻" },
        { ".h",    "💻" },
        { ".md",   "📝" },
        { ".log",  "📋" },
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string ext && IconMap.TryGetValue(ext, out var icon))
            return icon;
        return "📎";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

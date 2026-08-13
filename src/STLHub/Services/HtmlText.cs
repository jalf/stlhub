using System.Net;

namespace STLHub.Services;

/// <summary>
/// Helpers for the HTML-escaped text that slicers write into 3MF metadata.
/// </summary>
public static class HtmlText
{
    /// <summary>
    /// Maximum decode passes. Bounded so a value that legitimately looks like an entity
    /// after decoding cannot spin, and to keep the result predictable.
    /// </summary>
    private const int MaxDecodePasses = 5;

    /// <summary>
    /// Decodes HTML entities repeatedly until the text stops changing.
    /// Producers escape to different depths — Bambu Studio double-escapes, so a single pass
    /// leaves <c>&amp;lt;p&amp;gt;</c> markup and <c>&amp;apos;</c> apostrophes visible to the user.
    /// </summary>
    public static string Decode(string text)
    {
        for (var pass = 0; pass < MaxDecodePasses; pass++)
        {
            var decoded = WebUtility.HtmlDecode(text);
            if (decoded == text) break;
            text = decoded;
        }
        return text;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using STLHub.Models;

namespace STLHub.Services;

/// <summary>
/// Converts the HTML that slicers embed in 3MF <c>Description</c> metadata into a
/// small block/inline document the viewer can render with native controls.
/// </summary>
/// <remarks>
/// Only the tag vocabulary actually observed in real 3MF libraries is understood
/// (paragraphs, headings, lists, emphasis, links, line breaks and images, plus
/// MakerWorld's proprietary <c>boost*</c> tags). Unknown tags are transparent:
/// their text content is kept and the tag itself is ignored, so an unexpected
/// document degrades to readable plain text rather than failing.
/// </remarks>
public static partial class HtmlDescriptionParser
{
    [GeneratedRegex(@"<\s*(/?)\s*([a-zA-Z][a-zA-Z0-9]*)([^>]*?)/?\s*>", RegexOptions.Singleline)]
    private static partial Regex TagPattern { get; }

    [GeneratedRegex("""([a-zA-Z-]+)\s*=\s*("([^"]*)"|'([^']*)')""", RegexOptions.Singleline)]
    private static partial Regex AttributePattern { get; }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern { get; }

    /// <summary>
    /// Tag names treated as real markup. Covers the vocabulary observed in 3MF libraries plus
    /// common inline formatting; anything else is assumed to be bracketed plain text.
    /// </summary>
    private static readonly HashSet<string> KnownTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "div", "span", "section", "figure", "figcaption", "hr",
        "strong", "b", "em", "i", "u", "s", "small", "sub", "sup", "font", "mark",
        "a", "img", "oembed", "video", "iframe",
        "ul", "ol", "li", "dl", "dt", "dd",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "pre", "code", "blockquote",
        "table", "thead", "tbody", "tr", "td", "th",
        "boostme", "boosttitle", "boostcontent",
    };

    /// <summary>Tags that end the current paragraph when opened or closed.</summary>
    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "figure", "ul", "ol", "li", "pre", "blockquote", "table", "tr", "section",
        "h1", "h2", "h3", "h4", "h5", "h6", "boostme", "boosttitle", "boostcontent",
    };

    /// <summary>
    /// True when <paramref name="raw"/> contains recognizable HTML tags once its entities are decoded.
    /// Lets the caller show markup-free descriptions directly instead of behind a viewer.
    /// </summary>
    /// <remarks>
    /// Only known tag names count. Bracketed plain text such as an e-mail address in angle
    /// brackets looks like a tag to the tokenizer, and treating it as markup would route the
    /// description through the parser, which drops unrecognized tags along with their content.
    /// </remarks>
    public static bool HasMarkup(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;

        foreach (Match tag in TagPattern.Matches(HtmlText.Decode(raw)))
        {
            if (KnownTags.Contains(tag.Groups[2].Value)) return true;
        }
        return false;
    }

    /// <summary>
    /// Parses <paramref name="raw"/> into a renderable document.
    /// Returns <see cref="DescriptionDocument.Empty"/> for blank input.
    /// </summary>
    public static DescriptionDocument Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DescriptionDocument.Empty;

        var html = HtmlText.Decode(raw);

        // Plain-text descriptions are common; keep their line breaks and skip tag parsing.
        if (!TagPattern.IsMatch(html)) return ParsePlainText(html);

        var state = new ParserState();
        var position = 0;

        foreach (Match tag in TagPattern.Matches(html))
        {
            state.AppendText(html[position..tag.Index]);
            position = tag.Index + tag.Length;

            var isClosing = tag.Groups[1].Value == "/";
            var name = tag.Groups[2].Value;
            var attributes = tag.Groups[3].Value;

            ApplyTag(state, name, isClosing, attributes);
        }

        state.AppendText(html[position..]);
        state.FlushParagraph();

        return new DescriptionDocument(state.Blocks);
    }

    private static void ApplyTag(ParserState state, string name, bool isClosing, string attributes)
    {
        switch (name.ToLowerInvariant())
        {
            case "br":
                state.AddLineBreak();
                break;

            case "strong" or "b":
                state.Bold += isClosing ? -1 : 1;
                break;

            case "em" or "i":
                state.Italic += isClosing ? -1 : 1;
                break;

            case "a":
                state.Href = isClosing ? null : Attribute(attributes, "href");
                break;

            case "img":
                if (!isClosing && Attribute(attributes, "src") is { } src)
                    state.AddImage(src);
                break;

            case "oembed":
                // Video embeds carry their target in a url attribute; surface it as a link.
                if (!isClosing && Attribute(attributes, "url") is { } url)
                {
                    state.FlushParagraph();
                    state.AddLink(url);
                    state.FlushParagraph();
                }
                break;

            default:
                if (BlockTags.Contains(name))
                {
                    state.FlushParagraph();
                    if (!isClosing) state.Kind = KindFor(name);
                }
                // Any other tag (span, font, …) is inline and contributes nothing but its text.
                break;
        }
    }

    private static DescriptionParagraphKind KindFor(string name) => name.ToLowerInvariant() switch
    {
        "h1" or "h2" or "boosttitle" => DescriptionParagraphKind.Heading,
        "h3" or "h4" or "h5" or "h6" => DescriptionParagraphKind.Subheading,
        "li" => DescriptionParagraphKind.ListItem,
        _ => DescriptionParagraphKind.Body,
    };

    private static DescriptionDocument ParsePlainText(string text)
    {
        var blocks = text
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !IsBlank(line))
            .Select(DescriptionBlock (line) =>
                new DescriptionParagraph([new DescriptionRun(line)]))
            .ToList();

        return blocks.Count == 0 ? DescriptionDocument.Empty : new DescriptionDocument(blocks);
    }

    private static string? Attribute(string attributes, string name)
    {
        foreach (Match match in AttributePattern.Matches(attributes))
        {
            if (!match.Groups[1].Value.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;

            var value = match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        return null;
    }

    /// <summary>
    /// True when the text carries no visible glyphs. The explicit second test is for the
    /// non-breaking space that <c>&amp;nbsp;</c> decodes to, which producers use as a spacer.
    /// </summary>
    private static bool IsBlank(string text) =>
        text.All(c => char.IsWhiteSpace(c) || c == ' ');

    /// <summary>Mutable accumulator used while walking the tag stream.</summary>
    private sealed class ParserState
    {
        private readonly List<DescriptionInline> _inlines = [];

        public List<DescriptionBlock> Blocks { get; } = [];
        public int Bold { get; set; }
        public int Italic { get; set; }
        public string? Href { get; set; }
        public DescriptionParagraphKind Kind { get; set; } = DescriptionParagraphKind.Body;

        public void AppendText(string text)
        {
            if (text.Length == 0) return;

            // HTML collapses runs of whitespace; keep a single separating space.
            var collapsed = WhitespacePattern.Replace(text, " ");
            if (collapsed.Length == 0) return;

            _inlines.Add(new DescriptionRun(collapsed, Bold > 0, Italic > 0, Href));
        }

        public void AddLineBreak() => _inlines.Add(new DescriptionLineBreak());

        public void AddLink(string url) => _inlines.Add(new DescriptionRun(url, Href: url));

        public void AddImage(string source)
        {
            FlushParagraph();
            Blocks.Add(new DescriptionImage(source));
        }

        /// <summary>
        /// Emits the pending inlines as a paragraph and resets paragraph-scoped state.
        /// Paragraphs holding only whitespace (common with <c>&amp;nbsp;</c> spacers) are dropped.
        /// </summary>
        public void FlushParagraph()
        {
            if (_inlines.Count > 0)
            {
                var text = string.Concat(_inlines.OfType<DescriptionRun>().Select(r => r.Text));
                if (!IsBlank(text))
                {
                    Blocks.Add(new DescriptionParagraph(Trim(_inlines), Kind));
                }
            }

            _inlines.Clear();
            Kind = DescriptionParagraphKind.Body;
        }

        /// <summary>Drops leading and trailing line breaks and whitespace-only runs.</summary>
        private static List<DescriptionInline> Trim(List<DescriptionInline> inlines)
        {
            var start = 0;
            var end = inlines.Count - 1;

            while (start <= end && IsPadding(inlines[start])) start++;
            while (end >= start && IsPadding(inlines[end])) end--;

            var trimmed = inlines.GetRange(start, end - start + 1);

            // The first visible run should not start with the space left by the previous tag.
            if (trimmed.Count > 0 && trimmed[0] is DescriptionRun first)
                trimmed[0] = first with { Text = first.Text.TrimStart() };

            return trimmed;
        }

        private static bool IsPadding(DescriptionInline inline) =>
            inline is DescriptionLineBreak || (inline is DescriptionRun run && IsBlank(run.Text));
    }
}

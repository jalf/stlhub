using System.Collections.Generic;

namespace STLHub.Models;

/// <summary>
/// Visual role of a paragraph, used to pick font size and weight when rendering.
/// </summary>
public enum DescriptionParagraphKind
{
    /// <summary>Ordinary body text.</summary>
    Body,

    /// <summary>Top-level heading (<c>h1</c>, <c>h2</c> or MakerWorld's <c>boosttitle</c>).</summary>
    Heading,

    /// <summary>Lower-level heading (<c>h3</c>, <c>h4</c>).</summary>
    Subheading,

    /// <summary>Bulleted list entry (<c>li</c>).</summary>
    ListItem,
}

/// <summary>Base type for the inline content of a paragraph.</summary>
public abstract record DescriptionInline;

/// <summary>
/// A styled span of text inside a paragraph. When <see cref="Href"/> is set the run is a hyperlink.
/// </summary>
public sealed record DescriptionRun(string Text, bool Bold = false, bool Italic = false, string? Href = null)
    : DescriptionInline;

/// <summary>An explicit line break (<c>br</c>) inside a paragraph.</summary>
public sealed record DescriptionLineBreak : DescriptionInline;

/// <summary>Base type for the block-level content of a description.</summary>
public abstract record DescriptionBlock;

/// <summary>A block of flowing text made up of styled runs.</summary>
public sealed record DescriptionParagraph(
    IReadOnlyList<DescriptionInline> Inlines,
    DescriptionParagraphKind Kind = DescriptionParagraphKind.Body) : DescriptionBlock;

/// <summary>A remote image referenced by an <c>img</c> tag.</summary>
public sealed record DescriptionImage(string Source) : DescriptionBlock;

/// <summary>
/// A description parsed from the HTML that slicers embed in 3MF metadata,
/// reduced to the small block/inline vocabulary the viewer can render.
/// </summary>
public sealed record DescriptionDocument(IReadOnlyList<DescriptionBlock> Blocks)
{
    /// <summary>An empty document, used when there is nothing to show.</summary>
    public static readonly DescriptionDocument Empty = new([]);

    /// <summary>True when the document has no renderable content.</summary>
    public bool IsEmpty => Blocks.Count == 0;
}

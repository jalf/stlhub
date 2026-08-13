using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using STLHub.Models;

namespace STLHub.Views;

/// <summary>
/// Modal viewer that renders a 3MF project description with its original formatting,
/// links and images, instead of the raw HTML markup stored in the file.
/// </summary>
public partial class DescriptionViewerWindow : Window
{

    private const long MaxImageBytes = 20L * 1024 * 1024;

    private static readonly HttpClient Http = CreateHttpClient();

    private readonly CancellationTokenSource _cts = new();

    /// <summary>Design-time constructor.</summary>
    public DescriptionViewerWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the viewer for <paramref name="document"/>, captioned with <paramref name="subtitle"/>
    /// (normally the model title).
    /// </summary>
    public DescriptionViewerWindow(DescriptionDocument document, string? subtitle) : this()
    {
        SubtitleText.Text = subtitle;
        SubtitleText.IsVisible = !string.IsNullOrWhiteSpace(subtitle);
        BuildContent(document);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("STLHub");
        return client;
    }

    private void BuildContent(DescriptionDocument document)
    {
        if (document.IsEmpty)
        {
            ContentPanel.Children.Add(new TextBlock
            {
                Text = "Este arquivo não contém uma descrição.",
                FontSize = 13,
                Opacity = 0.6,
            });
            return;
        }

        foreach (var block in document.Blocks)
        {
            switch (block)
            {
                case DescriptionParagraph paragraph:
                    ContentPanel.Children.Add(CreateParagraph(paragraph));
                    break;
                case DescriptionImage image:
                    ContentPanel.Children.Add(CreateImage(image.Source));
                    break;
            }
        }
    }

    private Control CreateParagraph(DescriptionParagraph paragraph)
    {
        // Link runs become embedded buttons that are taller than the text line, so a fixed
        // line height would clip their descenders. Let those paragraphs size themselves.
        var hasLinks = paragraph.Inlines.Any(i => i is DescriptionRun { Href: not null });

        var text = new TextBlock
        {
            FontSize = paragraph.Kind switch
            {
                DescriptionParagraphKind.Heading => 16,
                DescriptionParagraphKind.Subheading => 14,
                _ => 13,
            },
            FontWeight = paragraph.Kind is DescriptionParagraphKind.Heading or DescriptionParagraphKind.Subheading
                ? FontWeight.SemiBold
                : FontWeight.Normal,
            LineHeight = hasLinks ? double.NaN : 20,
            Margin = paragraph.Kind is DescriptionParagraphKind.Heading or DescriptionParagraphKind.Subheading
                ? new Thickness(0, 8, 0, 0)
                : default,
        };

        foreach (var inline in paragraph.Inlines)
        {
            switch (inline)
            {
                case DescriptionLineBreak:
                    text.Inlines?.Add(new LineBreak());
                    break;

                case DescriptionRun { Href: { } href } run:
                    text.Inlines?.Add(new InlineUIContainer(CreateLink(run.Text, href)));
                    break;

                case DescriptionRun run:
                    text.Inlines?.Add(new Run(run.Text)
                    {
                        FontWeight = run.Bold ? FontWeight.Bold : FontWeight.Normal,
                        FontStyle = run.Italic ? FontStyle.Italic : FontStyle.Normal,
                    });
                    break;
            }
        }

        if (paragraph.Kind != DescriptionParagraphKind.ListItem) return text;

        // Bulleted entry: marker in its own column so wrapped lines stay aligned.
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        var bullet = new TextBlock { Text = "•", FontSize = 13, Margin = new Thickness(6, 0, 8, 0) };
        Grid.SetColumn(bullet, 0);
        Grid.SetColumn(text, 1);
        row.Children.Add(bullet);
        row.Children.Add(text);
        return row;
    }

    private Button CreateLink(string caption, string href)
    {
        var label = new TextBlock { Text = caption.Trim(), FontSize = 13, Classes = { "linktext" } };

        var button = new Button
        {
            Classes = { "link" },
            Content = label,
            Tag = href,
        };

        ToolTip.SetTip(button, href);
        button.Click += OpenLink_Click;
        return button;
    }

    private Control CreateImage(string source)
    {
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxHeight = 420,
        };

        var status = new TextBlock
        {
            Text = "Carregando imagem...",
            FontSize = 11,
            Opacity = 0.5,
        };

        var container = new StackPanel { Spacing = 4, Margin = new Thickness(0, 6, 0, 6) };
        container.Children.Add(image);
        container.Children.Add(status);

        _ = LoadImageAsync(image, status, source, _cts.Token);
        return container;
    }

    /// <summary>
    /// Downloads a remote image referenced by the description. Failures are reported inline
    /// rather than thrown, so one unreachable image never breaks the rest of the document.
    /// </summary>
    private static async Task LoadImageAsync(Image target, TextBlock status, string source, CancellationToken ct)
    {
        try
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                status.Text = "Imagem ignorada (endereço não suportado).";
                return;
            }

            using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaxImageBytes)
            {
                status.Text = "Imagem muito grande para exibir.";
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);

            if (buffer.Length > MaxImageBytes)
            {
                status.Text = "Imagem muito grande para exibir.";
                return;
            }

            buffer.Position = 0;
            target.Source = new Bitmap(buffer);
            status.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
            // Window closed while downloading; nothing to report.
        }
        catch (Exception)
        {
            status.Text = "Não foi possível carregar a imagem.";
        }
    }

    private void OpenLink_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url }) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
        }
        catch
        {
            // A malformed address in file metadata must not crash the viewer.
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        HideMinimizeAndMaximizeButtons();
    }

    /// <summary>
    /// Strips the minimize and maximize boxes so the viewer reads as a dialog, while still
    /// letting the user resize it by its edges. Avalonia has no property for this, and
    /// <c>CanResize=false</c> would remove resizing too, so the window style is edited directly.
    /// </summary>
    private void HideMinimizeAndMaximizeButtons()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (TryGetPlatformHandle()?.Handle is not { } handle || handle == IntPtr.Zero) return;

        var style = (long)GetWindowLongPtr(handle, GwlStyle);
        var stripped = style & ~(WsMinimizeBox | WsMaximizeBox);
        if (stripped == style) return;

        SetWindowLongPtr(handle, GwlStyle, (IntPtr)stripped);
        // Force the non-client area to redraw so the buttons disappear immediately.
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
    }

    private const int GwlStyle = -16;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    // DllImport rather than LibraryImport: the latter needs AllowUnsafeBlocks enabled project-wide.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _cts.Dispose();
        base.OnClosed(e);
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace STLHub.Controls;

/// <summary>
/// Covers a thumbnail and uncovers it from the bottom up, layer by layer, with a thin
/// nozzle line riding the edge. Animate <see cref="Progress"/> from 0 (covered) to 1
/// (revealed); at 1 it draws nothing.
/// </summary>
public class PrintReveal : Control
{
    /// <summary>Defines the <see cref="Progress"/> property.</summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<PrintReveal, double>(nameof(Progress), 1d);

    /// <summary>Defines the <see cref="CoverBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> CoverBrushProperty =
        AvaloniaProperty.Register<PrintReveal, IBrush?>(nameof(CoverBrush));

    /// <summary>Defines the <see cref="NozzleBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> NozzleBrushProperty =
        AvaloniaProperty.Register<PrintReveal, IBrush?>(nameof(NozzleBrush));

    /// <summary>How much of the content is revealed, from 0 (none) to 1 (all).</summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>The fill of the part still to be printed; normally the thumbnail well color.</summary>
    public IBrush? CoverBrush
    {
        get => GetValue(CoverBrushProperty);
        set => SetValue(CoverBrushProperty, value);
    }

    /// <summary>The line drawn along the reveal edge.</summary>
    public IBrush? NozzleBrush
    {
        get => GetValue(NozzleBrushProperty);
        set => SetValue(NozzleBrushProperty, value);
    }

    /// <summary>Height of the nozzle line, in device-independent pixels.</summary>
    private const double NozzleThickness = 2;

    /// <summary>The last stretch of the reveal over which the nozzle line fades out.</summary>
    private const double NozzleFadeSpan = 0.18;

    static PrintReveal()
    {
        AffectsRender<PrintReveal>(ProgressProperty, CoverBrushProperty, NozzleBrushProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<PrintReveal>(false);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress >= 1) return;

        var width = Bounds.Width;
        var edge = Math.Round(Bounds.Height * (1 - progress));

        if (CoverBrush != null && edge > 0)
            context.FillRectangle(CoverBrush, new Rect(0, 0, width, edge));

        if (NozzleBrush != null)
        {
            var fade = Math.Clamp((1 - progress) / NozzleFadeSpan, 0, 1);
            using (context.PushOpacity(fade))
                context.FillRectangle(NozzleBrush,
                    new Rect(0, Math.Max(0, edge - NozzleThickness / 2), width, NozzleThickness));
        }
    }
}

using SkiaSharp;

namespace MeineApps.UI.SkiaSharp;

/// <summary>
/// Wiederverwendbares Tooltip-System für SkiaSharp-Charts.
/// Zeichnet ein abgerundetes Tooltip mit Pfeil an einer Position.
/// Unterstützt automatische Positionierung (über/unter dem Datenpunkt).
/// </summary>
public static class SkiaChartTooltip
{
    // Gecachte Paints
    private static readonly SKPaint BackgroundPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint ShadowPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)
    };

    private static readonly SKPaint TextPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private static readonly SKFont TitleFont = new(
        SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
        12f);

    private static readonly SKFont SubTitleFont = new(
        SKTypeface.FromFamilyName("Inter", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
        10f);

    private static readonly SKPaint DotPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint DotGlowPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3f)
    };

    private static readonly SKPaint LinePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        PathEffect = SKPathEffect.CreateDash([4f, 4f], 0)
    };

    /// <summary>
    /// Zeichnet einen hervorgehobenen Datenpunkt mit Glow-Effekt.
    /// </summary>
    public static void DrawHighlightDot(
        SKCanvas canvas,
        float x, float y,
        SKColor color,
        float radius = 5f)
    {
        // Glow
        DotGlowPaint.Color = color.WithAlpha(80);
        canvas.DrawCircle(x, y, radius * 2.5f, DotGlowPaint);

        // Äußerer Ring (weiß)
        DotPaint.Color = SKColors.White;
        canvas.DrawCircle(x, y, radius + 1.5f, DotPaint);

        // Innerer Punkt
        DotPaint.Color = color;
        canvas.DrawCircle(x, y, radius, DotPaint);
    }

    /// <summary>
    /// Zeichnet eine vertikale gestrichelte Linie vom Datenpunkt zum unteren Rand.
    /// </summary>
    public static void DrawVerticalGuide(
        SKCanvas canvas,
        float x, float fromY, float toY,
        SKColor color)
    {
        LinePaint.Color = color.WithAlpha(60);
        canvas.DrawLine(x, fromY, x, toY, LinePaint);
    }

    /// <summary>
    /// Zeichnet ein Tooltip-Popup mit Text an der angegebenen Position.
    /// Positioniert sich automatisch über oder unter dem Punkt.
    /// </summary>
    public static void DrawTooltip(
        SKCanvas canvas,
        float x, float y,
        string title,
        string? subtitle,
        SKColor accentColor,
        SKRect chartBounds)
    {
        const float padding = 8f;
        const float arrowSize = 6f;
        const float cornerRadius = 6f;
        const float gap = 10f;

        // Text-Größen messen
        float titleWidth = TitleFont.MeasureText(title);
        float titleHeight = TitleFont.Size;

        float subtitleWidth = 0;
        float subtitleHeight = 0;
        if (!string.IsNullOrEmpty(subtitle))
        {
            subtitleWidth = SubTitleFont.MeasureText(subtitle);
            subtitleHeight = SubTitleFont.Size + 2f;
        }

        float contentWidth = Math.Max(titleWidth, subtitleWidth);
        float contentHeight = titleHeight + subtitleHeight;

        float boxWidth = contentWidth + padding * 2;
        float boxHeight = contentHeight + padding * 2;

        // Position: über dem Punkt bevorzugt
        bool above = (y - boxHeight - arrowSize - gap) > chartBounds.Top;

        float boxX = x - boxWidth / 2f;
        float boxY = above
            ? y - boxHeight - arrowSize - gap
            : y + arrowSize + gap;

        // Horizontal clampen
        boxX = Math.Clamp(boxX, chartBounds.Left + 2f, chartBounds.Right - boxWidth - 2f);

        var boxRect = new SKRect(boxX, boxY, boxX + boxWidth, boxY + boxHeight);

        // Schatten
        ShadowPaint.Color = SKColors.Black.WithAlpha(40);
        canvas.DrawRoundRect(
            new SKRoundRect(SKRect.Create(boxRect.Left + 1, boxRect.Top + 2, boxRect.Width, boxRect.Height), cornerRadius),
            ShadowPaint);

        // Hintergrund
        BackgroundPaint.Color = SkiaThemeHelper.Card;
        canvas.DrawRoundRect(new SKRoundRect(boxRect, cornerRadius), BackgroundPaint);

        // Akzent-Linie oben
        using var accentPath = new SKPath();
        accentPath.AddArc(
            new SKRect(boxRect.Left, boxRect.Top, boxRect.Left + cornerRadius * 2, boxRect.Top + cornerRadius * 2),
            180, 90);
        accentPath.LineTo(boxRect.Right - cornerRadius, boxRect.Top);
        accentPath.AddArc(
            new SKRect(boxRect.Right - cornerRadius * 2, boxRect.Top, boxRect.Right, boxRect.Top + cornerRadius * 2),
            270, 90);

        using var accentPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = accentColor
        };
        canvas.DrawPath(accentPath, accentPaint);

        // Pfeil
        using var arrowPath = new SKPath();
        float arrowX = Math.Clamp(x, boxRect.Left + 10f, boxRect.Right - 10f);

        if (above)
        {
            arrowPath.MoveTo(arrowX - arrowSize, boxRect.Bottom);
            arrowPath.LineTo(arrowX, boxRect.Bottom + arrowSize);
            arrowPath.LineTo(arrowX + arrowSize, boxRect.Bottom);
        }
        else
        {
            arrowPath.MoveTo(arrowX - arrowSize, boxRect.Top);
            arrowPath.LineTo(arrowX, boxRect.Top - arrowSize);
            arrowPath.LineTo(arrowX + arrowSize, boxRect.Top);
        }

        arrowPath.Close();
        canvas.DrawPath(arrowPath, BackgroundPaint);

        // Text zeichnen
        float textX = boxRect.Left + padding;
        float textY = boxRect.Top + padding + titleHeight - 2f;

        TextPaint.Color = SkiaThemeHelper.TextPrimary;
        canvas.DrawText(title, textX, textY, SKTextAlign.Left, TitleFont, TextPaint);

        if (!string.IsNullOrEmpty(subtitle))
        {
            TextPaint.Color = SkiaThemeHelper.TextMuted;
            canvas.DrawText(subtitle, textX, textY + subtitleHeight + 2f, SKTextAlign.Left, SubTitleFont, TextPaint);
        }
    }

    /// <summary>
    /// Findet den nächsten Datenpunkt zu einer Touch-Position.
    /// Gibt den Index oder -1 zurück.
    /// </summary>
    public static int FindNearestDataPoint(float touchX, float[] dataPointsX, float maxDistance = 30f)
    {
        int nearest = -1;
        float minDist = float.MaxValue;

        for (int i = 0; i < dataPointsX.Length; i++)
        {
            float dist = MathF.Abs(touchX - dataPointsX[i]);
            if (dist < minDist && dist <= maxDistance)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }
}

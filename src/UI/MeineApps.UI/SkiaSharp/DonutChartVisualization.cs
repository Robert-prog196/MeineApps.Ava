using MeineApps.UI.SkiaSharp.Shaders;
using SkiaSharp;

namespace MeineApps.UI.SkiaSharp;

/// <summary>
/// Wiederverwendbarer Donut-Chart-Renderer für alle Apps.
/// Premium-Look: 3D-Tiefe, Segment-Glow, Sweep-Gradient, innerer Schatten.
/// Thread-safe: Verwendet lokale Paint-Objekte statt statischer.
/// </summary>
public static class DonutChartVisualization
{
    /// <summary>
    /// Ein Segment des Donut-Charts.
    /// </summary>
    public struct Segment
    {
        public float Value;
        public SKColor Color;
        public string Label;
        public string ValueText;
    }

    /// <summary>
    /// Rendert einen Donut-Chart mit Premium-Optik.
    /// </summary>
    /// <param name="animationTime">Animationszeit für Shimmer-Effekt auf Segmenten (0 = kein Shimmer)</param>
    public static void Render(SKCanvas canvas, SKRect bounds,
        Segment[] segments, float innerRadiusFraction = 0.55f,
        string? centerText = null, string? centerSubText = null,
        bool showLabels = true, bool showLegend = true,
        float startAngle = -90f, float animationTime = 0f)
    {
        if (segments.Length == 0) return;

        float totalValue = 0f;
        for (int i = 0; i < segments.Length; i++)
            totalValue += segments[i].Value;
        if (totalValue <= 0) return;

        // Layout berechnen
        float legendH = showLegend ? Math.Min(segments.Length * 20f + 8f, bounds.Height * 0.3f) : 0f;
        float chartAreaH = bounds.Height - legendH;
        float chartCenterY = bounds.Top + chartAreaH / 2f;
        float chartCenterX = bounds.MidX;

        float maxRadius = Math.Min(chartAreaH, bounds.Width) / 2f - 14f;
        if (maxRadius <= 10) return;

        float outerRadius = maxRadius;
        float innerRadius = outerRadius * Math.Clamp(innerRadiusFraction, 0.3f, 0.85f);
        float ringThickness = outerRadius - innerRadius;
        float midRadius = (outerRadius + innerRadius) / 2f;

        // === Äußerer Schatten (Drop-Shadow unter dem gesamten Ring) ===
        using var dropShadow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = ringThickness,
            Color = new SKColor(0, 0, 0, 40),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6f)
        };
        canvas.DrawCircle(chartCenterX, chartCenterY + 2f, midRadius, dropShadow);
        dropShadow.MaskFilter?.Dispose();

        // === Segmente zeichnen ===
        float currentAngle = startAngle;
        float gapDeg = segments.Length > 1 ? 2.5f : 0f;
        float totalGap = gapDeg * segments.Length;

        for (int i = 0; i < segments.Length; i++)
        {
            float sweepAngle = (segments[i].Value / totalValue) * (360f - totalGap);
            if (sweepAngle < 0.3f) { currentAngle += sweepAngle + gapDeg; continue; }

            DrawSegment(canvas, chartCenterX, chartCenterY,
                outerRadius, innerRadius, currentAngle, sweepAngle,
                segments[i].Color);

            currentAngle += sweepAngle + gapDeg;
        }

        // === Segment-Glow (pro Segment, äußerer leuchtender Ring) ===
        currentAngle = startAngle;
        for (int i = 0; i < segments.Length; i++)
        {
            float sweepAngle = (segments[i].Value / totalValue) * (360f - totalGap);
            if (sweepAngle < 0.3f) { currentAngle += sweepAngle + gapDeg; continue; }

            DrawSegmentGlow(canvas, chartCenterX, chartCenterY,
                outerRadius, currentAngle, sweepAngle, segments[i].Color);

            currentAngle += sweepAngle + gapDeg;
        }

        // === Inneren Kreis mit Gradient füllen (Tiefe statt flacher Farbe) ===
        using var innerFill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        innerFill.Shader = SKShader.CreateRadialGradient(
            new SKPoint(chartCenterX, chartCenterY - innerRadius * 0.3f),
            innerRadius * 1.4f,
            new[]
            {
                Lighten(SkiaThemeHelper.Card, 0.08f),
                SkiaThemeHelper.Card,
                Darken(SkiaThemeHelper.Card, 0.1f)
            },
            new[] { 0f, 0.6f, 1f },
            SKShaderTileMode.Clamp);
        canvas.DrawCircle(chartCenterX, chartCenterY, innerRadius - 0.5f, innerFill);
        innerFill.Shader?.Dispose();

        // === Innerer Schatten-Ring (deutlichere Tiefe) ===
        using var innerShadow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 5f,
            Color = SKColors.Transparent
        };
        innerShadow.Shader = SKShader.CreateRadialGradient(
            new SKPoint(chartCenterX, chartCenterY),
            innerRadius + 6f,
            new[] { new SKColor(0, 0, 0, 80), new SKColor(0, 0, 0, 20), SKColors.Transparent },
            new[] { 0.85f, 0.95f, 1f },
            SKShaderTileMode.Clamp);
        canvas.DrawCircle(chartCenterX, chartCenterY, innerRadius + 2f, innerShadow);
        innerShadow.Shader?.Dispose();

        // === Highlight-Bogen oben (simuliert Licht von oben auf dem Ring) ===
        using var highlightPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = ringThickness * 0.5f
        };
        highlightPaint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(chartCenterX - outerRadius, chartCenterY - outerRadius),
            new SKPoint(chartCenterX + outerRadius, chartCenterY + outerRadius),
            new[] { new SKColor(255, 255, 255, 50), SKColors.Transparent, SKColors.Transparent },
            new[] { 0f, 0.35f, 1f },
            SKShaderTileMode.Clamp);
        // Nur oberen Bogen zeichnen (ca. 120°)
        var highlightRect = new SKRect(
            chartCenterX - midRadius, chartCenterY - midRadius,
            chartCenterX + midRadius, chartCenterY + midRadius);
        canvas.DrawArc(highlightRect, -150f, 120f, false, highlightPaint);
        highlightPaint.Shader?.Dispose();

        // === Shimmer-Overlay auf dem Ring (animiert, optional) ===
        if (animationTime > 0f)
        {
            canvas.Save();
            // Clip auf Ring-Bereich (Donut-Form)
            using var clipPath = new SKPath();
            clipPath.AddCircle(chartCenterX, chartCenterY, outerRadius);
            clipPath.AddCircle(chartCenterX, chartCenterY, innerRadius);
            clipPath.FillType = SKPathFillType.EvenOdd;
            canvas.ClipPath(clipPath);

            var ringBounds = new SKRect(
                chartCenterX - outerRadius, chartCenterY - outerRadius,
                chartCenterX + outerRadius, chartCenterY + outerRadius);
            SkiaShimmerEffect.DrawShimmerOverlay(canvas, ringBounds, animationTime,
                shimmerColor: SKColors.White.WithAlpha(40),
                stripWidth: 0.12f, speed: 0.3f);
            canvas.Restore();
        }

        // === Labels auf den Segmenten ===
        if (showLabels)
        {
            DrawLabels(canvas, segments, totalValue, totalGap, gapDeg,
                chartCenterX, chartCenterY, midRadius, startAngle);
        }

        // === Center-Text ===
        if (!string.IsNullOrEmpty(centerText))
        {
            DrawCenterText(canvas, chartCenterX, chartCenterY, innerRadius,
                centerText, centerSubText);
        }

        // === Legende ===
        if (showLegend && legendH > 0)
        {
            DrawLegend(canvas, bounds, segments, chartAreaH, legendH);
        }
    }

    /// <summary>
    /// Zeichnet ein einzelnes Segment als gefüllten Bogen mit 3D-Gradient.
    /// Bei >= 359° wird in zwei Hälften aufgeteilt (SkiaSharp ArcTo-Bug).
    /// </summary>
    private static void DrawSegment(SKCanvas canvas, float cx, float cy,
        float outerR, float innerR, float startAngle, float sweepAngle,
        SKColor color)
    {
        var lighter = Lighten(color, 0.35f);
        var darker = Darken(color, 0.25f);
        var darkest = Darken(color, 0.4f);

        var outerRect = new SKRect(cx - outerR, cy - outerR, cx + outerR, cy + outerR);
        var innerRect = new SKRect(cx - innerR, cy - innerR, cx + innerR, cy + innerR);

        using var segmentPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Radiales Gradient: Lichtquelle oben-links → 3D-Tiefe
        segmentPaint.Shader = SKShader.CreateRadialGradient(
            new SKPoint(cx - outerR * 0.25f, cy - outerR * 0.4f),
            outerR * 2.5f,
            new[] { lighter, color, darker, darkest },
            new[] { 0f, 0.3f, 0.7f, 1f },
            SKShaderTileMode.Clamp);

        if (sweepAngle >= 359f)
        {
            float half = sweepAngle / 2f;
            DrawSegmentPath(canvas, cx, cy, outerR, innerR, outerRect, innerRect,
                startAngle, half, segmentPaint);
            DrawSegmentPath(canvas, cx, cy, outerR, innerR, outerRect, innerRect,
                startAngle + half, sweepAngle - half, segmentPaint);
        }
        else
        {
            DrawSegmentPath(canvas, cx, cy, outerR, innerR, outerRect, innerRect,
                startAngle, sweepAngle, segmentPaint);
        }

        segmentPaint.Shader?.Dispose();

        // Highlight-Kante am äußeren Rand (subtiler Glanz)
        using var edgePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            Color = new SKColor(255, 255, 255, 40)
        };

        if (sweepAngle >= 359f)
        {
            canvas.DrawCircle(cx, cy, outerR - 0.5f, edgePaint);
        }
        else
        {
            using var edgePath = new SKPath();
            edgePath.ArcTo(outerRect, startAngle + 1f, sweepAngle - 2f, true);
            canvas.DrawPath(edgePath, edgePaint);
        }

        // Innere dunkle Kante (verstärkt 3D-Effekt)
        using var innerEdge = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = new SKColor(0, 0, 0, 35)
        };

        if (sweepAngle >= 359f)
        {
            canvas.DrawCircle(cx, cy, innerR + 0.5f, innerEdge);
        }
        else
        {
            using var innerEdgePath = new SKPath();
            innerEdgePath.ArcTo(innerRect, startAngle + 1f, sweepAngle - 2f, true);
            canvas.DrawPath(innerEdgePath, innerEdge);
        }
    }

    /// <summary>
    /// Zeichnet einen leuchtenden Glow-Effekt am äußeren Rand eines Segments.
    /// </summary>
    private static void DrawSegmentGlow(SKCanvas canvas, float cx, float cy,
        float outerR, float startAngle, float sweepAngle, SKColor color)
    {
        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4f,
            Color = color.WithAlpha(35),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)
        };

        var glowRect = new SKRect(cx - outerR - 1f, cy - outerR - 1f,
            cx + outerR + 1f, cy + outerR + 1f);

        if (sweepAngle >= 359f)
        {
            canvas.DrawCircle(cx, cy, outerR + 1f, glowPaint);
        }
        else
        {
            canvas.DrawArc(glowRect, startAngle, sweepAngle, false, glowPaint);
        }

        glowPaint.MaskFilter?.Dispose();
    }

    /// <summary>
    /// Zeichnet einen einzelnen Segment-Path (Outer-Arc CW → LineTo → Inner-Arc CCW → Close).
    /// </summary>
    private static void DrawSegmentPath(SKCanvas canvas, float cx, float cy,
        float outerR, float innerR, SKRect outerRect, SKRect innerRect,
        float startAngle, float sweepAngle, SKPaint paint)
    {
        using var path = new SKPath();

        path.ArcTo(outerRect, startAngle, sweepAngle, true);

        float endAngleRad = (startAngle + sweepAngle) * MathF.PI / 180f;
        float innerEndX = cx + MathF.Cos(endAngleRad) * innerR;
        float innerEndY = cy + MathF.Sin(endAngleRad) * innerR;
        path.LineTo(innerEndX, innerEndY);

        path.ArcTo(innerRect, startAngle + sweepAngle, -sweepAngle, false);
        path.Close();

        canvas.DrawPath(path, paint);
    }

    /// <summary>
    /// Zeichnet Prozent-Labels auf den Segmenten mit Schatten für Lesbarkeit.
    /// </summary>
    private static void DrawLabels(SKCanvas canvas, Segment[] segments,
        float totalValue, float totalGap, float gapDeg,
        float cx, float cy, float midR, float startAngle)
    {
        using var textPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var valueFont = new SKFont { Size = 12f };

        float currentAngle = startAngle;
        for (int i = 0; i < segments.Length; i++)
        {
            float sweepAngle = (segments[i].Value / totalValue) * (360f - totalGap);
            if (sweepAngle < 25f || string.IsNullOrEmpty(segments[i].ValueText))
            {
                currentAngle += sweepAngle + gapDeg;
                continue;
            }

            float midAngleRad = (currentAngle + sweepAngle / 2f) * MathF.PI / 180f;
            float labelX = cx + MathF.Cos(midAngleRad) * midR;
            float labelY = cy + MathF.Sin(midAngleRad) * midR;

            valueFont.Size = sweepAngle > 40f ? 13f : 10f;

            // Hintergrund-Pill für bessere Lesbarkeit
            using var bgPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = new SKColor(0, 0, 0, 100)
            };
            float textW = valueFont.MeasureText(segments[i].ValueText, textPaint);
            float pillW = textW + 10f;
            float pillH = valueFont.Size + 6f;
            canvas.DrawRoundRect(
                new SKRect(labelX - pillW / 2f, labelY - pillH / 2f + 2f,
                    labelX + pillW / 2f, labelY + pillH / 2f + 2f),
                pillH / 2f, pillH / 2f, bgPaint);

            // Text
            textPaint.Color = SKColors.White;
            canvas.DrawText(segments[i].ValueText, labelX, labelY + valueFont.Size * 0.35f,
                SKTextAlign.Center, valueFont, textPaint);

            currentAngle += sweepAngle + gapDeg;
        }
    }

    /// <summary>
    /// Zeichnet den Center-Text im Donut-Loch.
    /// </summary>
    private static void DrawCenterText(SKCanvas canvas, float cx, float cy,
        float innerR, string centerText, string? centerSubText)
    {
        using var centerPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        float fontSize = Math.Clamp(innerR * 0.5f, 14f, 24f);
        using var centerFont = new SKFont { Size = fontSize };
        using var subFont = new SKFont { Size = Math.Clamp(innerR * 0.25f, 9f, 12f) };

        centerPaint.Color = SkiaThemeHelper.TextPrimary;
        canvas.DrawText(centerText, cx, cy + fontSize * 0.15f,
            SKTextAlign.Center, centerFont, centerPaint);

        if (!string.IsNullOrEmpty(centerSubText))
        {
            centerPaint.Color = SkiaThemeHelper.TextMuted;
            canvas.DrawText(centerSubText, cx, cy + fontSize * 0.15f + subFont.Size + 4f,
                SKTextAlign.Center, subFont, centerPaint);
        }
    }

    /// <summary>
    /// Zeichnet die Legende unter dem Chart.
    /// </summary>
    private static void DrawLegend(SKCanvas canvas, SKRect bounds,
        Segment[] segments, float chartAreaH, float legendH)
    {
        float legendTop = bounds.Top + chartAreaH + 6f;
        float legendLeft = bounds.Left + 16f;
        float itemH = 18f;
        int maxItems = (int)(legendH / itemH);

        using var dotPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var textPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var labelFont = new SKFont { Size = 11f };

        for (int i = 0; i < Math.Min(segments.Length, maxItems); i++)
        {
            float y = legendTop + i * itemH;
            float dotSize = 9f;

            // Farb-Punkt mit subtiler Rundung
            dotPaint.Color = segments[i].Color;
            canvas.DrawRoundRect(new SKRect(legendLeft, y, legendLeft + dotSize, y + dotSize), 3f, 3f, dotPaint);

            // Label-Text
            textPaint.Color = SkiaThemeHelper.TextSecondary;
            string legendText = segments[i].Label;
            if (!string.IsNullOrEmpty(segments[i].ValueText))
                legendText += $" ({segments[i].ValueText})";
            canvas.DrawText(legendText, legendLeft + dotSize + 8f, y + dotSize - 1f,
                SKTextAlign.Left, labelFont, textPaint);
        }
    }

    private static SKColor Lighten(SKColor color, float amount)
    {
        return new SKColor(
            (byte)Math.Min(255, color.Red + (255 - color.Red) * amount),
            (byte)Math.Min(255, color.Green + (255 - color.Green) * amount),
            (byte)Math.Min(255, color.Blue + (255 - color.Blue) * amount),
            color.Alpha);
    }

    private static SKColor Darken(SKColor color, float amount)
    {
        return new SKColor(
            (byte)(color.Red * (1f - amount)),
            (byte)(color.Green * (1f - amount)),
            (byte)(color.Blue * (1f - amount)),
            color.Alpha);
    }
}

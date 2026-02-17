using SkiaSharp;
using MeineApps.UI.SkiaSharp;

namespace FitnessRechner.Graphics;

/// <summary>
/// SkiaSharp-Renderer für Wochen-Kalorien-Balkendiagramm.
/// Ersetzt LiveCharts ColumnSeries&lt;double&gt;.
/// Premium-Look: Abgerundete Balken mit Gradient, Ziel-Linie, Y-Grid.
/// Thread-safe: Verwendet lokale Paint-Objekte.
/// </summary>
public static class WeeklyCaloriesBarVisualization
{
    /// <summary>
    /// Rendert ein Balkendiagramm für 7 Wochentage mit optionaler Ziel-Linie.
    /// </summary>
    public static void Render(SKCanvas canvas, SKRect bounds,
        string[] dayLabels, float[] values,
        float targetCalories = 0f,
        SKColor? barStartColor = null,
        SKColor? barEndColor = null)
    {
        if (values.Length == 0) return;

        var startColor = barStartColor ?? new SKColor(0xF5, 0x9E, 0x0B); // #F59E0B Amber
        var endColor = barEndColor ?? new SKColor(0xEF, 0x44, 0x44);     // #EF4444 Red

        // Layout
        float leftMargin = 36f;
        float bottomMargin = 22f;
        float topMargin = 8f;
        float rightMargin = 8f;

        var chartRect = new SKRect(
            bounds.Left + leftMargin,
            bounds.Top + topMargin,
            bounds.Right - rightMargin,
            bounds.Bottom - bottomMargin);

        if (chartRect.Width < 20 || chartRect.Height < 20) return;

        // Y-Bereich
        float yMax = values.Max();
        if (targetCalories > 0) yMax = Math.Max(yMax, targetCalories);
        yMax *= 1.15f; // 15% Padding oben
        if (yMax <= 0) yMax = 2500;

        // Balken-Geometrie
        int barCount = values.Length;
        float barAreaWidth = chartRect.Width / barCount;
        float maxBarWidth = Math.Min(barAreaWidth * 0.65f, 30f);

        // Y-Koordinate berechnen
        float ToY(float val) => chartRect.Bottom - (val / yMax) * chartRect.Height;

        // Grid-Linien
        DrawYGrid(canvas, chartRect, yMax);

        // Ziel-Linie (gestrichelt)
        if (targetCalories > 0)
        {
            float targetY = ToY(targetCalories);
            using var targetPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                Color = SkiaThemeHelper.WithAlpha(SkiaThemeHelper.Success, 150),
                PathEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0)
            };
            canvas.DrawLine(chartRect.Left, targetY, chartRect.Right, targetY, targetPaint);
            targetPaint.PathEffect?.Dispose();

            // Ziel-Label
            using var targetLabelPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SkiaThemeHelper.WithAlpha(SkiaThemeHelper.Success, 200)
            };
            using var targetFont = new SKFont { Size = 9f };
            canvas.DrawText($"{targetCalories:F0}", chartRect.Right + 2f, targetY + 3f,
                SKTextAlign.Left, targetFont, targetLabelPaint);
        }

        // Balken zeichnen
        for (int i = 0; i < barCount; i++)
        {
            float centerX = chartRect.Left + barAreaWidth * (i + 0.5f);
            float barLeft = centerX - maxBarWidth / 2f;
            float barRight = centerX + maxBarWidth / 2f;
            float barBottom = chartRect.Bottom;
            float barTop = values[i] > 0 ? ToY(values[i]) : barBottom;

            float barHeight = barBottom - barTop;
            if (barHeight < 2f) continue;

            float cornerR = Math.Min(4f, barHeight / 2f);

            // Gradient pro Balken (oben→unten: Start→End)
            using var barPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // Balkenfarbe: Überschreitung=Rot-betont, Normal=Amber→Orange Gradient
            SKColor topColor, botColor;
            if (targetCalories > 0 && values[i] > targetCalories)
            {
                topColor = endColor;
                botColor = new SKColor(0xDC, 0x26, 0x26); // Dunkleres Rot
            }
            else
            {
                topColor = startColor;
                botColor = SkiaThemeHelper.WithAlpha(endColor, 200);
            }

            barPaint.Shader = SKShader.CreateLinearGradient(
                new SKPoint(centerX, barTop),
                new SKPoint(centerX, barBottom),
                new[] { topColor, botColor },
                null, SKShaderTileMode.Clamp);

            canvas.DrawRoundRect(new SKRect(barLeft, barTop, barRight, barBottom),
                cornerR, cornerR, barPaint);
            barPaint.Shader?.Dispose();

            // Highlight-Kante (oben links)
            using var highlightPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                Color = new SKColor(255, 255, 255, 40)
            };
            canvas.DrawRoundRect(new SKRect(barLeft, barTop, barRight, barBottom),
                cornerR, cornerR, highlightPaint);

            // Wert über dem Balken (nur wenn genug Platz)
            if (values[i] > 0 && barHeight > 10f)
            {
                using var valPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = SkiaThemeHelper.TextPrimary
                };
                using var valFont = new SKFont { Size = 9f };
                canvas.DrawText($"{values[i]:F0}", centerX, barTop - 4f,
                    SKTextAlign.Center, valFont, valPaint);
            }
        }

        // X-Achsen-Labels (Wochentage)
        DrawXLabels(canvas, chartRect, dayLabels, barAreaWidth, barCount);
    }

    /// <summary>
    /// Zeichnet horizontale Grid-Linien mit Y-Labels.
    /// </summary>
    private static void DrawYGrid(SKCanvas canvas, SKRect chartRect, float yMax)
    {
        using var gridPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            Color = new SKColor(128, 128, 128, 25)
        };
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(128, 128, 128, 180)
        };
        using var labelFont = new SKFont { Size = 10f };

        // ~4 Grid-Linien
        float step = RoundToNice(yMax / 4f);
        if (step <= 0) step = 500;

        for (float v = step; v < yMax; v += step)
        {
            float y = chartRect.Bottom - (v / yMax) * chartRect.Height;
            if (y < chartRect.Top) break;

            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y, gridPaint);
            canvas.DrawText($"{v:F0}", chartRect.Left - 4f, y + 4f,
                SKTextAlign.Right, labelFont, labelPaint);
        }
    }

    /// <summary>
    /// Zeichnet X-Achsen-Labels (Wochentage).
    /// </summary>
    private static void DrawXLabels(SKCanvas canvas, SKRect chartRect,
        string[] labels, float barAreaWidth, int barCount)
    {
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(128, 128, 128, 180)
        };
        using var labelFont = new SKFont { Size = 11f };

        for (int i = 0; i < Math.Min(labels.Length, barCount); i++)
        {
            float centerX = chartRect.Left + barAreaWidth * (i + 0.5f);
            canvas.DrawText(labels[i], centerX, chartRect.Bottom + 16f,
                SKTextAlign.Center, labelFont, labelPaint);
        }
    }

    /// <summary>
    /// Rundet auf einen "schönen" Wert für Grid-Schritte.
    /// </summary>
    private static float RoundToNice(float value)
    {
        if (value <= 0) return 500f;
        float magnitude = MathF.Pow(10, MathF.Floor(MathF.Log10(value)));
        float residual = value / magnitude;

        return residual switch
        {
            <= 1.5f => 1f * magnitude,
            <= 3f => 2f * magnitude,
            <= 7f => 5f * magnitude,
            _ => 10f * magnitude
        };
    }
}

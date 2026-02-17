using SkiaSharp;
using MeineApps.UI.SkiaSharp;

namespace FitnessRechner.Graphics;

/// <summary>
/// SkiaSharp-Renderer für Health-Trend-Kurven (Gewicht, BMI, Körperfett).
/// Ersetzt LiveCharts LineSeries&lt;DateTimePoint&gt;.
/// Premium-Look: Catmull-Rom Spline, Gradient-Füllung, Endpunkt-Dot, optionale Zielzone.
/// Thread-safe: Verwendet lokale Paint-Objekte.
/// </summary>
public static class HealthTrendVisualization
{
    /// <summary>
    /// Ein Datenpunkt im Trend-Chart.
    /// </summary>
    public struct DataPoint
    {
        public DateTime Date;
        public float Value;
    }

    /// <summary>
    /// Optionale horizontale Zone (z.B. gesunder BMI-Bereich 18.5-25).
    /// </summary>
    public struct TargetZone
    {
        public float MinValue;
        public float MaxValue;
        public SKColor Color;
    }

    /// <summary>
    /// Optionale vertikale Meilenstein-Markierung.
    /// </summary>
    public struct MilestoneLine
    {
        public DateTime Date;
        public SKColor Color;
    }

    /// <summary>
    /// Rendert eine Trend-Kurve mit Achsen, Gradient-Füllung und optionalen Zonen.
    /// </summary>
    public static void Render(SKCanvas canvas, SKRect bounds,
        DataPoint[] data, SKColor lineColor,
        TargetZone? targetZone = null,
        MilestoneLine[]? milestones = null,
        string? yLabelFormat = null,
        float yPadding = 5f)
    {
        if (data.Length == 0) return;

        // Layout: Ränder für Achsen-Labels
        float leftMargin = 40f;
        float bottomMargin = 28f;
        float topMargin = 8f;
        float rightMargin = 8f;

        var chartRect = new SKRect(
            bounds.Left + leftMargin,
            bounds.Top + topMargin,
            bounds.Right - rightMargin,
            bounds.Bottom - bottomMargin);

        if (chartRect.Width < 20 || chartRect.Height < 20) return;

        // Daten sortieren
        var sorted = data.OrderBy(d => d.Date).ToArray();

        // Y-Bereich berechnen
        float yMin = sorted.Min(d => d.Value);
        float yMax = sorted.Max(d => d.Value);

        // Zielzone in Y-Bereich einbeziehen
        if (targetZone != null)
        {
            yMin = Math.Min(yMin, targetZone.Value.MinValue);
            yMax = Math.Max(yMax, targetZone.Value.MaxValue);
        }

        // Padding
        float yRange = yMax - yMin;
        if (yRange < 0.1f) yRange = 1f;
        yMin -= yPadding;
        yMax += yPadding;
        yRange = yMax - yMin;

        // X-Bereich
        long xMin = sorted[0].Date.Ticks;
        long xMax = sorted[^1].Date.Ticks;
        long xRange = xMax - xMin;
        if (xRange <= 0) xRange = TimeSpan.FromDays(1).Ticks;

        // Koordinaten-Transformation
        float ToX(long ticks) => chartRect.Left + (float)(ticks - xMin) / xRange * chartRect.Width;
        float ToY(float val) => chartRect.Bottom - (val - yMin) / yRange * chartRect.Height;

        // Grid-Linien zeichnen
        DrawGrid(canvas, chartRect, yMin, yMax, yLabelFormat ?? "F1");

        // Zielzone zeichnen
        if (targetZone != null)
        {
            var zone = targetZone.Value;
            float zoneTop = Math.Max(ToY(zone.MaxValue), chartRect.Top);
            float zoneBot = Math.Min(ToY(zone.MinValue), chartRect.Bottom);

            using var zonePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SkiaThemeHelper.WithAlpha(zone.Color, 30)
            };
            canvas.DrawRect(chartRect.Left, zoneTop, chartRect.Width, zoneBot - zoneTop, zonePaint);
        }

        // Meilenstein-Linien zeichnen
        if (milestones is { Length: > 0 })
        {
            using var milestonePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0)
            };

            foreach (var ms in milestones)
            {
                float msX = ToX(ms.Date.Ticks);
                if (msX >= chartRect.Left && msX <= chartRect.Right)
                {
                    milestonePaint.Color = SkiaThemeHelper.WithAlpha(ms.Color, 80);
                    canvas.DrawLine(msX, chartRect.Top, msX, chartRect.Bottom, milestonePaint);
                }
            }

            milestonePaint.PathEffect?.Dispose();
        }

        // Punkte in Canvas-Koordinaten umrechnen
        var points = new SKPoint[sorted.Length];
        for (int i = 0; i < sorted.Length; i++)
        {
            points[i] = new SKPoint(ToX(sorted[i].Date.Ticks), ToY(sorted[i].Value));
        }

        // Spline-Pfad berechnen (Catmull-Rom)
        using var linePath = CreateSplinePath(points, 0.5f);

        // Gradient-Füllung unter der Kurve
        using var fillPath = new SKPath(linePath);
        fillPath.LineTo(points[^1].X, chartRect.Bottom);
        fillPath.LineTo(points[0].X, chartRect.Bottom);
        fillPath.Close();

        var lighterColor = SkiaThemeHelper.WithAlpha(lineColor, 120);
        var transparentColor = SkiaThemeHelper.WithAlpha(lineColor, 8);

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        fillPaint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(chartRect.MidX, chartRect.Top),
            new SKPoint(chartRect.MidX, chartRect.Bottom),
            new[] { lighterColor, transparentColor },
            null, SKShaderTileMode.Clamp);
        canvas.DrawPath(fillPath, fillPaint);
        fillPaint.Shader?.Dispose();

        // Kurve zeichnen
        using var strokePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f,
            Color = lineColor,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(linePath, strokePaint);

        // Glow auf der Linie
        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 6f,
            Color = SkiaThemeHelper.WithAlpha(lineColor, 30),
            StrokeCap = SKStrokeCap.Round,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3f)
        };
        canvas.DrawPath(linePath, glowPaint);
        glowPaint.MaskFilter?.Dispose();

        // Datenpunkte zeichnen
        DrawDataPoints(canvas, points, lineColor);

        // X-Achsen-Labels (Datum)
        DrawXLabels(canvas, chartRect, sorted, bottomMargin, ToX);
    }

    /// <summary>
    /// Erstellt einen Catmull-Rom Spline-Pfad durch die gegebenen Punkte.
    /// </summary>
    private static SKPath CreateSplinePath(SKPoint[] points, float tension)
    {
        var path = new SKPath();

        if (points.Length == 0) return path;
        if (points.Length == 1)
        {
            path.MoveTo(points[0]);
            return path;
        }
        if (points.Length == 2)
        {
            path.MoveTo(points[0]);
            path.LineTo(points[1]);
            return path;
        }

        path.MoveTo(points[0]);

        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i < points.Length - 2 ? points[i + 2] : points[i + 1];

            // Catmull-Rom → Cubic Bezier Kontrollpunkte
            float t = tension;
            var cp1 = new SKPoint(
                p1.X + (p2.X - p0.X) / (6f / t),
                p1.Y + (p2.Y - p0.Y) / (6f / t));
            var cp2 = new SKPoint(
                p2.X - (p3.X - p1.X) / (6f / t),
                p2.Y - (p3.Y - p1.Y) / (6f / t));

            path.CubicTo(cp1, cp2, p2);
        }

        return path;
    }

    /// <summary>
    /// Zeichnet Grid-Linien und Y-Achsen-Labels.
    /// </summary>
    private static void DrawGrid(SKCanvas canvas, SKRect chartRect,
        float yMin, float yMax, string format)
    {
        float yRange = yMax - yMin;
        if (yRange <= 0) return;

        // Auto-Step: ~4-5 Grid-Linien
        float rawStep = yRange / 5f;
        float step = RoundToNice(rawStep);
        if (step <= 0) step = 1f;

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

        float startVal = MathF.Ceiling(yMin / step) * step;
        for (float v = startVal; v <= yMax; v += step)
        {
            float y = chartRect.Bottom - (v - yMin) / yRange * chartRect.Height;
            if (y < chartRect.Top || y > chartRect.Bottom) continue;

            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y, gridPaint);

            string label = v.ToString(format);
            canvas.DrawText(label, chartRect.Left - 4f, y + 4f,
                SKTextAlign.Right, labelFont, labelPaint);
        }
    }

    /// <summary>
    /// Zeichnet Datenpunkte als Kreise mit weißem Rand.
    /// </summary>
    private static void DrawDataPoints(SKCanvas canvas, SKPoint[] points, SKColor color)
    {
        // Zu viele Punkte → nur erste/letzte und jeden N-ten
        int step = Math.Max(1, points.Length / 12);

        using var dotFill = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = color
        };
        using var dotStroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = SKColors.White
        };

        for (int i = 0; i < points.Length; i++)
        {
            // Immer ersten und letzten zeichnen + jeden N-ten
            if (i != 0 && i != points.Length - 1 && i % step != 0) continue;

            float radius = (i == 0 || i == points.Length - 1) ? 5f : 4f;
            canvas.DrawCircle(points[i], radius, dotFill);
            canvas.DrawCircle(points[i], radius, dotStroke);
        }

        // Letzter Punkt mit Glow
        if (points.Length > 0)
        {
            using var lastGlow = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = SkiaThemeHelper.WithAlpha(color, 40),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)
            };
            canvas.DrawCircle(points[^1], 8f, lastGlow);
            lastGlow.MaskFilter?.Dispose();
        }
    }

    /// <summary>
    /// Zeichnet X-Achsen-Labels (Datum).
    /// </summary>
    private static void DrawXLabels(SKCanvas canvas, SKRect chartRect,
        DataPoint[] sorted, float bottomMargin, Func<long, float> toX)
    {
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(128, 128, 128, 180)
        };
        using var labelFont = new SKFont { Size = 10f };

        // Max 5-6 Labels anzeigen
        int labelStep = Math.Max(1, sorted.Length / 6);

        for (int i = 0; i < sorted.Length; i += labelStep)
        {
            float x = toX(sorted[i].Date.Ticks);
            string label = sorted[i].Date.ToString("d MMM");

            canvas.Save();
            canvas.Translate(x, chartRect.Bottom + 6f);
            canvas.RotateDegrees(-35);
            canvas.DrawText(label, 0, 10f, SKTextAlign.Right, labelFont, labelPaint);
            canvas.Restore();
        }

        // Letzten Datenpunkt immer anzeigen (wenn nicht bereits gezeichnet)
        int lastIndex = sorted.Length - 1;
        if (lastIndex % labelStep != 0 && lastIndex > 0)
        {
            float x = toX(sorted[lastIndex].Date.Ticks);
            string label = sorted[lastIndex].Date.ToString("d MMM");

            canvas.Save();
            canvas.Translate(x, chartRect.Bottom + 6f);
            canvas.RotateDegrees(-35);
            canvas.DrawText(label, 0, 10f, SKTextAlign.Right, labelFont, labelPaint);
            canvas.Restore();
        }
    }

    /// <summary>
    /// Rundet auf einen "schönen" Wert für Grid-Schritte.
    /// </summary>
    private static float RoundToNice(float value)
    {
        if (value <= 0) return 1f;
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

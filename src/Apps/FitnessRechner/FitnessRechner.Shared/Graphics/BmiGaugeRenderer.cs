using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace FitnessRechner.Graphics;

/// <summary>
/// BMI-Halbkreis-Gauge mit 4 Zonen (Untergewicht/Normal/Übergewicht/Adipositas).
/// Nutzt SkiaGauge-Pattern aber direkt gerendert für inline-Einbettung.
/// </summary>
public static class BmiGaugeRenderer
{
    private static readonly SKPaint _arcPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
    private static readonly SKPaint _needlePaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint _textPaint = new() { IsAntialias = true };
    private static readonly SKPaint _zoneFill = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Butt };

    // BMI-Zonen: Blau (Untergewicht), Grün (Normal), Gelb (Übergewicht), Rot (Adipositas)
    private static readonly SKColor _zoneUnderweight = new(0x3B, 0x82, 0xF6); // Blau
    private static readonly SKColor _zoneNormal = new(0x22, 0xC5, 0x5E); // Grün
    private static readonly SKColor _zoneOverweight = new(0xF5, 0x9E, 0x0B); // Gelb/Amber
    private static readonly SKColor _zoneObese = new(0xEF, 0x44, 0x44); // Rot

    /// <summary>
    /// Rendert BMI-Gauge als Halbkreis mit Zeiger.
    /// </summary>
    /// <param name="bmiValue">BMI-Wert (10-45 Range)</param>
    /// <param name="hasResult">Ergebnis vorhanden</param>
    public static void Render(SKCanvas canvas, SKRect bounds, float bmiValue, bool hasResult)
    {
        if (!hasResult || bmiValue <= 0) return;

        float w = bounds.Width;
        float h = bounds.Height;
        float cx = bounds.MidX;

        // Gauge nimmt unteren 2/3-Bereich ein
        float strokeW = Math.Max(12f, w * 0.04f);
        float radius = Math.Min(w * 0.4f, h * 0.55f);
        float cy = bounds.Top + h * 0.55f;

        var arcRect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        // Startwinkel 180° (links), Sweep 180° (Halbkreis)
        float startAngle = 180f;
        float totalSweep = 180f;

        // 4 BMI-Zonen zeichnen (10-18.5, 18.5-25, 25-30, 30-45)
        float minBmi = 10f;
        float maxBmi = 45f;
        float range = maxBmi - minBmi;

        var zones = new[]
        {
            (Start: 10f, End: 18.5f, Color: _zoneUnderweight),
            (Start: 18.5f, End: 25f, Color: _zoneNormal),
            (Start: 25f, End: 30f, Color: _zoneOverweight),
            (Start: 30f, End: 45f, Color: _zoneObese),
        };

        _zoneFill.StrokeWidth = strokeW;

        foreach (var zone in zones)
        {
            float zoneStart = (zone.Start - minBmi) / range * totalSweep + startAngle;
            float zoneSweep = (zone.End - zone.Start) / range * totalSweep;

            _zoneFill.Color = zone.Color;
            using var path = new SKPath();
            path.AddArc(arcRect, zoneStart, zoneSweep);
            canvas.DrawPath(path, _zoneFill);
        }

        // Track-Rahmen (dünner Rand über den Zonen)
        _arcPaint.StrokeWidth = strokeW + 2f;
        _arcPaint.Color = SkiaThemeHelper.WithAlpha(SkiaThemeHelper.TextMuted, 30);
        using var trackPath = new SKPath();
        trackPath.AddArc(arcRect, startAngle, totalSweep);
        canvas.DrawPath(trackPath, _arcPaint);

        // Zeiger
        float clampedBmi = Math.Clamp(bmiValue, minBmi, maxBmi);
        float needleAngle = (clampedBmi - minBmi) / range * totalSweep + startAngle;
        float needleAngleRad = needleAngle * MathF.PI / 180f;

        float needleLen = radius - strokeW * 0.5f - 4f;
        float needleX = cx + MathF.Cos(needleAngleRad) * needleLen;
        float needleY = cy + MathF.Sin(needleAngleRad) * needleLen;

        // Zeiger-Linie
        _needlePaint.Color = SkiaThemeHelper.TextPrimary;
        using var needlePath = new SKPath();
        needlePath.MoveTo(cx, cy);
        needlePath.LineTo(needleX, needleY);
        _arcPaint.StrokeWidth = 2.5f;
        _arcPaint.Color = SkiaThemeHelper.TextPrimary;
        canvas.DrawPath(needlePath, _arcPaint);

        // Zentraler Kreis (Zeiger-Basis)
        _needlePaint.Color = SkiaThemeHelper.TextPrimary;
        canvas.DrawCircle(cx, cy, 5f, _needlePaint);

        // Zeiger-Spitze (leuchtender Punkt)
        SKColor dotColor = bmiValue switch
        {
            < 18.5f => _zoneUnderweight,
            < 25f => _zoneNormal,
            < 30f => _zoneOverweight,
            _ => _zoneObese,
        };
        _needlePaint.Color = dotColor;
        canvas.DrawCircle(needleX, needleY, 6f, _needlePaint);

        // BMI-Wert Text unter dem Gauge
        _textPaint.Color = SkiaThemeHelper.TextPrimary;
        _textPaint.TextSize = Math.Max(18f, radius * 0.22f);
        _textPaint.TextAlign = SKTextAlign.Center;
        _textPaint.FakeBoldText = true;
        canvas.DrawText($"{bmiValue:F1}", cx, cy + radius * 0.35f, _textPaint);

        // Zonen-Labels (klein, unter dem Bogen)
        _textPaint.TextSize = Math.Max(8f, radius * 0.1f);
        _textPaint.FakeBoldText = false;
        _textPaint.Color = SkiaThemeHelper.TextMuted;

        float labelRadius = radius + strokeW * 0.5f + 10f;
        var labels = new[] { ("18.5", 18.5f), ("25", 25f), ("30", 30f) };
        foreach (var (text, val) in labels)
        {
            float angle = (val - minBmi) / range * totalSweep + startAngle;
            float rad = angle * MathF.PI / 180f;
            float lx = cx + MathF.Cos(rad) * labelRadius;
            float ly = cy + MathF.Sin(rad) * labelRadius;
            canvas.DrawText(text, lx, ly, _textPaint);
        }
    }
}

using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace ZeitManager.Graphics;

/// <summary>
/// Monats-Heatmap für Pomodoro-Sessions (GitHub-Contributions-Style).
/// 7x5 Grid, Farbe nach Focus-Intensität (0=grau → 4+=dunkelrot).
/// Wochentag-Labels links, Tages-Tooltip bei Tap.
/// </summary>
public static class PomodoroStatisticsVisualization
{
    // Heatmap-Farben (Intensitätsstufen)
    private static readonly SKColor Level0 = new(0x1E, 0x29, 0x3B); // Keine Sessions (Surface)
    private static readonly SKColor Level1 = new(0x7F, 0x1D, 0x1D); // 1 Session (dunkelrot)
    private static readonly SKColor Level2 = new(0xB9, 0x1C, 0x1C); // 2 Sessions
    private static readonly SKColor Level3 = new(0xDC, 0x26, 0x26); // 3 Sessions
    private static readonly SKColor Level4 = new(0xEF, 0x44, 0x44); // 4+ Sessions (hell rot)

    private static readonly SKPaint _cellPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint _borderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private static readonly SKPaint _textPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint _highlightPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f };
    private static readonly SKFont _labelFont = new() { Size = 10f };
    private static readonly SKFont _titleFont = new() { Size = 12f };
    private static readonly SKFont _legendFont = new() { Size = 8f };

    /// <summary>
    /// Daten für einen Tag in der Heatmap.
    /// </summary>
    public struct DayData
    {
        /// <summary>Anzahl Work-Sessions an diesem Tag.</summary>
        public int Sessions;

        /// <summary>Datum (für Label).</summary>
        public DateTime Date;

        /// <summary>Ob dieser Tag heute ist.</summary>
        public bool IsToday;
    }

    /// <summary>
    /// Rendert die Monats-Heatmap.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas</param>
    /// <param name="bounds">Zeichenbereich</param>
    /// <param name="days">28-35 Tage (4-5 Wochen), Montag=Index 0 der ersten Woche</param>
    /// <param name="weekDayLabels">7 Wochentag-Kürzel (Mo, Di, Mi, ...)</param>
    /// <param name="title">Titel (z.B. "Februar 2026")</param>
    public static void Render(SKCanvas canvas, SKRect bounds,
        DayData[] days, string[] weekDayLabels, string title)
    {
        if (days.Length == 0) return;

        float padding = 12f;
        float titleH = 24f;
        float legendH = 20f;
        float labelW = 28f; // Platz für Wochentag-Labels
        float gridLeft = bounds.Left + padding + labelW;
        float gridTop = bounds.Top + padding + titleH;
        float gridRight = bounds.Right - padding;
        float gridBottom = bounds.Bottom - padding - legendH;

        float gridW = gridRight - gridLeft;
        float gridH = gridBottom - gridTop;

        // Wochen und Tage berechnen
        int totalDays = days.Length;
        int weeks = (int)Math.Ceiling(totalDays / 7.0);
        if (weeks < 1) weeks = 1;

        float cellW = gridW / weeks;
        float cellH = gridH / 7f;
        float cellSize = Math.Min(cellW, cellH) - 2f; // Gap
        float cornerR = Math.Max(2f, cellSize * 0.2f);

        // Titel
        _textPaint.Color = SkiaThemeHelper.TextPrimary;
        _titleFont.Size = 12f;
        canvas.DrawText(title, bounds.Left + padding, bounds.Top + padding + _titleFont.Size,
            SKTextAlign.Left, _titleFont, _textPaint);

        // Wochentag-Labels (links)
        _textPaint.Color = SkiaThemeHelper.TextMuted;
        _labelFont.Size = Math.Min(10f, cellSize * 0.7f);
        if (weekDayLabels.Length >= 7)
        {
            for (int d = 0; d < 7; d++)
            {
                float y = gridTop + d * (cellSize + 2f) + cellSize / 2f + _labelFont.Size * 0.35f;
                canvas.DrawText(weekDayLabels[d], bounds.Left + padding + labelW - 4f, y,
                    SKTextAlign.Right, _labelFont, _textPaint);
            }
        }

        // Heatmap-Zellen zeichnen
        for (int i = 0; i < totalDays; i++)
        {
            int week = i / 7;
            int dayOfWeek = i % 7;

            float x = gridLeft + week * (cellSize + 2f);
            float y = gridTop + dayOfWeek * (cellSize + 2f);

            var cellRect = new SKRect(x, y, x + cellSize, y + cellSize);

            // Farbe nach Intensität
            var day = days[i];
            _cellPaint.Color = GetIntensityColor(day.Sessions);
            canvas.DrawRoundRect(cellRect, cornerR, cornerR, _cellPaint);

            // Heute: Leuchtender Rand
            if (day.IsToday)
            {
                _highlightPaint.Color = SkiaThemeHelper.TextPrimary;
                canvas.DrawRoundRect(cellRect, cornerR, cornerR, _highlightPaint);
            }
        }

        // Legende (unten: "Weniger" [...□□■■■] "Mehr")
        DrawLegend(canvas, gridLeft, gridBottom + 6f, cellSize);
    }

    /// <summary>
    /// Bestimmt die Heatmap-Farbe basierend auf der Session-Anzahl.
    /// </summary>
    private static SKColor GetIntensityColor(int sessions)
    {
        return sessions switch
        {
            0 => Level0,
            1 => Level1,
            2 => Level2,
            3 => Level3,
            _ => Level4 // 4+
        };
    }

    /// <summary>
    /// Zeichnet die Farb-Legende unterhalb der Heatmap.
    /// </summary>
    private static void DrawLegend(SKCanvas canvas, float startX, float y, float cellSize)
    {
        float legendCellSize = Math.Min(10f, cellSize * 0.6f);
        float gap = 3f;
        float cornerR = 2f;

        // "Weniger" Text
        _textPaint.Color = SkiaThemeHelper.TextMuted;
        _legendFont.Size = 8f;
        canvas.DrawText("0", startX, y + legendCellSize / 2f + 3f,
            SKTextAlign.Right, _legendFont, _textPaint);

        float x = startX + 4f;

        // 5 Farbstufen
        SKColor[] levels = { Level0, Level1, Level2, Level3, Level4 };
        foreach (var lvl in levels)
        {
            _cellPaint.Color = lvl;
            canvas.DrawRoundRect(new SKRect(x, y, x + legendCellSize, y + legendCellSize),
                cornerR, cornerR, _cellPaint);
            x += legendCellSize + gap;
        }

        // "Mehr" Text
        canvas.DrawText("4+", x + 2f, y + legendCellSize / 2f + 3f,
            SKTextAlign.Left, _legendFont, _textPaint);
    }

    /// <summary>
    /// Hit-Test: Gibt den Index des Tages zurück, der an Position (x, y) liegt.
    /// Gibt -1 zurück wenn kein Tag getroffen wurde.
    /// </summary>
    public static int HitTest(SKRect bounds, float hitX, float hitY,
        int totalDays, string[] weekDayLabels)
    {
        float padding = 12f;
        float titleH = 24f;
        float legendH = 20f;
        float labelW = 28f;
        float gridLeft = bounds.Left + padding + labelW;
        float gridTop = bounds.Top + padding + titleH;
        float gridRight = bounds.Right - padding;
        float gridBottom = bounds.Bottom - padding - legendH;

        float gridW = gridRight - gridLeft;
        float gridH = gridBottom - gridTop;

        int weeks = (int)Math.Ceiling(totalDays / 7.0);
        if (weeks < 1) return -1;

        float cellW = gridW / weeks;
        float cellH = gridH / 7f;
        float cellSize = Math.Min(cellW, cellH) - 2f;

        // Berechne welche Zelle getroffen wurde
        int week = (int)((hitX - gridLeft) / (cellSize + 2f));
        int dayOfWeek = (int)((hitY - gridTop) / (cellSize + 2f));

        if (week < 0 || week >= weeks || dayOfWeek < 0 || dayOfWeek >= 7)
            return -1;

        int index = week * 7 + dayOfWeek;
        if (index < 0 || index >= totalDays)
            return -1;

        return index;
    }
}

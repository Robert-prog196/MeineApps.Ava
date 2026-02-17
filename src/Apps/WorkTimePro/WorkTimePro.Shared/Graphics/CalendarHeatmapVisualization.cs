using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace WorkTimePro.Graphics;

/// <summary>
/// GitHub-Contribution-Style Heatmap für einen Kalendermonat.
/// 7x5/6 Grid mit Gradient-Färbung nach Arbeitsstunden,
/// Status-Symbole, Heute-Ring, Wochentag-Labels, Touch-HitTest.
/// </summary>
public static class CalendarHeatmapVisualization
{
    private static readonly SKPaint _cellFill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint _cellStroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
    private static readonly SKPaint _todayRing = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f };
    private static readonly SKPaint _textPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKPaint _iconPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private static readonly SKFont _dayFont = new() { Size = 12f };
    private static readonly SKFont _labelFont = new() { Size = 10f };
    private static readonly SKFont _headerFont = new() { Size = 11f };

    // Heatmap-Farben (0h → >8h)
    private static readonly SKColor _level0 = new(0x1E, 0x29, 0x3B); // Keine Arbeit (dunkelgrau-blau)
    private static readonly SKColor _level1 = new(0x16, 0x5D, 0x3A); // <4h (hellgrün-dunkel)
    private static readonly SKColor _level2 = new(0x1A, 0x80, 0x3E); // 4-6h (mittelgrün)
    private static readonly SKColor _level3 = new(0x22, 0xC5, 0x5E); // 6-8h (sattgrün)
    private static readonly SKColor _level4 = new(0x4A, 0xDE, 0x80); // >8h (hellgrün)

    // Status-Farben
    private static readonly SKColor _vacationColor = new(0x38, 0xBD, 0xF8); // Info/Blau
    private static readonly SKColor _sickColor = new(0xEF, 0x44, 0x44);     // Rot
    private static readonly SKColor _homeOfficeColor = new(0xA7, 0x8B, 0xFA);// Violett
    private static readonly SKColor _holidayColor = new(0xF5, 0x9E, 0x0B);  // Amber

    /// <summary>
    /// Daten für einen einzelnen Kalendertag.
    /// </summary>
    public struct DayData
    {
        /// <summary>Tag des Monats (1-31), 0 = leere Zelle.</summary>
        public int DayNumber;

        /// <summary>Arbeitsminuten (0-1440+).</summary>
        public int WorkMinutes;

        /// <summary>Status: 0=Normal, 1=Urlaub, 2=Krank, 3=HomeOffice, 4=Feiertag, 5=Wochenende.</summary>
        public int Status;

        /// <summary>Ob dieser Tag heute ist.</summary>
        public bool IsToday;

        /// <summary>Ob dieser Tag zum aktuellen Monat gehört.</summary>
        public bool IsCurrentMonth;
    }

    /// <summary>
    /// Rendert die Kalender-Heatmap.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas</param>
    /// <param name="bounds">Zeichenbereich</param>
    /// <param name="days">42 Tage (6 Wochen × 7 Tage), linksbündig Mo-So</param>
    /// <param name="weekdayLabels">7 Wochentags-Kürzel (Mo, Di, Mi, ...)</param>
    /// <param name="animTime">Animations-Zeit für pulsierenden Heute-Ring</param>
    public static void Render(SKCanvas canvas, SKRect bounds,
        DayData[] days, string[] weekdayLabels, float animTime = 0)
    {
        if (days.Length < 35) return;

        float labelWidth = 28f; // Platz für Wochentag-Labels links
        float headerH = 0f;    // Kein Header (wird extern gehandhabt)
        float gridLeft = bounds.Left + labelWidth;
        float gridTop = bounds.Top + headerH;
        float gridW = bounds.Width - labelWidth - 4f;
        float gridH = bounds.Height - headerH - 4f;

        int rows = days.Length >= 42 ? 6 : 5;
        float cellW = gridW / 7f;
        float cellH = gridH / rows;
        float cellSize = MathF.Min(cellW, cellH);
        float gap = 2f;

        // Wochentag-Labels (links)
        if (weekdayLabels.Length >= 7)
        {
            _textPaint.Color = SkiaThemeHelper.TextMuted;
            _labelFont.Size = 9f;
            for (int d = 0; d < 7; d++)
            {
                // Nur Mo, Mi, Fr anzeigen (spart Platz)
                if (d % 2 == 0)
                {
                    float y = gridTop + d * cellSize + cellSize / 2f + 3f;
                    canvas.DrawText(weekdayLabels[d], bounds.Left + labelWidth / 2f, y,
                        SKTextAlign.Center, _labelFont, _textPaint);
                }
            }
        }

        // Zellen zeichnen
        for (int i = 0; i < days.Length && i < rows * 7; i++)
        {
            int col = i % 7;
            int row = i / 7;

            float x = gridLeft + col * cellSize + gap / 2f;
            float y = gridTop + row * cellSize + gap / 2f;
            float size = cellSize - gap;

            var day = days[i];

            if (day.DayNumber <= 0)
            {
                // Leere Zelle (dezent)
                _cellFill.Color = SkiaThemeHelper.WithAlpha(SkiaThemeHelper.Surface, 20);
                canvas.DrawRoundRect(new SKRect(x, y, x + size, y + size), 4f, 4f, _cellFill);
                continue;
            }

            // Zellenfarbe bestimmen
            SKColor cellColor;
            if (!day.IsCurrentMonth)
            {
                cellColor = SkiaThemeHelper.WithAlpha(_level0, 40);
            }
            else if (day.Status == 5) // Wochenende
            {
                cellColor = SkiaThemeHelper.WithAlpha(_level0, 80);
            }
            else if (day.Status == 1) // Urlaub
            {
                cellColor = SkiaThemeHelper.WithAlpha(_vacationColor, 80);
            }
            else if (day.Status == 2) // Krank
            {
                cellColor = SkiaThemeHelper.WithAlpha(_sickColor, 80);
            }
            else if (day.Status == 3) // HomeOffice
            {
                cellColor = SkiaThemeHelper.WithAlpha(_homeOfficeColor, 80);
            }
            else if (day.Status == 4) // Feiertag
            {
                cellColor = SkiaThemeHelper.WithAlpha(_holidayColor, 80);
            }
            else
            {
                // Arbeitsminuten → Heatmap-Level
                cellColor = GetHeatColor(day.WorkMinutes);
            }

            // Zelle zeichnen
            _cellFill.Color = cellColor;
            var cellRect = new SKRect(x, y, x + size, y + size);
            canvas.DrawRoundRect(cellRect, 4f, 4f, _cellFill);

            // Tag-Nummer
            byte textAlpha = (byte)(day.IsCurrentMonth ? 220 : 80);
            _textPaint.Color = SKColors.White.WithAlpha(textAlpha);
            _dayFont.Size = size > 30 ? 12f : 10f;
            canvas.DrawText(day.DayNumber.ToString(), x + size / 2f, y + size / 2f + 4f,
                SKTextAlign.Center, _dayFont, _textPaint);

            // Status-Punkt (rechts unten, klein)
            if (day.IsCurrentMonth && day.Status >= 1 && day.Status <= 4)
            {
                SKColor dotColor = day.Status switch
                {
                    1 => _vacationColor,
                    2 => _sickColor,
                    3 => _homeOfficeColor,
                    4 => _holidayColor,
                    _ => SKColors.Transparent
                };
                _iconPaint.Color = dotColor;
                canvas.DrawCircle(x + size - 5f, y + size - 5f, 3f, _iconPaint);
            }

            // Heute: Pulsierender leuchtender Ring
            if (day.IsToday)
            {
                float pulse = 0.6f + 0.4f * MathF.Sin(animTime * 3f); // Pulsieren
                _todayRing.Color = SkiaThemeHelper.Accent.WithAlpha((byte)(pulse * 255));
                canvas.DrawRoundRect(cellRect, 4f, 4f, _todayRing);
            }
        }
    }

    /// <summary>
    /// HitTest: Gibt den Tagesindex (0-41) zurück, oder -1 wenn kein Treffer.
    /// </summary>
    public static int HitTest(SKRect bounds, float hitX, float hitY,
        int totalDays, string[]? weekdayLabels = null)
    {
        float labelWidth = 28f;
        float gridLeft = bounds.Left + labelWidth;
        float gridTop = bounds.Top;
        float gridW = bounds.Width - labelWidth - 4f;
        float gridH = bounds.Height - 4f;

        int rows = totalDays >= 42 ? 6 : 5;
        float cellW = gridW / 7f;
        float cellH = gridH / rows;
        float cellSize = MathF.Min(cellW, cellH);

        if (hitX < gridLeft || hitY < gridTop) return -1;

        int col = (int)((hitX - gridLeft) / cellSize);
        int row = (int)((hitY - gridTop) / cellSize);

        if (col < 0 || col >= 7 || row < 0 || row >= rows) return -1;

        int index = row * 7 + col;
        return index < totalDays ? index : -1;
    }

    /// <summary>
    /// Arbeitsminuten → Heatmap-Farbe (GitHub Contribution Style).
    /// </summary>
    private static SKColor GetHeatColor(int workMinutes)
    {
        if (workMinutes <= 0) return _level0;
        if (workMinutes < 240) return _level1;  // <4h
        if (workMinutes < 360) return _level2;  // 4-6h
        if (workMinutes < 480) return _level3;  // 6-8h
        return _level4;                          // >8h
    }
}

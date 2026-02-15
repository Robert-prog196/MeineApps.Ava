using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// Datenstruktur fuer eine einzelne Inspektions-Zelle (View -> Renderer).
/// </summary>
public struct InspectionCellData
{
    public string Icon;
    public bool IsDefect;
    public bool IsDefectFound;
    public bool IsFalseAlarm;
    public bool IsInspected;
    public float ContentOpacity;
    public SKColor BackgroundColor;
}

/// <summary>
/// SkiaSharp-Renderer fuer das Bauabnahme-Minigame (Inspection).
/// Baustellen-Optik: Betongrau mit Rissen, Kacheln mit Baustellen-Elementen,
/// Staub-Partikel, Lupe als Deko, Mangel-Hinweis per subtiles Schimmern.
/// Pixel-Art Stil: Flache Fuellungen, kein Anti-Aliasing, passend zu CityRenderer/SawingGameRenderer.
/// </summary>
public class InspectionGameRenderer
{
    // Animationszeit
    private float _time;

    // Hintergrund-Farben (Beton-Baustelle)
    private static readonly SKColor ConcreteBase = new(0x60, 0x7D, 0x8B);       // Betongrau
    private static readonly SKColor ConcreteDark = new(0x45, 0x5A, 0x64);       // Dunklerer Beton
    private static readonly SKColor CrackColor = new(0x37, 0x47, 0x4F, 100);    // Risse
    private static readonly SKColor GridLineColor = new(0x78, 0x90, 0x9C, 80);  // Grid-Linien

    // Zellen-Farben
    private static readonly SKColor CellNormal = new(0x37, 0x47, 0x4F);         // Uninspiziert
    private static readonly SKColor CellDefectFound = new(0x2E, 0x7D, 0x32);    // Gruener Hintergrund
    private static readonly SKColor CellFalseAlarm = new(0xC6, 0x28, 0x28);     // Roter Hintergrund
    private static readonly SKColor CellInspected = new(0x4E, 0x5B, 0x65);      // Schon inspiziert (kein Defekt)

    // Rahmenfarben
    private static readonly SKColor BorderNormal = new(0x78, 0x90, 0x9C);
    private static readonly SKColor BorderDefectFound = new(0x4C, 0xAF, 0x50);
    private static readonly SKColor BorderFalseAlarm = new(0xF4, 0x43, 0x36);

    // Akzent-Farben
    private static readonly SKColor CheckmarkGreen = new(0x66, 0xBB, 0x6A);
    private static readonly SKColor CrossRed = new(0xEF, 0x53, 0x50);
    private static readonly SKColor DefectShimmer = new(0xFF, 0x17, 0x44, 25);  // Subtil

    // Lupe-Farben
    private static readonly SKColor MagnifierRing = new(0xB0, 0xBE, 0xC5);
    private static readonly SKColor MagnifierGlass = new(0x42, 0xA5, 0xF5, 40);
    private static readonly SKColor MagnifierHandle = new(0x5D, 0x40, 0x37);

    // Staub-Partikel
    private readonly List<DustParticle> _dustParticles = [];
    private const int MaxDustParticles = 15;

    private struct DustParticle
    {
        public float X, Y, VelocityX, VelocityY, Life, MaxLife, Size;
        public byte Alpha;
    }

    /// <summary>
    /// Rendert das gesamte Inspektions-Spielfeld.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas zum Zeichnen.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="cells">Zell-Daten fuer das Grid.</param>
    /// <param name="cols">Spaltenanzahl.</param>
    /// <param name="rows">Zeilenanzahl.</param>
    /// <param name="isPlaying">Ob das Spiel laeuft.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame in Sekunden.</param>
    public void Render(SKCanvas canvas, SKRect bounds, InspectionCellData[] cells, int cols, int rows, bool isPlaying, float deltaTime)
    {
        _time += deltaTime;

        // Hintergrund: Beton mit Rissen
        DrawBackground(canvas, bounds);

        // Grid-Bereich berechnen (zentriert, mit Padding)
        float padding = 12;
        float availableWidth = bounds.Width - 2 * padding;
        float availableHeight = bounds.Height - 2 * padding;

        // Zellengroesse berechnen (quadratisch, passend zum Grid)
        float cellSize = Math.Min(availableWidth / cols, availableHeight / rows);
        float spacing = 3;
        float effectiveCellSize = cellSize - spacing;

        float gridWidth = cols * cellSize;
        float gridHeight = rows * cellSize;
        float gridLeft = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten statt vertikal zentrieren (bessere Platznutzung)
        float gridTop = bounds.Top + padding;

        // Grid-Linien
        DrawGridLines(canvas, gridLeft, gridTop, gridWidth, gridHeight, cols, rows, cellSize);

        // Zellen zeichnen
        if (cells != null)
        {
            for (int i = 0; i < cells.Length && i < cols * rows; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float cellX = gridLeft + col * cellSize + spacing / 2;
                float cellY = gridTop + row * cellSize + spacing / 2;
                DrawCell(canvas, cellX, cellY, effectiveCellSize, effectiveCellSize, cells[i], isPlaying);
            }
        }

        // Lupe/Inspektor-Deko (oben rechts)
        DrawMagnifier(canvas, bounds.Right - 44, bounds.Top + 12);

        // Staub-Partikel
        if (isPlaying)
        {
            UpdateAndDrawDust(canvas, bounds, deltaTime);
        }
    }

    /// <summary>
    /// HitTest: Gibt den Zell-Index zurueck oder -1 wenn kein Treffer.
    /// </summary>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="touchX">Touch X-Koordinate (in Skia-Pixeln).</param>
    /// <param name="touchY">Touch Y-Koordinate (in Skia-Pixeln).</param>
    /// <param name="cols">Spaltenanzahl.</param>
    /// <param name="rows">Zeilenanzahl.</param>
    /// <returns>Zell-Index (0..cols*rows-1) oder -1.</returns>
    public int HitTest(SKRect bounds, float touchX, float touchY, int cols, int rows)
    {
        float padding = 12;
        float availableWidth = bounds.Width - 2 * padding;
        float availableHeight = bounds.Height - 2 * padding;

        float cellSize = Math.Min(availableWidth / cols, availableHeight / rows);

        float gridWidth = cols * cellSize;
        float gridHeight = rows * cellSize;
        float gridLeft = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten (identisch mit Render)
        float gridTop = bounds.Top + padding;

        // Pruefen ob Touch im Grid-Bereich liegt
        if (touchX < gridLeft || touchX >= gridLeft + gridWidth ||
            touchY < gridTop || touchY >= gridTop + gridHeight)
        {
            return -1;
        }

        int col = (int)((touchX - gridLeft) / cellSize);
        int row = (int)((touchY - gridTop) / cellSize);

        col = Math.Clamp(col, 0, cols - 1);
        row = Math.Clamp(row, 0, rows - 1);

        return row * cols + col;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HINTERGRUND
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zeichnet Beton-Hintergrund mit Rissen und Texturen.
    /// </summary>
    private void DrawBackground(SKCanvas canvas, SKRect bounds)
    {
        // Grundfarbe: Betongrau
        using var bgPaint = new SKPaint { Color = ConcreteBase, IsAntialias = false };
        canvas.DrawRect(bounds, bgPaint);

        // Beton-Textur: Horizontale Streifen fuer Fugen
        using var stripePaint = new SKPaint { Color = ConcreteDark, IsAntialias = false, StrokeWidth = 1 };
        for (float y = bounds.Top + 30; y < bounds.Bottom; y += 40)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, stripePaint);
        }

        // Risse im Beton (deterministische Positionen)
        using var crackPaint = new SKPaint { Color = CrackColor, IsAntialias = false, StrokeWidth = 1 };

        // Riss 1: Oben links
        float cx1 = bounds.Left + bounds.Width * 0.15f;
        float cy1 = bounds.Top + bounds.Height * 0.2f;
        canvas.DrawLine(cx1, cy1, cx1 + 18, cy1 + 12, crackPaint);
        canvas.DrawLine(cx1 + 18, cy1 + 12, cx1 + 14, cy1 + 28, crackPaint);
        canvas.DrawLine(cx1 + 18, cy1 + 12, cx1 + 30, cy1 + 8, crackPaint);

        // Riss 2: Unten rechts
        float cx2 = bounds.Right - bounds.Width * 0.2f;
        float cy2 = bounds.Bottom - bounds.Height * 0.25f;
        canvas.DrawLine(cx2, cy2, cx2 - 10, cy2 + 16, crackPaint);
        canvas.DrawLine(cx2, cy2, cx2 + 12, cy2 + 10, crackPaint);

        // Riss 3: Mitte oben
        float cx3 = bounds.MidX + 20;
        float cy3 = bounds.Top + 10;
        canvas.DrawLine(cx3, cy3, cx3 + 8, cy3 + 14, crackPaint);
        canvas.DrawLine(cx3 + 8, cy3 + 14, cx3 + 20, cy3 + 18, crackPaint);

        // Kleine Beton-Kratzer
        using var scratchPaint = new SKPaint { Color = new SKColor(0x50, 0x60, 0x68, 60), IsAntialias = false, StrokeWidth = 1 };
        canvas.DrawLine(bounds.Left + 40, bounds.Bottom - 20, bounds.Left + 70, bounds.Bottom - 22, scratchPaint);
        canvas.DrawLine(bounds.Right - 60, bounds.Top + 50, bounds.Right - 30, bounds.Top + 48, scratchPaint);
    }

    /// <summary>
    /// Zeichnet subtile Hilfslinien fuer das Grid.
    /// </summary>
    private static void DrawGridLines(SKCanvas canvas, float gridLeft, float gridTop, float gridWidth, float gridHeight, int cols, int rows, float cellSize)
    {
        using var linePaint = new SKPaint { Color = GridLineColor, IsAntialias = false, StrokeWidth = 1 };

        // Vertikale Linien
        for (int c = 0; c <= cols; c++)
        {
            float x = gridLeft + c * cellSize;
            canvas.DrawLine(x, gridTop, x, gridTop + gridHeight, linePaint);
        }

        // Horizontale Linien
        for (int r = 0; r <= rows; r++)
        {
            float y = gridTop + r * cellSize;
            canvas.DrawLine(gridLeft, y, gridLeft + gridWidth, y, linePaint);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ZELLEN
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zeichnet eine einzelne Inspektions-Zelle mit Icon, Rahmen und Status-Overlay.
    /// </summary>
    private void DrawCell(SKCanvas canvas, float x, float y, float w, float h, InspectionCellData cell, bool isPlaying)
    {
        float cornerRadius = 4;

        // Hintergrundfarbe bestimmen
        SKColor bgColor;
        if (cell.IsDefectFound)
            bgColor = CellDefectFound;
        else if (cell.IsFalseAlarm)
            bgColor = CellFalseAlarm;
        else if (cell.IsInspected)
            bgColor = CellInspected;
        else
            bgColor = CellNormal;

        // Zellen-Hintergrund (abgerundet)
        using var bgPaint = new SKPaint { Color = bgColor, IsAntialias = false };
        var cellRect = new SKRect(x, y, x + w, y + h);
        canvas.DrawRoundRect(cellRect, cornerRadius, cornerRadius, bgPaint);

        // Rahmenfarbe bestimmen
        SKColor borderColor;
        if (cell.IsDefectFound)
            borderColor = BorderDefectFound;
        else if (cell.IsFalseAlarm)
            borderColor = BorderFalseAlarm;
        else
            borderColor = BorderNormal;

        // Rahmen zeichnen (2px bei inspiziert, 1px sonst)
        float borderWidth = cell.IsInspected ? 2.5f : 1;
        using var borderPaint = new SKPaint { Color = borderColor, IsAntialias = false, StrokeWidth = borderWidth, Style = SKPaintStyle.Stroke };
        canvas.DrawRoundRect(cellRect, cornerRadius, cornerRadius, borderPaint);

        // Subtiles Mangel-Schimmern (nur fuer unentdeckte Defekte, leicht pulsierend)
        if (cell.IsDefect && !cell.IsInspected && isPlaying)
        {
            float shimmerPulse = (float)(0.3 + 0.7 * Math.Sin(_time * 2.5 + x * 0.1));
            byte shimmerAlpha = (byte)(DefectShimmer.Alpha * shimmerPulse);
            using var shimmerPaint = new SKPaint { Color = DefectShimmer.WithAlpha(shimmerAlpha), IsAntialias = false };
            canvas.DrawRoundRect(cellRect, cornerRadius, cornerRadius, shimmerPaint);
        }

        // Icon zeichnen (zentriert in der Zelle)
        if (!string.IsNullOrEmpty(cell.Icon))
        {
            float fontSize = Math.Min(w, h) * 0.5f;
            using var iconFont = new SKFont(SKTypeface.Default, fontSize);
            using var iconPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha((byte)(255 * cell.ContentOpacity)),
                IsAntialias = true // Text braucht Antialiasing fuer Lesbarkeit
            };

            // Textbreite messen fuer Zentrierung
            float textWidth = iconFont.MeasureText(cell.Icon, iconPaint);
            float textX = x + (w - textWidth) / 2;
            float textY = y + h / 2 + fontSize * 0.35f; // Vertikale Zentrierung

            canvas.DrawText(cell.Icon, textX, textY, SKTextAlign.Left, iconFont, iconPaint);
        }

        // Haekchen oben rechts bei gefundenem Defekt
        if (cell.IsDefectFound)
        {
            DrawCheckmark(canvas, x + w - 14, y + 2, 12);
        }

        // X-Markierung oben rechts bei Fehlalarm
        if (cell.IsFalseAlarm)
        {
            DrawCrossMark(canvas, x + w - 14, y + 2, 12);
        }
    }

    /// <summary>
    /// Zeichnet ein Pixel-Art Haekchen (gruener Kreis mit weissem Check).
    /// </summary>
    private static void DrawCheckmark(SKCanvas canvas, float x, float y, float size)
    {
        // Gruener Kreis-Hintergrund
        float centerX = x + size / 2;
        float centerY = y + size / 2;
        float radius = size / 2;
        using var circlePaint = new SKPaint { Color = CheckmarkGreen, IsAntialias = false };
        canvas.DrawCircle(centerX, centerY, radius, circlePaint);

        // Weisses Haekchen (2 Linien: kurz links, lang rechts)
        using var checkPaint = new SKPaint { Color = SKColors.White, IsAntialias = false, StrokeWidth = 2, StrokeCap = SKStrokeCap.Square };
        float s = size * 0.25f; // Skalierungsfaktor
        canvas.DrawLine(centerX - s * 1.2f, centerY, centerX - s * 0.2f, centerY + s, checkPaint);
        canvas.DrawLine(centerX - s * 0.2f, centerY + s, centerX + s * 1.5f, centerY - s * 0.8f, checkPaint);
    }

    /// <summary>
    /// Zeichnet eine Pixel-Art X-Markierung (roter Kreis mit weissem X).
    /// </summary>
    private static void DrawCrossMark(SKCanvas canvas, float x, float y, float size)
    {
        // Roter Kreis-Hintergrund
        float centerX = x + size / 2;
        float centerY = y + size / 2;
        float radius = size / 2;
        using var circlePaint = new SKPaint { Color = CrossRed, IsAntialias = false };
        canvas.DrawCircle(centerX, centerY, radius, circlePaint);

        // Weisses X (2 diagonale Linien)
        using var xPaint = new SKPaint { Color = SKColors.White, IsAntialias = false, StrokeWidth = 2, StrokeCap = SKStrokeCap.Square };
        float offset = size * 0.25f;
        canvas.DrawLine(centerX - offset, centerY - offset, centerX + offset, centerY + offset, xPaint);
        canvas.DrawLine(centerX + offset, centerY - offset, centerX - offset, centerY + offset, xPaint);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DEKO: LUPE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zeichnet eine kleine Lupe/Inspektor-Symbol als Deko-Element.
    /// Leichtes Schwanken waehrend das Spiel laeuft.
    /// </summary>
    private void DrawMagnifier(SKCanvas canvas, float x, float y)
    {
        // Leichte Schwankbewegung
        float bobY = (float)Math.Sin(_time * 1.5) * 2;

        float cx = x + 12;
        float cy = y + 12 + bobY;
        float radius = 10;

        // Glasflaeche (halbtransparent blau)
        using var glassPaint = new SKPaint { Color = MagnifierGlass, IsAntialias = false };
        canvas.DrawCircle(cx, cy, radius, glassPaint);

        // Metallring
        using var ringPaint = new SKPaint { Color = MagnifierRing, IsAntialias = false, StrokeWidth = 3, Style = SKPaintStyle.Stroke };
        canvas.DrawCircle(cx, cy, radius, ringPaint);

        // Glanz auf dem Glas (kleiner heller Punkt)
        using var glintPaint = new SKPaint { Color = new SKColor(255, 255, 255, 80), IsAntialias = false };
        canvas.DrawCircle(cx - 3, cy - 3, 3, glintPaint);

        // Griff (diagonal nach unten rechts)
        using var handlePaint = new SKPaint { Color = MagnifierHandle, IsAntialias = false, StrokeWidth = 4, StrokeCap = SKStrokeCap.Round };
        float handleStartX = cx + radius * 0.6f;
        float handleStartY = cy + radius * 0.6f;
        canvas.DrawLine(handleStartX, handleStartY, handleStartX + 10, handleStartY + 10, handlePaint);

        // Griff-Akzent (hellere Kante)
        using var handleAccent = new SKPaint { Color = new SKColor(0x8D, 0x6E, 0x63), IsAntialias = false, StrokeWidth = 2, StrokeCap = SKStrokeCap.Round };
        canvas.DrawLine(handleStartX + 1, handleStartY, handleStartX + 9, handleStartY + 8, handleAccent);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PARTIKEL: STAUB
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aktualisiert und zeichnet Staub-Partikel die ueber die Baustelle schweben.
    /// </summary>
    private void UpdateAndDrawDust(SKCanvas canvas, SKRect bounds, float deltaTime)
    {
        var random = Random.Shared;

        // Neue Partikel erzeugen
        if (_dustParticles.Count < MaxDustParticles)
        {
            _dustParticles.Add(new DustParticle
            {
                X = bounds.Left + (float)(random.NextDouble() * bounds.Width),
                Y = bounds.Bottom + 5,
                VelocityX = (float)(random.NextDouble() - 0.5) * 15,
                VelocityY = -10 - (float)(random.NextDouble() * 20),
                Life = 0,
                MaxLife = 2.0f + (float)random.NextDouble() * 2.0f,
                Size = 1 + random.Next(0, 3),
                Alpha = (byte)(60 + random.Next(0, 60))
            });
        }

        // Partikel aktualisieren und zeichnen
        using var dustPaint = new SKPaint { IsAntialias = false };
        for (int i = _dustParticles.Count - 1; i >= 0; i--)
        {
            var p = _dustParticles[i];
            p.Life += deltaTime;
            p.X += p.VelocityX * deltaTime;
            p.Y += p.VelocityY * deltaTime;

            // Leichte horizontale Drift (Wind-Effekt)
            p.VelocityX += (float)(Math.Sin(_time * 0.5 + i) * 2) * deltaTime;

            if (p.Life >= p.MaxLife || p.Y < bounds.Top - 10)
            {
                _dustParticles.RemoveAt(i);
                continue;
            }

            _dustParticles[i] = p;

            // Alpha basierend auf Lebenszeit (Fade-Out)
            float lifeRatio = p.Life / p.MaxLife;
            float alpha = lifeRatio < 0.2f
                ? lifeRatio / 0.2f   // Fade-In
                : 1.0f - (lifeRatio - 0.2f) / 0.8f; // Fade-Out
            byte finalAlpha = (byte)(p.Alpha * alpha);

            dustPaint.Color = new SKColor(0xB0, 0xBE, 0xC5, finalAlpha);
            canvas.DrawRect(p.X, p.Y, p.Size, p.Size, dustPaint);
        }
    }
}

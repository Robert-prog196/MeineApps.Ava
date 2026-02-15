using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Streich-Minigame.
/// Zeichnet eine Putzwand mit Zellen die gestrichen werden muessen.
/// Pixel-Art Stil: Flache Fuellungen, kein Anti-Aliasing, passend zu CityRenderer/SawingGameRenderer.
/// </summary>
public class PaintingGameRenderer
{
    // Wand-Farben
    private static readonly SKColor WallBg = new(0xE0, 0xD5, 0xC0);          // Helle Putzwand
    private static readonly SKColor WallLine = new(0xC8, 0xBC, 0xA0);        // Wandfugen
    private static readonly SKColor CellNormal = new(0xD8, 0xCC, 0xB0);      // Normale Zelle (Putz)
    private static readonly SKColor CellTarget = new(0xD0, 0xC8, 0xAA);      // Zielzelle (leicht heller)
    private static readonly SKColor CellBorder = new(0xB0, 0xA4, 0x88, 80);  // Zell-Rand

    // Feedback-Farben
    private static readonly SKColor ErrorFlash = new(0xEF, 0x44, 0x44, 120); // Fehler-Rot
    private static readonly SKColor ErrorCross = new(0xEF, 0x44, 0x44);      // X-Markierung
    private static readonly SKColor CheckColor = new(0xFF, 0xFF, 0xFF, 200);  // Haekchen

    // Farbroller-Spritzer
    private readonly List<PaintSplatter> _splatters = [];
    private float _animTime;

    private struct PaintSplatter
    {
        public float X, Y, Size, Life, MaxLife;
        public SKColor Color;
    }

    /// <summary>
    /// Rendert das Streich-Spielfeld.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas zum Zeichnen.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="cells">Zell-Daten (IsTarget, IsPainted, IsCorrect, HasError).</param>
    /// <param name="gridSize">Grid-Groesse (quadratisch: gridSize x gridSize).</param>
    /// <param name="paintColor">Aktive Streichfarbe.</param>
    /// <param name="isPlaying">Ob das Spiel laeuft.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame in Sekunden.</param>
    public void Render(SKCanvas canvas, SKRect bounds, PaintCellData[] cells, int gridSize,
        SKColor paintColor, bool isPlaying, float deltaTime)
    {
        _animTime += deltaTime;

        // Tile-Groesse berechnen (quadratisches Grid)
        // Padding 12 pro Seite wegen CornerRadius=8 am Border
        float padding = 12;
        float maxTileSize = Math.Min(
            (bounds.Width - padding * 2) / gridSize,
            (bounds.Height - padding * 2) / gridSize);
        float tileSize = maxTileSize;

        float gridWidth = gridSize * tileSize;
        float gridHeight = gridSize * tileSize;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten statt vertikal zentrieren
        float startY = bounds.Top + padding;

        // 1. Wand-Hintergrund
        DrawWallBackground(canvas, bounds);

        // 2. Zellen zeichnen
        for (int i = 0; i < cells.Length && i < gridSize * gridSize; i++)
        {
            int col = i % gridSize;
            int row = i / gridSize;
            float cx = startX + col * tileSize;
            float cy = startY + row * tileSize;

            DrawCell(canvas, cx, cy, tileSize, cells[i], paintColor);
        }

        // 3. Farbroller-Spritzer ueber den Zellen
        UpdateAndDrawSplatters(canvas, deltaTime);
    }

    /// <summary>
    /// Berechnet welche Zelle bei Touch-Koordinaten getroffen wurde.
    /// Gibt -1 zurueck wenn kein Treffer.
    /// </summary>
    public int HitTest(SKRect bounds, float touchX, float touchY, int gridSize)
    {
        float padding = 12;
        float maxTileSize = Math.Min(
            (bounds.Width - padding * 2) / gridSize,
            (bounds.Height - padding * 2) / gridSize);
        float tileSize = maxTileSize;

        float gridWidth = gridSize * tileSize;
        float gridHeight = gridSize * tileSize;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten (identisch mit Render)
        float startY = bounds.Top + padding;

        int col = (int)((touchX - startX) / tileSize);
        int row = (int)((touchY - startY) / tileSize);

        if (col < 0 || col >= gridSize || row < 0 || row >= gridSize) return -1;
        return row * gridSize + col;
    }

    /// <summary>
    /// Fuegt Farbspritzer hinzu (bei erfolgreichem Streichen).
    /// </summary>
    public void AddSplatter(SKRect bounds, int cellIndex, int gridSize, SKColor color)
    {
        float padding = 12;
        float maxTileSize = Math.Min((bounds.Width - padding * 2) / gridSize, (bounds.Height - padding * 2) / gridSize);
        float tileSize = maxTileSize;
        float gridWidth = gridSize * tileSize;
        float gridHeight = gridSize * tileSize;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten (identisch mit Render)
        float startY = bounds.Top + padding;

        int col = cellIndex % gridSize;
        int row = cellIndex / gridSize;
        float cx = startX + col * tileSize + tileSize / 2;
        float cy = startY + row * tileSize + tileSize / 2;

        var random = Random.Shared;
        for (int i = 0; i < 5; i++)
        {
            _splatters.Add(new PaintSplatter
            {
                X = cx + random.Next(-15, 16),
                Y = cy + random.Next(-15, 16),
                Size = 2 + random.Next(0, 5),
                Life = 0,
                MaxLife = 0.6f + (float)random.NextDouble() * 0.4f,
                Color = color
            });
        }
    }

    /// <summary>
    /// Zeichnet den Putzwand-Hintergrund mit subtilen Fugenlinien.
    /// </summary>
    private void DrawWallBackground(SKCanvas canvas, SKRect bounds)
    {
        // Putzwand-Flaeche
        using var wallPaint = new SKPaint { Color = WallBg, IsAntialias = false };
        canvas.DrawRect(bounds, wallPaint);

        // Wandstruktur (subtile horizontale Linien als Putzstruktur)
        using var linePaint = new SKPaint { Color = WallLine, IsAntialias = false, StrokeWidth = 1 };
        for (float y = bounds.Top + 18; y < bounds.Bottom; y += 18)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
        }
    }

    /// <summary>
    /// Zeichnet eine einzelne Zelle (normal, Ziel, gestrichen oder Fehler).
    /// </summary>
    private void DrawCell(SKCanvas canvas, float x, float y, float size, PaintCellData cell, SKColor paintColor)
    {
        float margin = 2;
        float innerX = x + margin;
        float innerY = y + margin;
        float innerSize = size - margin * 2;

        if (cell.IsPainted)
        {
            // Gestrichen: Farbflaeche mit Pinselstrich-Textur
            using var paintedPaint = new SKPaint { Color = paintColor, IsAntialias = false };
            canvas.DrawRect(innerX, innerY, innerSize, innerSize, paintedPaint);

            // Pinselstrich-Textur (horizontale Streifen fuer realistischen Look)
            using var strokePaint = new SKPaint { Color = paintColor.WithAlpha(180), IsAntialias = false };
            for (float sy = innerY + 3; sy < innerY + innerSize; sy += 6)
            {
                canvas.DrawRect(innerX + 1, sy, innerSize - 2, 2, strokePaint);
            }

            // Korrekt-Markierung (Haekchen wenn Zielzelle korrekt gestrichen)
            if (cell.IsCorrect)
            {
                using var checkPaint = new SKPaint
                {
                    Color = CheckColor,
                    IsAntialias = false,
                    StrokeWidth = 2,
                    Style = SKPaintStyle.Stroke
                };
                float cx = innerX + innerSize / 2;
                float cy = innerY + innerSize / 2;
                // Pixel-Art Haekchen
                canvas.DrawLine(cx - 6, cy, cx - 2, cy + 5, checkPaint);
                canvas.DrawLine(cx - 2, cy + 5, cx + 6, cy - 5, checkPaint);
            }
        }
        else if (cell.IsTarget)
        {
            // Zielzelle: Leicht heller mit pulsierendem Markierungspunkt
            using var targetPaint = new SKPaint { Color = CellTarget, IsAntialias = false };
            canvas.DrawRect(innerX, innerY, innerSize, innerSize, targetPaint);

            // Pulsierender Markierungspunkt in der Mitte
            float pulse = (float)(0.4 + 0.2 * Math.Sin(_animTime * 2 + x * 0.1f));
            using var markPaint = new SKPaint
            {
                Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(pulse * 255)),
                IsAntialias = false
            };
            canvas.DrawRect(innerX + innerSize / 2 - 3, innerY + innerSize / 2 - 3, 6, 6, markPaint);

            // Gestrichelter Rand als Hinweis (animiert)
            using var dashPaint = new SKPaint
            {
                Color = paintColor.WithAlpha(80),
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                PathEffect = SKPathEffect.CreateDash([4, 4], _animTime * 10)
            };
            canvas.DrawRect(innerX, innerY, innerSize, innerSize, dashPaint);
        }
        else
        {
            // Normale Zelle (Putzfarbe)
            using var normalPaint = new SKPaint { Color = CellNormal, IsAntialias = false };
            canvas.DrawRect(innerX, innerY, innerSize, innerSize, normalPaint);
        }

        // Fehler-Flash (rotes Overlay mit X-Markierung)
        if (cell.HasError)
        {
            using var errorPaint = new SKPaint { Color = ErrorFlash, IsAntialias = false };
            canvas.DrawRect(innerX, innerY, innerSize, innerSize, errorPaint);

            // X-Markierung
            using var xPaint = new SKPaint { Color = ErrorCross, IsAntialias = false, StrokeWidth = 3 };
            canvas.DrawLine(innerX + 8, innerY + 8, innerX + innerSize - 8, innerY + innerSize - 8, xPaint);
            canvas.DrawLine(innerX + innerSize - 8, innerY + 8, innerX + 8, innerY + innerSize - 8, xPaint);
        }

        // Zell-Rand (subtil)
        using var borderPaint = new SKPaint
        {
            Color = CellBorder,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        canvas.DrawRect(innerX, innerY, innerSize, innerSize, borderPaint);
    }

    /// <summary>
    /// Aktualisiert und zeichnet Farbspritzer-Partikel (entstehen bei erfolgreichem Streichen).
    /// </summary>
    private void UpdateAndDrawSplatters(SKCanvas canvas, float deltaTime)
    {
        using var splatPaint = new SKPaint { IsAntialias = false };

        for (int i = _splatters.Count - 1; i >= 0; i--)
        {
            var s = _splatters[i];
            s.Life += deltaTime;

            if (s.Life >= s.MaxLife)
            {
                _splatters.RemoveAt(i);
                continue;
            }
            _splatters[i] = s;

            // Alpha verblasst ueber Lebensdauer
            float alpha = 1 - (s.Life / s.MaxLife);
            splatPaint.Color = s.Color.WithAlpha((byte)(alpha * 180));
            canvas.DrawCircle(s.X, s.Y, s.Size, splatPaint);
        }
    }
}

/// <summary>
/// Vereinfachte Zell-Daten fuer den Renderer.
/// Wird aus dem ViewModel (PaintCell) befuellt.
/// </summary>
public struct PaintCellData
{
    public bool IsTarget;
    public bool IsPainted;
    public bool IsCorrect;
    public bool HasError;
}

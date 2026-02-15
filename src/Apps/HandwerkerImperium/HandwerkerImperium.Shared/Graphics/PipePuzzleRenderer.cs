using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Rohrleitungs-Puzzle.
/// Zeichnet Metall-Rohre auf Beton-Kacheln mit Wasser-Animation.
/// Pixel-Art Stil: Keine Gradienten, kein Anti-Aliasing, flache Fuellungen.
/// </summary>
public class PipePuzzleRenderer
{
    // Kachel-Farben (Betonboden-Optik)
    private static readonly SKColor TileBg = new(0x37, 0x47, 0x4F);
    private static readonly SKColor TileBorder = new(0x26, 0x32, 0x38);
    private static readonly SKColor TileHighlight = new(0x45, 0x55, 0x5E);

    // Rohr-Metall-Farben
    private static readonly SKColor PipeColor = new(0x78, 0x90, 0x9C);
    private static readonly SKColor PipeHighlight = new(0x90, 0xA4, 0xAE);
    private static readonly SKColor PipeShadow = new(0x54, 0x6E, 0x7A);
    private static readonly SKColor PipeDark = new(0x45, 0x5A, 0x64);

    // Wasser-Farben
    private static readonly SKColor WaterColor = new(0x29, 0xB6, 0xF6);
    private static readonly SKColor WaterDark = new(0x03, 0x9B, 0xE5);
    private static readonly SKColor WaterLight = new(0x4F, 0xC3, 0xF7);

    // Spezial-Indikator-Farben
    private static readonly SKColor SourceColor = new(0x4C, 0xAF, 0x50);
    private static readonly SKColor SourceDark = new(0x38, 0x8E, 0x3C);
    private static readonly SKColor DrainColor = new(0x42, 0xA5, 0xF5);
    private static readonly SKColor DrainDark = new(0x1E, 0x88, 0xE5);
    private static readonly SKColor LockColor = new(0xFF, 0xC1, 0x07);

    // Hintergrund
    private static readonly SKColor BackgroundColor = new(0x1A, 0x23, 0x27);

    private float _waterAnimTime;

    // Rohr-Typen:
    // 0 = Straight (Links-Rechts bei Rotation 0)
    // 1 = Corner (Rechts-Unten bei Rotation 0)
    // 2 = TJunction (Rechts-Unten-Links bei Rotation 0)
    // 3 = Cross (Alle 4 Richtungen)

    /// <summary>
    /// Rendert das gesamte Puzzle-Grid.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="tiles">Array mit Tile-Daten.</param>
    /// <param name="cols">Anzahl Spalten im Grid.</param>
    /// <param name="rows">Anzahl Zeilen im Grid.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame (Sekunden).</param>
    public void Render(SKCanvas canvas, SKRect bounds, PipeTileData[] tiles, int cols, int rows, float deltaTime)
    {
        _waterAnimTime += deltaTime;

        // Padding 12 wegen CornerRadius=8 am Border (Ecken-Clipping vermeiden)
        float padding = 12;
        float tileSize = Math.Min(
            (bounds.Width - padding * 2) / cols,
            (bounds.Height - padding * 2) / rows);

        float gridWidth = cols * tileSize;
        float gridHeight = rows * tileSize;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten statt vertikal zentrieren (vermeidet leeren Raum unten)
        float startY = bounds.Top + padding;

        // Hintergrund
        using var bgPaint = new SKPaint { Color = BackgroundColor, IsAntialias = false };
        canvas.DrawRect(bounds, bgPaint);

        // Grid-Schatten (Tiefeneffekt)
        using var gridShadowPaint = new SKPaint { Color = new SKColor(0x00, 0x00, 0x00, 60), IsAntialias = false };
        canvas.DrawRect(startX + 3, startY + 3, gridWidth, gridHeight, gridShadowPaint);

        // Grid-Kacheln zeichnen
        for (int i = 0; i < tiles.Length && i < cols * rows; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float tx = startX + col * tileSize;
            float ty = startY + row * tileSize;

            DrawTile(canvas, tx, ty, tileSize, tiles[i]);
        }
    }

    /// <summary>
    /// Berechnet welche Kachel bei gegebenen Koordinaten getroffen wurde.
    /// Gibt den Index zurueck oder -1 wenn keine Kachel getroffen.
    /// </summary>
    public int HitTest(SKRect bounds, float touchX, float touchY, int cols, int rows)
    {
        float padding = 12;
        float tileSize = Math.Min(
            (bounds.Width - padding * 2) / cols,
            (bounds.Height - padding * 2) / rows);

        float gridWidth = cols * tileSize;
        float gridHeight = rows * tileSize;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten (identisch mit Render)
        float startY = bounds.Top + padding;

        // Pruefung ob innerhalb des Grids
        if (touchX < startX || touchX >= startX + gridWidth)
            return -1;
        if (touchY < startY || touchY >= startY + gridHeight)
            return -1;

        int col = (int)((touchX - startX) / tileSize);
        int row = (int)((touchY - startY) / tileSize);

        if (col < 0 || col >= cols || row < 0 || row >= rows)
            return -1;

        return row * cols + col;
    }

    /// <summary>
    /// Zeichnet eine einzelne Kachel mit Rohr, Indikatoren und Wasser-Effekt.
    /// </summary>
    private void DrawTile(SKCanvas canvas, float x, float y, float size, PipeTileData tile)
    {
        float margin = 2;
        float innerSize = size - margin * 2;
        float innerX = x + margin;
        float innerY = y + margin;

        // Kachel-Hintergrund (Betonboden)
        using var tilePaint = new SKPaint { Color = TileBg, IsAntialias = false };
        canvas.DrawRect(innerX, innerY, innerSize, innerSize, tilePaint);

        // Leichter Highlight-Streifen oben (3D-Effekt)
        using var highlightPaint = new SKPaint { Color = TileHighlight, IsAntialias = false };
        canvas.DrawRect(innerX, innerY, innerSize, 2, highlightPaint);

        // Kachel-Rand
        using var borderPaint = new SKPaint
        {
            Color = tile.IsConnected ? WaterDark.WithAlpha(120) : TileBorder,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        canvas.DrawRect(innerX, innerY, innerSize, innerSize, borderPaint);

        // Rohre zeichnen basierend auf Typ + Rotation
        var openings = GetOpenings(tile.PipeType, tile.Rotation);
        float center = innerSize / 2;
        float pipeWidth = innerSize * 0.28f;
        float halfPipe = pipeWidth / 2;

        // Rohr-Segmente von Kante zur Mitte
        foreach (var dir in openings)
        {
            DrawPipeSegment(canvas, innerX, innerY, innerSize, center, halfPipe, dir, tile.IsConnected);
        }

        // Rohr-Kreuzung in der Mitte (Verbindungsstueck)
        if (openings.Length >= 2)
        {
            DrawPipeCenter(canvas, innerX, innerY, center, halfPipe, tile.IsConnected, openings);
        }

        // Quell-Indikator (Wasserpumpe)
        if (tile.IsSource)
        {
            DrawSourceIndicator(canvas, innerX, innerY, innerSize, center);
        }

        // Abfluss-Indikator (Ziel)
        if (tile.IsDrain)
        {
            DrawDrainIndicator(canvas, innerX, innerY, innerSize, center);
        }

        // Schloss-Indikator fuer gesperrte Kacheln (Quelle/Abfluss)
        if (tile.IsLocked && !tile.IsSource && !tile.IsDrain)
        {
            DrawLockIndicator(canvas, innerX, innerY, innerSize);
        }

        // Wasser-Puls-Animation bei verbundenen Rohren
        if (tile.IsConnected)
        {
            DrawWaterPulse(canvas, innerX, innerY, innerSize, x);
        }
    }

    /// <summary>
    /// Zeichnet ein Rohrsegment von der Kachelmitte zum Rand in gegebener Richtung.
    /// Metall-Optik mit Schatten und Highlight.
    /// </summary>
    private void DrawPipeSegment(SKCanvas canvas, float tileX, float tileY, float tileSize,
        float center, float halfPipe, int direction, bool connected)
    {
        var mainColor = connected ? WaterColor : PipeColor;
        var shadowColor = connected ? WaterDark : PipeShadow;
        var lightColor = connected ? WaterLight : PipeHighlight;

        using var pipePaint = new SKPaint { Color = mainColor, IsAntialias = false };
        using var shadowPaint = new SKPaint { Color = shadowColor, IsAntialias = false };
        using var lightPaint = new SKPaint { Color = lightColor.WithAlpha(100), IsAntialias = false };

        // Richtung: 0=Oben, 1=Unten, 2=Links, 3=Rechts
        switch (direction)
        {
            case 0: // Oben: Rohr von Mitte nach oben
                canvas.DrawRect(tileX + center - halfPipe, tileY, halfPipe * 2, center + halfPipe, pipePaint);
                // Schatten links
                canvas.DrawRect(tileX + center - halfPipe, tileY, 2, center + halfPipe, shadowPaint);
                // Highlight rechts
                canvas.DrawRect(tileX + center + halfPipe - 2, tileY, 2, center + halfPipe, lightPaint);
                // Flansch am Rand (Rohrende)
                canvas.DrawRect(tileX + center - halfPipe - 1, tileY, halfPipe * 2 + 2, 3, shadowPaint);
                break;

            case 1: // Unten: Rohr von Mitte nach unten
                canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, halfPipe * 2, center + halfPipe, pipePaint);
                canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, 2, center + halfPipe, shadowPaint);
                canvas.DrawRect(tileX + center + halfPipe - 2, tileY + center - halfPipe, 2, center + halfPipe, lightPaint);
                // Flansch
                canvas.DrawRect(tileX + center - halfPipe - 1, tileY + tileSize - 3, halfPipe * 2 + 2, 3, shadowPaint);
                break;

            case 2: // Links: Rohr von Mitte nach links
                canvas.DrawRect(tileX, tileY + center - halfPipe, center + halfPipe, halfPipe * 2, pipePaint);
                // Schatten oben
                canvas.DrawRect(tileX, tileY + center - halfPipe, center + halfPipe, 2, shadowPaint);
                // Highlight unten
                canvas.DrawRect(tileX, tileY + center + halfPipe - 2, center + halfPipe, 2, lightPaint);
                // Flansch
                canvas.DrawRect(tileX, tileY + center - halfPipe - 1, 3, halfPipe * 2 + 2, shadowPaint);
                break;

            case 3: // Rechts: Rohr von Mitte nach rechts
                canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, center + halfPipe, halfPipe * 2, pipePaint);
                canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, center + halfPipe, 2, shadowPaint);
                canvas.DrawRect(tileX + center - halfPipe, tileY + center + halfPipe - 2, center + halfPipe, 2, lightPaint);
                // Flansch
                canvas.DrawRect(tileX + tileSize - 3, tileY + center - halfPipe - 1, 3, halfPipe * 2 + 2, shadowPaint);
                break;
        }
    }

    /// <summary>
    /// Zeichnet das Verbindungsstueck in der Kachelmitte.
    /// Bei Ecken wird ein L-foermiges Verbindungsstueck gezeichnet.
    /// </summary>
    private void DrawPipeCenter(SKCanvas canvas, float tileX, float tileY, float center, float halfPipe,
        bool connected, int[] openings)
    {
        var mainColor = connected ? WaterColor : PipeColor;
        var shadowColor = connected ? WaterDark : PipeShadow;
        var lightColor = connected ? WaterLight : PipeHighlight;

        using var centerPaint = new SKPaint { Color = mainColor, IsAntialias = false };
        canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, halfPipe * 2, halfPipe * 2, centerPaint);

        // Niet-Punkte in der Mitte (Metall-Detail)
        using var nietPaint = new SKPaint { Color = shadowColor, IsAntialias = false };
        float nietSize = Math.Max(2, halfPipe * 0.35f);
        canvas.DrawRect(tileX + center - nietSize / 2, tileY + center - nietSize / 2, nietSize, nietSize, nietPaint);

        // Highlight oben links im Center-Stueck
        using var lightPaint = new SKPaint { Color = lightColor.WithAlpha(80), IsAntialias = false };
        canvas.DrawRect(tileX + center - halfPipe, tileY + center - halfPipe, halfPipe * 2, 2, lightPaint);
    }

    /// <summary>
    /// Zeichnet den Quell-Indikator: Gruenes Quadrat mit "S" Markierung.
    /// </summary>
    private void DrawSourceIndicator(SKCanvas canvas, float tileX, float tileY, float tileSize, float center)
    {
        float iconSize = tileSize * 0.32f;
        float iconX = tileX + center - iconSize / 2;
        float iconY = tileY + center - iconSize / 2;

        // Gruener Hintergrund
        using var bgPaint = new SKPaint { Color = SourceColor, IsAntialias = false };
        canvas.DrawRect(iconX, iconY, iconSize, iconSize, bgPaint);

        // Dunklerer Rand
        using var borderPaint = new SKPaint { Color = SourceDark, IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.DrawRect(iconX, iconY, iconSize, iconSize, borderPaint);

        // "S" Text
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = false };
        using var font = new SKFont(SKTypeface.Default, iconSize * 0.65f);
        canvas.DrawText("S", tileX + center, tileY + center + iconSize * 0.2f, SKTextAlign.Center, font, textPaint);
    }

    /// <summary>
    /// Zeichnet den Abfluss-Indikator: Blaues Quadrat mit "Z" (Ziel) Markierung.
    /// </summary>
    private void DrawDrainIndicator(SKCanvas canvas, float tileX, float tileY, float tileSize, float center)
    {
        float iconSize = tileSize * 0.32f;
        float iconX = tileX + center - iconSize / 2;
        float iconY = tileY + center - iconSize / 2;

        // Blauer Hintergrund
        using var bgPaint = new SKPaint { Color = DrainColor, IsAntialias = false };
        canvas.DrawRect(iconX, iconY, iconSize, iconSize, bgPaint);

        // Dunklerer Rand
        using var borderPaint = new SKPaint { Color = DrainDark, IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.DrawRect(iconX, iconY, iconSize, iconSize, borderPaint);

        // "Z" Text (Ziel)
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = false };
        using var font = new SKFont(SKTypeface.Default, iconSize * 0.65f);
        canvas.DrawText("Z", tileX + center, tileY + center + iconSize * 0.2f, SKTextAlign.Center, font, textPaint);
    }

    /// <summary>
    /// Zeichnet ein kleines Schloss-Symbol oben rechts auf der Kachel.
    /// </summary>
    private static void DrawLockIndicator(SKCanvas canvas, float tileX, float tileY, float tileSize)
    {
        float lockSize = 8;
        float lockX = tileX + tileSize - lockSize - 3;
        float lockY = tileY + 3;

        // Schloss-Koerper
        using var lockPaint = new SKPaint { Color = LockColor.WithAlpha(180), IsAntialias = false };
        canvas.DrawRect(lockX, lockY + 3, lockSize, lockSize - 3, lockPaint);

        // Schloss-Buegel (oben)
        using var buegelPaint = new SKPaint
        {
            Color = LockColor.WithAlpha(180),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(lockX + 2, lockY, lockSize - 4, 4, buegelPaint);
    }

    /// <summary>
    /// Pulsierender Wasser-Glow-Effekt auf verbundenen Kacheln.
    /// </summary>
    private void DrawWaterPulse(SKCanvas canvas, float tileX, float tileY, float tileSize, float posX)
    {
        // Sinuswelle fuer pulsierende Transparenz
        float pulse = 0.15f + 0.15f * MathF.Sin(_waterAnimTime * 3.5f + posX * 0.08f);
        byte alpha = (byte)(pulse * 255);

        using var waterGlow = new SKPaint { Color = WaterColor.WithAlpha(alpha), IsAntialias = false };
        canvas.DrawRect(tileX + 1, tileY + 1, tileSize - 2, tileSize - 2, waterGlow);
    }

    /// <summary>
    /// Gibt die offenen Richtungen fuer einen Rohr-Typ mit gegebener Rotation zurueck.
    /// Richtungen: 0=Oben, 1=Unten, 2=Links, 3=Rechts.
    /// </summary>
    private static int[] GetOpenings(int pipeType, int rotation)
    {
        // Basis-Oeffnungen bei Rotation 0
        int[] baseOpenings = pipeType switch
        {
            0 => [2, 3],          // Straight: Links, Rechts
            1 => [3, 1],          // Corner: Rechts, Unten
            2 => [3, 1, 2],       // TJunction: Rechts, Unten, Links
            3 => [0, 1, 2, 3],    // Cross: Alle 4
            _ => [2, 3]
        };

        int steps = (rotation / 90) % 4;
        if (steps == 0) return baseOpenings;

        // Rotation anwenden: Im Uhrzeigersinn drehen
        var rotated = new int[baseOpenings.Length];
        for (int i = 0; i < baseOpenings.Length; i++)
        {
            int d = baseOpenings[i];
            for (int s = 0; s < steps; s++)
            {
                d = d switch
                {
                    0 => 3, // Oben -> Rechts
                    3 => 1, // Rechts -> Unten
                    1 => 2, // Unten -> Links
                    2 => 0, // Links -> Oben
                    _ => d
                };
            }
            rotated[i] = d;
        }
        return rotated;
    }
}

/// <summary>
/// Vereinfachte Tile-Daten fuer den Renderer.
/// Wird aus dem ViewModel (PipeTile) befuellt.
/// </summary>
public struct PipeTileData
{
    /// <summary>0=Straight, 1=Corner, 2=TJunction, 3=Cross</summary>
    public int PipeType;

    /// <summary>Rotation in Grad: 0, 90, 180, 270</summary>
    public int Rotation;

    /// <summary>Ist dies die Wasserquelle?</summary>
    public bool IsSource;

    /// <summary>Ist dies der Abfluss/das Ziel?</summary>
    public bool IsDrain;

    /// <summary>Ist die Kachel gesperrt (nicht drehbar)?</summary>
    public bool IsLocked;

    /// <summary>Ist die Kachel mit der Quelle verbunden (Wasser fliesst)?</summary>
    public bool IsConnected;
}

using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Bauplan-Minigame.
/// Zeichnet ein Blaupausen-Grid mit Kacheln, Icons, Nummern und Partikel-Effekten.
/// Pixel-Art Stil passend zu CityRenderer/SawingGameRenderer.
/// </summary>
public class BlueprintGameRenderer
{
    // Blaupausen-Hintergrund
    private static readonly SKColor BlueprintBg = new(0x1A, 0x23, 0x7E);        // Dunkelblau
    private static readonly SKColor BlueprintGrid = new(0x28, 0x35, 0x93);       // Grid-Linien
    private static readonly SKColor BlueprintGridFine = new(0x1E, 0x2D, 0x8A);   // Feinere Grid-Linien

    // Kachel-Zustaende
    private static readonly SKColor CompletedBorder = new(0x4C, 0xAF, 0x50);     // Gruen
    private static readonly SKColor ActiveBorderColor = new(0xFF, 0xD7, 0x00);   // Gelb (pulsierend)
    private static readonly SKColor DefaultBorder = new(0xFF, 0xFF, 0xFF, 0x40); // Halbtransparent weiss
    private static readonly SKColor CheckmarkBg = new(0x4C, 0xAF, 0x50);        // Gruener Hintergrund fuer Haekchen
    private static readonly SKColor ErrorColor = new(0xF4, 0x43, 0x36);          // Rot bei Fehler

    // Text-Farben
    private static readonly SKColor TextWhite = SKColors.White;
    private static readonly SKColor TextNumber = new(0xFF, 0xFF, 0xFF, 0xE0);    // Leicht transparentes Weiss
    private static readonly SKColor TextQuestion = new(0xFF, 0xFF, 0xFF, 0x80);  // Gedimmtes Fragezeichen

    // Blaupausen-Staub-Partikel
    private readonly List<BlueprintDustParticle> _dustParticles = new();
    private float _animTime;

    private struct BlueprintDustParticle
    {
        public float X, Y, VelocityX, VelocityY, Life, MaxLife, Size;
    }

    /// <summary>
    /// Daten-Struct fuer einen einzelnen Bauschritt (View-optimiert, keine VM-Referenz).
    /// </summary>
    public struct BlueprintStepData
    {
        public string Icon;
        public string DisplayNumber;
        public uint BackgroundColor; // ARGB
        public bool IsCompleted;
        public bool IsActive;
        public bool HasError;
    }

    // Zwischengespeicherte Kachel-Positionen fuer HitTest
    private SKRect[] _tileRects = Array.Empty<SKRect>();

    /// <summary>
    /// Rendert das gesamte Bauplan-Spielfeld.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas zum Zeichnen.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="steps">Bauschritt-Daten.</param>
    /// <param name="cols">Anzahl Spalten im Grid.</param>
    /// <param name="isMemorizing">Memorisierungsphase aktiv.</param>
    /// <param name="isPlaying">Spielphase aktiv.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame in Sekunden.</param>
    public void Render(SKCanvas canvas, SKRect bounds,
        BlueprintStepData[] steps, int cols,
        bool isMemorizing, bool isPlaying,
        float deltaTime)
    {
        _animTime += deltaTime;

        // Blaupausen-Hintergrund zeichnen
        DrawBlueprintBackground(canvas, bounds);

        if (steps.Length == 0 || cols <= 0) return;

        // Grid-Layout berechnen
        int rows = (int)Math.Ceiling((double)steps.Length / cols);
        float padding = 16;
        float tileSpacing = 8;

        float availableWidth = bounds.Width - 2 * padding;
        float availableHeight = bounds.Height - 2 * padding;

        // Kachelgroesse berechnen (quadratisch, passt in verfuegbaren Platz)
        float maxTileWidth = (availableWidth - (cols - 1) * tileSpacing) / cols;
        float maxTileHeight = (availableHeight - (rows - 1) * tileSpacing) / rows;
        float tileSize = Math.Min(maxTileWidth, maxTileHeight);
        // Grid zentrieren
        float gridWidth = cols * tileSize + (cols - 1) * tileSpacing;
        float gridHeight = rows * tileSize + (rows - 1) * tileSpacing;
        float startX = bounds.Left + (bounds.Width - gridWidth) / 2;
        // Oben ausrichten statt vertikal zentrieren
        float startY = bounds.Top + padding;

        // Kachel-Positionen speichern fuer HitTest
        if (_tileRects.Length != steps.Length)
            _tileRects = new SKRect[steps.Length];

        // Kacheln zeichnen
        for (int i = 0; i < steps.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float x = startX + col * (tileSize + tileSpacing);
            float y = startY + row * (tileSize + tileSpacing);

            var tileRect = SKRect.Create(x, y, tileSize, tileSize);
            _tileRects[i] = tileRect;

            DrawTile(canvas, tileRect, steps[i], isMemorizing, isPlaying);
        }

        // Blaupausen-Staub-Partikel (atmosphaerisch)
        UpdateAndDrawDustParticles(canvas, bounds, deltaTime);
    }

    /// <summary>
    /// HitTest: Gibt den Index des getroffenen Schritts zurueck, oder -1.
    /// </summary>
    /// <param name="bounds">Zeichenbereich (gleich wie bei Render).</param>
    /// <param name="touchX">Touch X-Koordinate (in Canvas-Koordinaten).</param>
    /// <param name="touchY">Touch Y-Koordinate (in Canvas-Koordinaten).</param>
    /// <param name="cols">Anzahl Spalten (gleich wie bei Render).</param>
    /// <param name="totalSteps">Gesamtanzahl Schritte.</param>
    public int HitTest(SKRect bounds, float touchX, float touchY, int cols, int totalSteps)
    {
        for (int i = 0; i < _tileRects.Length && i < totalSteps; i++)
        {
            if (_tileRects[i].Contains(touchX, touchY))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Zeichnet den Blaupausen-Hintergrund mit Rasterlinien.
    /// </summary>
    private void DrawBlueprintBackground(SKCanvas canvas, SKRect bounds)
    {
        // Dunkelblauer Hintergrund
        using var bgPaint = new SKPaint { Color = BlueprintBg, IsAntialias = false };
        canvas.DrawRect(bounds, bgPaint);

        // Feine Rasterlinien (kleine Quadrate, 20px Abstand)
        using var fineGridPaint = new SKPaint
        {
            Color = BlueprintGridFine,
            IsAntialias = false,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        for (float x = bounds.Left; x < bounds.Right; x += 20)
        {
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, fineGridPaint);
        }
        for (float y = bounds.Top; y < bounds.Bottom; y += 20)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, fineGridPaint);
        }

        // Grobe Rasterlinien (groessere Quadrate, 80px Abstand)
        using var gridPaint = new SKPaint
        {
            Color = BlueprintGrid,
            IsAntialias = false,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        for (float x = bounds.Left; x < bounds.Right; x += 80)
        {
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, gridPaint);
        }
        for (float y = bounds.Top; y < bounds.Bottom; y += 80)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, gridPaint);
        }

        // Diagonale Markierung in den Ecken (Blaupausen-Stil)
        using var cornerPaint = new SKPaint
        {
            Color = BlueprintGrid.WithAlpha(80),
            IsAntialias = false,
            StrokeWidth = 1
        };

        float cornerSize = 30;
        // Links oben
        canvas.DrawLine(bounds.Left, bounds.Top + cornerSize, bounds.Left + cornerSize, bounds.Top, cornerPaint);
        // Rechts oben
        canvas.DrawLine(bounds.Right - cornerSize, bounds.Top, bounds.Right, bounds.Top + cornerSize, cornerPaint);
        // Links unten
        canvas.DrawLine(bounds.Left, bounds.Bottom - cornerSize, bounds.Left + cornerSize, bounds.Bottom, cornerPaint);
        // Rechts unten
        canvas.DrawLine(bounds.Right - cornerSize, bounds.Bottom, bounds.Right, bounds.Bottom - cornerSize, cornerPaint);
    }

    /// <summary>
    /// Zeichnet eine einzelne Kachel mit Icon, Nummer und Zustandsanzeige.
    /// </summary>
    private void DrawTile(SKCanvas canvas, SKRect rect, BlueprintStepData step, bool isMemorizing, bool isPlaying)
    {
        float cornerRadius = 8;

        // Hintergrundfarbe der Kachel
        var bgColor = new SKColor(
            (byte)((step.BackgroundColor >> 16) & 0xFF),
            (byte)((step.BackgroundColor >> 8) & 0xFF),
            (byte)(step.BackgroundColor & 0xFF),
            (byte)((step.BackgroundColor >> 24) & 0xFF)
        );

        // Kachel-Hintergrund (abgerundetes Rechteck)
        using var bgPaint = new SKPaint { Color = bgColor, IsAntialias = true };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, bgPaint);

        // Leichter Gradient-Effekt (obere Haelfte heller)
        using var highlightPaint = new SKPaint
        {
            Color = new SKColor(0xFF, 0xFF, 0xFF, 0x15),
            IsAntialias = true
        };
        var highlightRect = SKRect.Create(rect.Left, rect.Top, rect.Width, rect.Height * 0.45f);
        canvas.DrawRoundRect(highlightRect, cornerRadius, cornerRadius, highlightPaint);

        // Rahmen basierend auf Zustand
        DrawTileBorder(canvas, rect, step, cornerRadius);

        // Icon (Emoji oben in der Kachel)
        DrawTileIcon(canvas, rect, step.Icon);

        // Nummer oder Fragezeichen (unten in der Kachel)
        DrawTileNumber(canvas, rect, step.DisplayNumber, isMemorizing);

        // Haekchen bei abgeschlossenen Schritten (oben rechts)
        if (step.IsCompleted)
        {
            DrawCheckmark(canvas, rect);
        }

        // Fehler-Overlay (rotes Blinken)
        if (step.HasError)
        {
            using var errorPaint = new SKPaint
            {
                Color = ErrorColor.WithAlpha(100),
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, errorPaint);
        }
    }

    /// <summary>
    /// Zeichnet den Kachel-Rahmen je nach Zustand (aktiv=pulsierend gelb, erledigt=gruen, normal=weiss).
    /// </summary>
    private void DrawTileBorder(SKCanvas canvas, SKRect rect, BlueprintStepData step, float cornerRadius)
    {
        SKColor borderColor;
        float borderWidth;

        if (step.IsActive && !step.IsCompleted)
        {
            // Pulsierender gelber Rand fuer aktiven Schritt
            float pulse = (float)(0.5 + 0.5 * Math.Sin(_animTime * 5));
            byte alpha = (byte)(150 + 105 * pulse);
            borderColor = ActiveBorderColor.WithAlpha(alpha);
            borderWidth = 3;

            // Aeusserer Glow-Effekt
            using var glowPaint = new SKPaint
            {
                Color = ActiveBorderColor.WithAlpha((byte)(40 * pulse)),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 6
            };
            var glowRect = SKRect.Create(rect.Left - 2, rect.Top - 2, rect.Width + 4, rect.Height + 4);
            canvas.DrawRoundRect(glowRect, cornerRadius + 2, cornerRadius + 2, glowPaint);
        }
        else if (step.IsCompleted)
        {
            borderColor = CompletedBorder;
            borderWidth = 3;
        }
        else if (step.HasError)
        {
            borderColor = ErrorColor;
            borderWidth = 3;
        }
        else
        {
            borderColor = DefaultBorder;
            borderWidth = 2;
        }

        using var borderPaint = new SKPaint
        {
            Color = borderColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = borderWidth
        };
        canvas.DrawRoundRect(rect, cornerRadius, cornerRadius, borderPaint);
    }

    /// <summary>
    /// Zeichnet das Emoji-Icon in der oberen Haelfte der Kachel.
    /// </summary>
    private static void DrawTileIcon(SKCanvas canvas, SKRect rect, string icon)
    {
        if (string.IsNullOrEmpty(icon)) return;

        float fontSize = rect.Height * 0.35f;
        using var iconPaint = new SKPaint
        {
            TextSize = fontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        float x = rect.MidX;
        float y = rect.Top + rect.Height * 0.42f;

        canvas.DrawText(icon, x, y, iconPaint);
    }

    /// <summary>
    /// Zeichnet die Nummer oder das Fragezeichen in der unteren Haelfte der Kachel.
    /// </summary>
    private static void DrawTileNumber(SKCanvas canvas, SKRect rect, string displayNumber, bool isMemorizing)
    {
        if (string.IsNullOrEmpty(displayNumber)) return;

        bool isQuestion = displayNumber == "?";
        float fontSize = rect.Height * 0.28f;

        using var numberPaint = new SKPaint
        {
            Color = isQuestion ? TextQuestion : TextNumber,
            TextSize = fontSize,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = !isQuestion
        };

        float x = rect.MidX;
        float y = rect.Bottom - rect.Height * 0.12f;

        // Schatten unter der Nummer
        if (!isQuestion)
        {
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 80),
                TextSize = fontSize,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                FakeBoldText = true
            };
            canvas.DrawText(displayNumber, x + 1, y + 1, shadowPaint);
        }

        canvas.DrawText(displayNumber, x, y, numberPaint);

        // Memorisierungsphase: Nummer pulsiert leicht
        if (isMemorizing && !isQuestion)
        {
            using var glowPaint = new SKPaint
            {
                Color = TextWhite.WithAlpha(30),
                TextSize = fontSize + 2,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                FakeBoldText = true
            };
            canvas.DrawText(displayNumber, x, y, glowPaint);
        }
    }

    /// <summary>
    /// Zeichnet ein gruenes Haekchen oben rechts in der Kachel.
    /// </summary>
    private static void DrawCheckmark(SKCanvas canvas, SKRect rect)
    {
        float badgeSize = 16;
        float badgeX = rect.Right - badgeSize - 3;
        float badgeY = rect.Top + 3;

        // Gruener Kreis-Hintergrund
        using var circlePaint = new SKPaint
        {
            Color = CheckmarkBg,
            IsAntialias = true
        };
        canvas.DrawCircle(badgeX + badgeSize / 2, badgeY + badgeSize / 2, badgeSize / 2, circlePaint);

        // Haekchen (zwei Linien)
        using var checkPaint = new SKPaint
        {
            Color = TextWhite,
            IsAntialias = true,
            StrokeWidth = 2,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke
        };

        float cx = badgeX + badgeSize / 2;
        float cy = badgeY + badgeSize / 2;
        float s = badgeSize * 0.22f;

        // Kurzer Strich nach unten-links
        canvas.DrawLine(cx - s * 1.2f, cy, cx - s * 0.1f, cy + s, checkPaint);
        // Langer Strich nach oben-rechts
        canvas.DrawLine(cx - s * 0.1f, cy + s, cx + s * 1.5f, cy - s * 0.8f, checkPaint);
    }

    /// <summary>
    /// Aktualisiert und zeichnet die schwebenden Blaupausen-Staub-Partikel.
    /// Kleine weisse/blaue Punkte die langsam ueber den Hintergrund schweben.
    /// </summary>
    private void UpdateAndDrawDustParticles(SKCanvas canvas, SKRect bounds, float deltaTime)
    {
        var random = Random.Shared;

        // Neue Partikel erzeugen (maximal 15 gleichzeitig)
        while (_dustParticles.Count < 15)
        {
            _dustParticles.Add(new BlueprintDustParticle
            {
                X = bounds.Left + (float)random.NextDouble() * bounds.Width,
                Y = bounds.Top + (float)random.NextDouble() * bounds.Height,
                VelocityX = ((float)random.NextDouble() - 0.5f) * 15,
                VelocityY = -5 - (float)random.NextDouble() * 10,
                Life = 0,
                MaxLife = 3f + (float)random.NextDouble() * 4f,
                Size = 1 + (float)random.NextDouble() * 2
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

            // Leichtes horizontales Driften (Sinus)
            p.X += (float)Math.Sin(p.Life * 1.5f + i) * 3 * deltaTime;

            if (p.Life >= p.MaxLife || p.Y < bounds.Top - 10 || p.X < bounds.Left - 10 || p.X > bounds.Right + 10)
            {
                _dustParticles.RemoveAt(i);
                continue;
            }

            _dustParticles[i] = p;

            // Alpha: Einblenden -> Halten -> Ausblenden
            float lifeRatio = p.Life / p.MaxLife;
            float alpha;
            if (lifeRatio < 0.2f)
                alpha = lifeRatio / 0.2f; // Einblenden
            else if (lifeRatio > 0.7f)
                alpha = (1 - lifeRatio) / 0.3f; // Ausblenden
            else
                alpha = 1; // Volle Sichtbarkeit

            // Farbe: Weiss-Blau-Mix (Blaupausen-Atmosphaere)
            byte r = (byte)(200 + random.Next(0, 56));
            byte g = (byte)(210 + random.Next(0, 46));
            byte b = 255;
            dustPaint.Color = new SKColor(r, g, b, (byte)(alpha * 60));

            canvas.DrawCircle(p.X, p.Y, p.Size, dustPaint);
        }
    }
}

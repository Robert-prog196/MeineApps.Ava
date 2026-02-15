using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Saege-Minigame.
/// Zeichnet Holzbrett, Saegeschnitt, Timing-Bar mit Zonen und Marker.
/// Pixel-Art Stil: Flache Fuellungen, kein Anti-Aliasing, passend zu CityRenderer/WorkshopInteriorRenderer.
/// </summary>
public class SawingGameRenderer
{
    // Holz-Farben
    private static readonly SKColor WoodDark = new(0x6D, 0x4C, 0x41);
    private static readonly SKColor WoodMedium = new(0x8B, 0x5A, 0x2B);
    private static readonly SKColor WoodLight = new(0xA6, 0x7C, 0x52);
    private static readonly SKColor WoodGrain = new(0xBC, 0x98, 0x6A);

    // Saege-Farben
    private static readonly SKColor SawBlade = new(0xB0, 0xB0, 0xB0);
    private static readonly SKColor SawHandle = new(0xA0, 0x52, 0x2D);

    // Zonen-Farben
    private static readonly SKColor PerfectZone = new(0x4C, 0xAF, 0x50); // Gruen
    private static readonly SKColor GoodZone = new(0xE8, 0xA0, 0x0E);    // CraftOrange
    private static readonly SKColor OkZone = new(0xFF, 0xC1, 0x07);       // Gelb
    private static readonly SKColor MissZone = new(0xEF, 0x44, 0x44);     // Rot

    // Marker
    private static readonly SKColor MarkerColor = SKColors.White;

    // Saegemehl-Partikel
    private readonly List<SawdustParticle> _sawdust = new();
    private float _sawAnimTime;

    private struct SawdustParticle
    {
        public float X, Y, VelocityX, VelocityY, Life, MaxLife, Size;
    }

    /// <summary>
    /// Rendert das gesamte Saege-Spielfeld.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas zum Zeichnen.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="markerPosition">Marker-Position 0.0-1.0.</param>
    /// <param name="perfectStart">Perfect-Zone Startposition 0.0-1.0.</param>
    /// <param name="perfectWidth">Perfect-Zone Breite 0.0-1.0.</param>
    /// <param name="goodStart">Good-Zone Startposition 0.0-1.0.</param>
    /// <param name="goodWidth">Good-Zone Breite 0.0-1.0.</param>
    /// <param name="okStart">Ok-Zone Startposition 0.0-1.0.</param>
    /// <param name="okWidth">Ok-Zone Breite 0.0-1.0.</param>
    /// <param name="isPlaying">Ob das Spiel laeuft (Marker bewegt sich).</param>
    /// <param name="isResultShown">Ob das Ergebnis angezeigt wird.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame in Sekunden.</param>
    public void Render(SKCanvas canvas, SKRect bounds,
        double markerPosition,
        double perfectStart, double perfectWidth,
        double goodStart, double goodWidth,
        double okStart, double okWidth,
        bool isPlaying, bool isResultShown,
        float deltaTime)
    {
        _sawAnimTime += deltaTime;

        float padding = 16;
        float barAreaTop = bounds.Top + padding;
        float barAreaBottom = bounds.Bottom - padding;
        float barAreaHeight = barAreaBottom - barAreaTop;

        // Obere Haelfte: Holzbrett mit Saege
        float woodTop = barAreaTop;
        float woodHeight = barAreaHeight * 0.55f;
        float woodBottom = woodTop + woodHeight;
        DrawWoodBoard(canvas, bounds.Left + padding, woodTop, bounds.Width - 2 * padding, woodHeight, markerPosition, isPlaying);

        // Untere Haelfte: Timing-Bar
        float barTop = woodBottom + 20;
        float barHeight = Math.Min(50, barAreaHeight * 0.2f);
        float barLeft = bounds.Left + padding + 8;
        float barWidth = bounds.Width - 2 * padding - 16;
        DrawTimingBar(canvas, barLeft, barTop, barWidth, barHeight,
            markerPosition, perfectStart, perfectWidth, goodStart, goodWidth, okStart, okWidth, isPlaying);

        // Saegemehl-Partikel (nur waehrend des Spiels)
        if (isPlaying)
        {
            UpdateAndDrawSawdust(canvas, bounds, padding, woodBottom, deltaTime);
        }
    }

    /// <summary>
    /// Zeichnet das Holzbrett mit Maserung, Astloechern und Schnittkante.
    /// </summary>
    private void DrawWoodBoard(SKCanvas canvas, float x, float y, float width, float height, double markerPos, bool isPlaying)
    {
        // Holzbrett-Koerper
        using var woodPaint = new SKPaint { Color = WoodMedium, IsAntialias = false };
        canvas.DrawRect(x, y, width, height, woodPaint);

        // Holzmaserung (horizontale Linien im Pixel-Art-Stil)
        using var grainPaint = new SKPaint { Color = WoodGrain, IsAntialias = false, StrokeWidth = 2 };
        for (float gy = y + 10; gy < y + height; gy += 12)
        {
            canvas.DrawLine(x, gy, x + width, gy, grainPaint);
        }

        // Hellere Akzentlinien fuer mehr Tiefe
        using var lightGrainPaint = new SKPaint { Color = WoodLight, IsAntialias = false, StrokeWidth = 1 };
        for (float gy = y + 16; gy < y + height; gy += 24)
        {
            canvas.DrawLine(x + 4, gy, x + width - 4, gy, lightGrainPaint);
        }

        // Dunklerer Rand oben/unten (Brett-Kante)
        using var edgePaint = new SKPaint { Color = WoodDark, IsAntialias = false };
        canvas.DrawRect(x, y, width, 4, edgePaint);
        canvas.DrawRect(x, y + height - 4, width, 4, edgePaint);

        // Seitliche Kanten
        canvas.DrawRect(x, y, 3, height, edgePaint);
        canvas.DrawRect(x + width - 3, y, 3, height, edgePaint);

        // Astloecher (dekorative Kreise)
        using var knotPaint = new SKPaint { Color = WoodDark.WithAlpha(100), IsAntialias = false };
        canvas.DrawCircle(x + width * 0.2f, y + height * 0.35f, 4, knotPaint);
        canvas.DrawCircle(x + width * 0.75f, y + height * 0.6f, 3, knotPaint);
        canvas.DrawCircle(x + width * 0.45f, y + height * 0.75f, 2, knotPaint);

        // Schnittkante (vertikale Markierung in der Mitte)
        float cutX = x + width * 0.5f;

        // Gestrichelte Markierungslinie wo geschnitten werden soll
        using var markPaint = new SKPaint
        {
            Color = new SKColor(0xFF, 0xFF, 0xFF, 80),
            IsAntialias = false,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, 0)
        };
        canvas.DrawLine(cutX, y + 2, cutX, y + height - 2, markPaint);

        // Dunklere Schnittlinie darueber
        using var cutPaint = new SKPaint { Color = new SKColor(0x00, 0x00, 0x00, 0x40), IsAntialias = false, StrokeWidth = 3 };
        canvas.DrawLine(cutX, y, cutX, y + height, cutPaint);

        // Saege-Werkzeug unter dem Brett
        float sawY = y + height + 4;
        if (isPlaying)
        {
            // Saege vibriert waehrend des Spiels
            float sawBounce = (float)Math.Sin(_sawAnimTime * 8) * 3;
            DrawSaw(canvas, cutX, sawY + sawBounce);
        }
        else
        {
            // Saege ruht
            DrawSaw(canvas, cutX, sawY);
        }
    }

    /// <summary>
    /// Zeichnet die Pixel-Art Saege (Blatt + Zaehne + Griff).
    /// </summary>
    private void DrawSaw(SKCanvas canvas, float x, float y)
    {
        // Saegeblatt (horizontaler Balken)
        using var bladePaint = new SKPaint { Color = SawBlade, IsAntialias = false };
        canvas.DrawRect(x - 24, y, 48, 4, bladePaint);

        // Saegeblatt-Glanz (obere Kante heller)
        using var bladeHighlight = new SKPaint { Color = new SKColor(0xD0, 0xD0, 0xD0), IsAntialias = false };
        canvas.DrawRect(x - 22, y, 44, 1, bladeHighlight);

        // Saegezaehne (kleine Pixel-Bloecke unten)
        using var toothPaint = new SKPaint { Color = new SKColor(0x90, 0x90, 0x90), IsAntialias = false };
        for (float tx = x - 22; tx < x + 22; tx += 6)
        {
            canvas.DrawRect(tx, y + 4, 3, 3, toothPaint);
        }

        // Griff (Block ueber dem Blatt)
        using var handlePaint = new SKPaint { Color = SawHandle, IsAntialias = false };
        canvas.DrawRect(x - 8, y - 12, 16, 14, handlePaint);

        // Griff-Akzent (hellere Leiste)
        using var handleAccent = new SKPaint { Color = new SKColor(0xC0, 0x6C, 0x3A), IsAntialias = false };
        canvas.DrawRect(x - 6, y - 10, 12, 3, handleAccent);

        // Griff-Schatten (untere Kante dunkler)
        using var handleShadow = new SKPaint { Color = new SKColor(0x7D, 0x3E, 0x1A), IsAntialias = false };
        canvas.DrawRect(x - 6, y - 2, 12, 2, handleShadow);
    }

    /// <summary>
    /// Zeichnet die Timing-Bar mit Zonen (Miss, Ok, Good, Perfect) und Marker.
    /// </summary>
    private void DrawTimingBar(SKCanvas canvas, float x, float y, float width, float height,
        double markerPos, double pStart, double pWidth, double gStart, double gWidth, double oStart, double oWidth, bool isPlaying)
    {
        // Bar-Hintergrund (dunkles Holz-Optik)
        using var bgPaint = new SKPaint { Color = new SKColor(0x33, 0x2B, 0x20), IsAntialias = false };
        canvas.DrawRect(x, y, width, height, bgPaint);

        // Rahmen (Holzoptik)
        using var framePaint = new SKPaint { Color = new SKColor(0x5D, 0x40, 0x37), IsAntialias = false, StrokeWidth = 2, Style = SKPaintStyle.Stroke };
        canvas.DrawRect(x, y, width, height, framePaint);

        // Miss-Zone (gesamte Bar, transparent rot)
        using var missPaint = new SKPaint { Color = MissZone.WithAlpha(60), IsAntialias = false };
        canvas.DrawRect(x + 2, y + 2, width - 4, height - 4, missPaint);

        // OK-Zone
        float okLeft = x + (float)(oStart * width);
        float okW = (float)(oWidth * width);
        using var okPaint = new SKPaint { Color = OkZone.WithAlpha(100), IsAntialias = false };
        canvas.DrawRect(okLeft, y + 2, okW, height - 4, okPaint);

        // Good-Zone
        float goodLeft = x + (float)(gStart * width);
        float goodW = (float)(gWidth * width);
        using var goodPaint = new SKPaint { Color = GoodZone.WithAlpha(140), IsAntialias = false };
        canvas.DrawRect(goodLeft, y + 2, goodW, height - 4, goodPaint);

        // Perfect-Zone
        float perfLeft = x + (float)(pStart * width);
        float perfW = (float)(pWidth * width);
        using var perfectPaint = new SKPaint { Color = PerfectZone.WithAlpha(200), IsAntialias = false };
        canvas.DrawRect(perfLeft, y + 2, perfW, height - 4, perfectPaint);

        // Perfect-Zone Glow-Puls wenn Spiel aktiv
        if (isPlaying)
        {
            float pulse = (float)(0.5 + 0.5 * Math.Sin(_sawAnimTime * 4));
            using var glowPaint = new SKPaint { Color = PerfectZone.WithAlpha((byte)(40 * pulse)), IsAntialias = false };
            canvas.DrawRect(perfLeft - 2, y - 2, perfW + 4, height + 4, glowPaint);
        }

        // Tick-Markierungen auf der Bar (kleine Striche am oberen Rand)
        using var tickPaint = new SKPaint { Color = new SKColor(255, 255, 255, 40), IsAntialias = false, StrokeWidth = 1 };
        for (float t = 0.1f; t < 1.0f; t += 0.1f)
        {
            float tickX = x + t * width;
            canvas.DrawLine(tickX, y, tickX, y + 4, tickPaint);
            canvas.DrawLine(tickX, y + height - 4, tickX, y + height, tickPaint);
        }

        // Marker (weisser vertikaler Balken mit Schatten und Glanz)
        float markerX = x + (float)(markerPos * width);

        // Marker-Schatten (dahinter)
        using var markerShadow = new SKPaint { Color = new SKColor(0, 0, 0, 80), IsAntialias = false };
        canvas.DrawRect(markerX - 2, y - 3, 6, height + 6, markerShadow);

        // Marker-Koerper
        using var markerPaint = new SKPaint { Color = MarkerColor, IsAntialias = false };
        canvas.DrawRect(markerX - 3, y - 4, 6, height + 8, markerPaint);

        // Marker-Glanz (heller Pixel oben)
        using var markerHighlight = new SKPaint { Color = new SKColor(255, 255, 255, 200), IsAntialias = false };
        canvas.DrawRect(markerX - 1, y - 3, 2, 4, markerHighlight);

        // Marker-Spitze (kleines Dreieck oben als Pfeil-Indikator)
        using var arrowPaint = new SKPaint { Color = MarkerColor, IsAntialias = false };
        canvas.DrawRect(markerX - 4, y - 7, 8, 3, arrowPaint);
        canvas.DrawRect(markerX - 2, y - 9, 4, 2, arrowPaint);
    }

    /// <summary>
    /// Aktualisiert und zeichnet Saegemehl-Partikel die beim Saegen herunterfallen.
    /// </summary>
    private void UpdateAndDrawSawdust(SKCanvas canvas, SKRect bounds, float padding, float woodBottom, float deltaTime)
    {
        var random = Random.Shared;
        float cutX = bounds.Left + padding + (bounds.Width - 2 * padding) * 0.5f;

        // Neue Partikel erzeugen (maximal 20 gleichzeitig)
        if (_sawdust.Count < 20)
        {
            _sawdust.Add(new SawdustParticle
            {
                X = cutX + random.Next(-8, 9),
                Y = woodBottom - random.Next(0, 10),
                VelocityX = (float)(random.NextDouble() - 0.5) * 40,
                VelocityY = -20 - random.Next(0, 30),
                Life = 0,
                MaxLife = 0.5f + (float)random.NextDouble() * 0.5f,
                Size = 1 + random.Next(0, 3)
            });
        }

        // Partikel aktualisieren und zeichnen
        using var dustPaint = new SKPaint { IsAntialias = false };
        for (int i = _sawdust.Count - 1; i >= 0; i--)
        {
            var p = _sawdust[i];
            p.Life += deltaTime;
            p.X += p.VelocityX * deltaTime;
            p.Y += p.VelocityY * deltaTime;
            p.VelocityY += 60 * deltaTime; // Schwerkraft

            if (p.Life >= p.MaxLife)
            {
                _sawdust.RemoveAt(i);
                continue;
            }

            _sawdust[i] = p;

            // Alpha basierend auf verbleibender Lebenszeit
            float alpha = 1 - (p.Life / p.MaxLife);
            dustPaint.Color = new SKColor(0xD2, 0xB4, 0x8C, (byte)(alpha * 200));
            canvas.DrawRect(p.X, p.Y, p.Size, p.Size, dustPaint);
        }
    }
}

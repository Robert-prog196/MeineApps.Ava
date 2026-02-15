using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Verkabelungs-Minigame.
/// Zeichnet Sicherungskasten mit Kabelstraengen links/rechts im Pixel-Art/Handwerker-Stil.
/// Passend zu CityRenderer/WorkshopInteriorRenderer: Flache Fuellungen, kein Anti-Aliasing.
/// </summary>
public class WiringGameRenderer
{
    // Sicherungskasten-Farben
    private static readonly SKColor PanelBg = new(0x37, 0x47, 0x4F);
    private static readonly SKColor PanelBorder = new(0x26, 0x32, 0x38);
    private static readonly SKColor PanelAccent = new(0x54, 0x6E, 0x7A);
    private static readonly SKColor ConnectedBg = new(0x2E, 0x7D, 0x32, 40);
    private static readonly SKColor SelectedBg = new(0xFF, 0xFF, 0xFF, 50);
    private static readonly SKColor ErrorBg = new(0xFF, 0x44, 0x44, 60);

    // Kabelfarben (passend zum WireColor Enum im ViewModel)
    private static readonly SKColor[] WireColors =
    {
        new(0xFF, 0x44, 0x44), // Red
        new(0x44, 0x44, 0xFF), // Blue
        new(0x44, 0xFF, 0x44), // Green
        new(0xFF, 0xFF, 0x44), // Yellow
        new(0xFF, 0x88, 0x44), // Orange
        new(0xAA, 0x44, 0xFF), // Purple
    };

    private float _sparkTime;

    // Funken-Partikel fuer verbundene Kabel
    private readonly List<SparkParticle> _sparks = new();

    private struct SparkParticle
    {
        public float X, Y, VelocityX, VelocityY, Life, MaxLife;
    }

    /// <summary>
    /// Rendert das Verkabelungs-Spielfeld mit zwei Panels (IN/OUT),
    /// Verbindungslinien und Funken-Partikeln.
    /// </summary>
    public void Render(SKCanvas canvas, SKRect bounds, WireRenderData[] leftWires, WireRenderData[] rightWires,
        int? selectedLeftIndex, float deltaTime)
    {
        _sparkTime += deltaTime;

        // Padding 12 wegen CornerRadius=8 am Border (Ecken-Clipping vermeiden)
        float padding = 12;
        float gap = 6; // Abstand zwischen den 2 Panels
        float panelWidth = (bounds.Width - padding * 2 - gap) / 2;
        float panelHeight = bounds.Height - padding * 2;

        // Betonwand als Hintergrund
        DrawWallBackground(canvas, bounds);

        // Linkes Panel (Input-Seite)
        float leftPanelX = bounds.Left + padding;
        float panelY = bounds.Top + padding;
        DrawPanel(canvas, leftPanelX, panelY, panelWidth, panelHeight, "IN");
        DrawWires(canvas, leftPanelX + 8, panelY + 28, panelWidth - 16, panelHeight - 36,
            leftWires, true, selectedLeftIndex);

        // Rechtes Panel (Output-Seite)
        float rightPanelX = bounds.Left + padding + panelWidth + gap;
        DrawPanel(canvas, rightPanelX, panelY, panelWidth, panelHeight, "OUT");
        DrawWires(canvas, rightPanelX + 8, panelY + 28, panelWidth - 16, panelHeight - 36,
            rightWires, false, null);

        // Verbindungslinien zwischen verbundenen Kabeln
        DrawConnections(canvas, leftPanelX, rightPanelX, panelWidth, panelY, panelHeight,
            leftWires, rightWires);

        // Funken-Partikel bei verbundenen Kabeln
        UpdateAndDrawSparks(canvas, bounds, leftWires, rightWires, leftPanelX, rightPanelX,
            panelWidth, panelY, panelHeight, deltaTime);
    }

    /// <summary>
    /// Berechnet welches Kabel bei Touch getroffen wurde.
    /// Gibt (isLeft, wireIndex) zurueck oder (false, -1) wenn nichts getroffen.
    /// </summary>
    public (bool isLeft, int index) HitTest(SKRect bounds, float touchX, float touchY, int wireCount)
    {
        if (wireCount <= 0) return (false, -1);

        float padding = 12;
        float gap = 8; // Muss mit Render() uebereinstimmen
        float panelWidth = (bounds.Width - padding * 2 - gap) / 2;
        float panelY = bounds.Top + padding;
        float panelHeight = bounds.Height - padding * 2;
        float wireAreaTop = panelY + 28;
        float wireAreaHeight = panelHeight - 36;

        float wireHeight = Math.Min(50, (wireAreaHeight - (wireCount - 1) * 8) / wireCount);

        // Linkes Panel pruefen
        float leftPanelX = bounds.Left + padding;
        if (touchX >= leftPanelX && touchX <= leftPanelX + panelWidth)
        {
            for (int i = 0; i < wireCount; i++)
            {
                float wy = wireAreaTop + i * (wireHeight + 8);
                if (touchY >= wy && touchY <= wy + wireHeight)
                    return (true, i);
            }
        }

        // Rechtes Panel pruefen
        float rightPanelX = bounds.Left + padding + panelWidth + gap;
        if (touchX >= rightPanelX && touchX <= rightPanelX + panelWidth)
        {
            for (int i = 0; i < wireCount; i++)
            {
                float wy = wireAreaTop + i * (wireHeight + 8);
                if (touchY >= wy && touchY <= wy + wireHeight)
                    return (false, i);
            }
        }

        return (false, -1);
    }

    /// <summary>
    /// Zeichnet den Betonwand-Hintergrund mit subtiler Textur.
    /// </summary>
    private void DrawWallBackground(SKCanvas canvas, SKRect bounds)
    {
        using var wallPaint = new SKPaint { Color = new SKColor(0x45, 0x45, 0x45), IsAntialias = false };
        canvas.DrawRect(bounds, wallPaint);

        // Wand-Textur: horizontale Fugenlinien
        using var texturePaint = new SKPaint
        {
            Color = new SKColor(0x50, 0x50, 0x50),
            IsAntialias = false,
            StrokeWidth = 1
        };
        for (float y = bounds.Top + 20; y < bounds.Bottom; y += 20)
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, texturePaint);
    }

    /// <summary>
    /// Zeichnet einen Sicherungskasten (Panel) mit Header-Leiste und Label.
    /// </summary>
    private void DrawPanel(SKCanvas canvas, float x, float y, float width, float height, string label)
    {
        // Sicherungskasten-Koerper
        using var panelPaint = new SKPaint { Color = PanelBg, IsAntialias = false };
        canvas.DrawRect(x, y, width, height, panelPaint);

        // Rand
        using var borderPaint = new SKPaint
        {
            Color = PanelBorder,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(x, y, width, height, borderPaint);

        // Obere Leiste (Header-Balken)
        using var headerPaint = new SKPaint { Color = PanelAccent, IsAntialias = false };
        canvas.DrawRect(x + 2, y + 2, width - 4, 22, headerPaint);

        // Label-Text
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = false };
        using var font = new SKFont(SKTypeface.Default, 12);
        canvas.DrawText(label, x + width / 2, y + 16, SKTextAlign.Center, font, textPaint);

        // Schrauben in den Ecken (Pixel-Art Detail)
        using var screwPaint = new SKPaint { Color = new SKColor(0x78, 0x78, 0x78), IsAntialias = false };
        using var screwCenter = new SKPaint { Color = new SKColor(0x60, 0x60, 0x60), IsAntialias = false };
        float[] screwXs = { x + 6, x + width - 6 };
        float[] screwYs = { y + 6, y + height - 6 };
        foreach (float sx in screwXs)
        foreach (float sy in screwYs)
        {
            canvas.DrawRect(sx - 3, sy - 3, 6, 6, screwPaint);
            canvas.DrawRect(sx - 1, sy - 1, 2, 2, screwCenter);
        }
    }

    /// <summary>
    /// Zeichnet die Kabel innerhalb eines Panels (links oder rechts).
    /// Jedes Kabel hat Rahmen, Kabelstrang, Farbpunkt und Status-Hintergrund.
    /// </summary>
    private void DrawWires(SKCanvas canvas, float x, float y, float width, float height,
        WireRenderData[] wires, bool isLeft, int? selectedIndex)
    {
        if (wires.Length == 0) return;

        float wireHeight = Math.Min(50, (height - (wires.Length - 1) * 8) / wires.Length);

        for (int i = 0; i < wires.Length; i++)
        {
            var wire = wires[i];
            float wy = y + i * (wireHeight + 8);

            var wireColor = WireColors[Math.Min(wire.ColorIndex, WireColors.Length - 1)];

            // Status-Hintergrund zeichnen
            if (wire.IsConnected)
            {
                using var connPaint = new SKPaint { Color = ConnectedBg, IsAntialias = false };
                canvas.DrawRect(x, wy, width, wireHeight, connPaint);
            }
            else if (wire.HasError)
            {
                using var errPaint = new SKPaint { Color = ErrorBg, IsAntialias = false };
                canvas.DrawRect(x, wy, width, wireHeight, errPaint);
            }
            else if (isLeft && selectedIndex == i)
            {
                using var selPaint = new SKPaint { Color = SelectedBg, IsAntialias = false };
                canvas.DrawRect(x, wy, width, wireHeight, selPaint);

                // Pulsierender Glow fuer selektiertes Kabel
                float pulse = (float)(0.3 + 0.3 * Math.Sin(_sparkTime * 6));
                using var glowPaint = new SKPaint
                {
                    Color = wireColor.WithAlpha((byte)(pulse * 255)),
                    IsAntialias = false
                };
                canvas.DrawRect(x - 2, wy - 2, width + 4, wireHeight + 4, glowPaint);
            }

            // Kabel-Rahmen in Kabelfarbe
            using var framePaint = new SKPaint
            {
                Color = wireColor,
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3
            };
            canvas.DrawRect(x + 2, wy + 2, width - 4, wireHeight - 4, framePaint);

            // Kabelstrang in der Mitte (horizontaler Balken)
            float cableY = wy + wireHeight / 2;
            float cableThickness = 6;
            using var cablePaint = new SKPaint { Color = wireColor, IsAntialias = false };

            if (isLeft)
            {
                // Kabel geht nach rechts raus (Stecker am rechten Rand)
                canvas.DrawRect(x + width * 0.2f, cableY - cableThickness / 2,
                    width * 0.8f, cableThickness, cablePaint);
                // Stecker-Ende
                using var plugPaint = new SKPaint { Color = wireColor.WithAlpha(200), IsAntialias = false };
                canvas.DrawRect(x + width - 4, cableY - cableThickness, 4, cableThickness * 2, plugPaint);
            }
            else
            {
                // Kabel geht nach links raus (Stecker am linken Rand)
                canvas.DrawRect(x, cableY - cableThickness / 2,
                    width * 0.8f, cableThickness, cablePaint);
                // Stecker-Ende
                using var plugPaint = new SKPaint { Color = wireColor.WithAlpha(200), IsAntialias = false };
                canvas.DrawRect(x, cableY - cableThickness, 4, cableThickness * 2, plugPaint);
            }

            // Farbiger Kreis als Indikator
            using var dotPaint = new SKPaint { Color = wireColor, IsAntialias = false };
            float dotX = isLeft ? x + 12 : x + width - 12;
            canvas.DrawCircle(dotX, cableY, 8, dotPaint);

            // Heller Kern im Kreis (Highlight)
            using var dotHighlight = new SKPaint
            {
                Color = new SKColor(
                    (byte)Math.Min(255, wireColor.Red + 80),
                    (byte)Math.Min(255, wireColor.Green + 80),
                    (byte)Math.Min(255, wireColor.Blue + 80),
                    180),
                IsAntialias = false
            };
            canvas.DrawCircle(dotX, cableY - 2, 4, dotHighlight);

            // Verbunden-Haekchen (Pixel-Art L-Form in Gruen)
            if (wire.IsConnected)
            {
                using var checkPaint = new SKPaint { Color = new SKColor(0x4C, 0xAF, 0x50), IsAntialias = false };
                float cx = x + width / 2;
                float cy = cableY;
                // Kurzer vertikaler Strich (links)
                canvas.DrawRect(cx - 6, cy - 4, 3, 8, checkPaint);
                // Laengerer horizontaler Strich (nach rechts)
                canvas.DrawRect(cx - 6, cy + 1, 12, 3, checkPaint);
            }

            // Kabel-Isolierung-Streifen (Pixel-Art Detail auf dem Strang)
            if (!wire.IsConnected)
            {
                using var stripePaint = new SKPaint
                {
                    Color = wireColor.WithAlpha(140),
                    IsAntialias = false
                };
                float stripeStart = isLeft ? x + width * 0.3f : x + width * 0.1f;
                float stripeEnd = isLeft ? x + width * 0.7f : x + width * 0.5f;
                for (float sx = stripeStart; sx < stripeEnd; sx += 10)
                {
                    canvas.DrawRect(sx, cableY - cableThickness / 2 - 1, 2, cableThickness + 2, stripePaint);
                }
            }
        }
    }

    /// <summary>
    /// Zeichnet Verbindungslinien zwischen korrekt verbundenen Kabeln
    /// (links nach rechts, passende Farbe).
    /// </summary>
    private void DrawConnections(SKCanvas canvas, float leftPanelX, float rightPanelX, float panelWidth,
        float panelY, float panelHeight, WireRenderData[] leftWires, WireRenderData[] rightWires)
    {
        if (leftWires.Length == 0) return;

        float wireAreaTop = panelY + 28;
        float wireAreaHeight = panelHeight - 36;
        float wireHeight = Math.Min(50, (wireAreaHeight - (leftWires.Length - 1) * 8) / leftWires.Length);

        float leftEndX = leftPanelX + panelWidth;
        float rightStartX = rightPanelX;
        float gapWidth = rightStartX - leftEndX;

        using var linePaint = new SKPaint { IsAntialias = false, StrokeWidth = 3 };
        using var glowPaint = new SKPaint { IsAntialias = false, StrokeWidth = 5 };

        for (int i = 0; i < leftWires.Length; i++)
        {
            if (!leftWires[i].IsConnected) continue;

            var wireColor = WireColors[Math.Min(leftWires[i].ColorIndex, WireColors.Length - 1)];

            // Passendes rechtes Kabel suchen (gleiche Farbe)
            for (int j = 0; j < rightWires.Length; j++)
            {
                if (!rightWires[j].IsConnected || rightWires[j].ColorIndex != leftWires[i].ColorIndex)
                    continue;

                float leftY = wireAreaTop + i * (wireHeight + 8) + wireHeight / 2;
                float rightY = wireAreaTop + j * (wireHeight + 8) + wireHeight / 2;

                // Hinterer Glow (breiter, transparent)
                glowPaint.Color = wireColor.WithAlpha(60);
                canvas.DrawLine(leftEndX, leftY, rightStartX, rightY, glowPaint);

                // Kabel-Linie
                linePaint.Color = wireColor.WithAlpha(180);
                canvas.DrawLine(leftEndX, leftY, rightStartX, rightY, linePaint);

                // Mittelpunkt-Markierung (kleiner Kreis als Verbindungs-Knoten)
                float midX = leftEndX + gapWidth / 2;
                float midY = leftY + (rightY - leftY) / 2;
                using var nodePaint = new SKPaint { Color = wireColor, IsAntialias = false };
                canvas.DrawCircle(midX, midY, 3, nodePaint);

                break;
            }
        }
    }

    /// <summary>
    /// Erzeugt, aktualisiert und zeichnet Funken-Partikel bei verbundenen Kabeln.
    /// Gelb-orange Funken steigen kurz auf und fallen dann herunter.
    /// </summary>
    private void UpdateAndDrawSparks(SKCanvas canvas, SKRect bounds,
        WireRenderData[] leftWires, WireRenderData[] rightWires,
        float leftPanelX, float rightPanelX, float panelWidth,
        float panelY, float panelHeight, float deltaTime)
    {
        var random = Random.Shared;

        float wireAreaTop = panelY + 28;
        float wireAreaHeight = panelHeight - 36;
        int wireCount = leftWires.Length;
        if (wireCount == 0) return;
        float wireHeight = Math.Min(50, (wireAreaHeight - (wireCount - 1) * 8) / wireCount);
        float leftEndX = leftPanelX + panelWidth;
        float rightStartX = rightPanelX;
        float gapMidX = (leftEndX + rightStartX) / 2;

        // Neue Funken bei verbundenen Kabeln erzeugen (max 20)
        if (_sparks.Count < 20)
        {
            for (int i = 0; i < leftWires.Length; i++)
            {
                if (!leftWires[i].IsConnected) continue;
                if (random.Next(8) != 0) continue; // Zufaellig neue Funken

                // Funkenposition am Mittelpunkt der Verbindungslinie
                float leftY = wireAreaTop + i * (wireHeight + 8) + wireHeight / 2;

                // Passendes rechtes Kabel finden fuer Y-Position
                float rightY = leftY;
                for (int j = 0; j < rightWires.Length; j++)
                {
                    if (rightWires[j].IsConnected && rightWires[j].ColorIndex == leftWires[i].ColorIndex)
                    {
                        rightY = wireAreaTop + j * (wireHeight + 8) + wireHeight / 2;
                        break;
                    }
                }

                float midY = leftY + (rightY - leftY) / 2;

                _sparks.Add(new SparkParticle
                {
                    X = gapMidX + random.Next(-15, 16),
                    Y = midY + random.Next(-4, 5),
                    VelocityX = (float)(random.NextDouble() - 0.5) * 50,
                    VelocityY = -25 - random.Next(0, 20),
                    Life = 0,
                    MaxLife = 0.3f + (float)random.NextDouble() * 0.3f
                });
            }
        }

        // Partikel aktualisieren und zeichnen
        using var sparkPaint = new SKPaint { IsAntialias = false };
        for (int i = _sparks.Count - 1; i >= 0; i--)
        {
            var p = _sparks[i];
            p.Life += deltaTime;
            p.X += p.VelocityX * deltaTime;
            p.Y += p.VelocityY * deltaTime;
            p.VelocityY += 80 * deltaTime; // Schwerkraft

            if (p.Life >= p.MaxLife)
            {
                _sparks.RemoveAt(i);
                continue;
            }

            _sparks[i] = p;

            // Alpha basierend auf verbleibender Lebenszeit
            float alpha = 1 - (p.Life / p.MaxLife);
            // Gelb-orange Funken (warme Farbe)
            sparkPaint.Color = new SKColor(0xFF, 0xC1, 0x07, (byte)(alpha * 255));
            canvas.DrawRect(p.X, p.Y, 2, 2, sparkPaint);

            // Hellerer Kern fuer groessere Partikel
            if (alpha > 0.5f)
            {
                sparkPaint.Color = new SKColor(0xFF, 0xE0, 0x82, (byte)(alpha * 200));
                canvas.DrawRect(p.X, p.Y, 1, 1, sparkPaint);
            }
        }
    }
}

/// <summary>
/// Vereinfachte Kabel-Daten fuer den Renderer.
/// Wird im Code-Behind aus dem ViewModel-Wire extrahiert.
/// </summary>
public struct WireRenderData
{
    /// <summary>Index in WireColors Array (0=Red, 1=Blue, ...)</summary>
    public int ColorIndex;

    /// <summary>Ob das Kabel aktuell ausgewaehlt ist.</summary>
    public bool IsSelected;

    /// <summary>Ob das Kabel erfolgreich verbunden wurde.</summary>
    public bool IsConnected;

    /// <summary>Ob ein Fehl-Versuch angezeigt wird (roter Flash).</summary>
    public bool HasError;
}

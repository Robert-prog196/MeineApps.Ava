using SkiaSharp;
using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// Rendert das Innere einer Werkstatt: Boden, Werkbänke, Arbeiter, Werkzeugwand.
/// Jeder Workshop-Typ hat eine einzigartige visuelle Szene.
/// </summary>
public class WorkshopInteriorRenderer
{
    // Tier -> Farbe fuer Worker-Kreis
    private static readonly Dictionary<WorkerTier, SKColor> TierColors = new()
    {
        { WorkerTier.F, new SKColor(0x9E, 0x9E, 0x9E) },  // Grey
        { WorkerTier.E, new SKColor(0x4C, 0xAF, 0x50) },  // Green
        { WorkerTier.D, new SKColor(0x21, 0x96, 0xF3) },  // Blue
        { WorkerTier.C, new SKColor(0x9C, 0x27, 0xB0) },  // Purple
        { WorkerTier.B, new SKColor(0xFF, 0xC1, 0x07) },  // Gold
        { WorkerTier.A, new SKColor(0xF4, 0x43, 0x36) },  // Red
        { WorkerTier.S, new SKColor(0xFF, 0x98, 0x00) }   // Orange
    };

    // Workshop-Typ-spezifische Farbpaletten
    private static readonly Dictionary<WorkshopType, (SKColor floor, SKColor wall, SKColor accent)> WorkshopColors = new()
    {
        { WorkshopType.Carpenter,          (new SKColor(0xBC, 0xAA, 0x84), new SKColor(0xD7, 0xCC, 0xB7), new SKColor(0xA0, 0x52, 0x2D)) }, // Holztöne
        { WorkshopType.Plumber,            (new SKColor(0xB0, 0xBE, 0xC5), new SKColor(0xCF, 0xD8, 0xDC), new SKColor(0x0E, 0x74, 0x90)) }, // Grau-Blau
        { WorkshopType.Electrician,        (new SKColor(0xBD, 0xBD, 0xBD), new SKColor(0xE0, 0xE0, 0xE0), new SKColor(0xF9, 0x73, 0x16)) }, // Grau-Orange
        { WorkshopType.Painter,            (new SKColor(0xE1, 0xD5, 0xC0), new SKColor(0xF5, 0xF5, 0xF5), new SKColor(0xEC, 0x48, 0x99)) }, // Hell-Pink
        { WorkshopType.Roofer,             (new SKColor(0xA1, 0x88, 0x7F), new SKColor(0xD7, 0xCC, 0xA1), new SKColor(0xDC, 0x26, 0x26)) }, // Braun-Rot
        { WorkshopType.Contractor,         (new SKColor(0xC8, 0xB0, 0x90), new SKColor(0xD2, 0xC4, 0xA0), new SKColor(0xEA, 0x58, 0x0C)) }, // Sand-Orange
        { WorkshopType.Architect,          (new SKColor(0xD0, 0xD0, 0xD0), new SKColor(0xEE, 0xEE, 0xEE), new SKColor(0x78, 0x71, 0x6C)) }, // Clean-Grau
        { WorkshopType.GeneralContractor,  (new SKColor(0xC0, 0xA8, 0x70), new SKColor(0xE8, 0xD8, 0xA0), new SKColor(0xFF, 0xD7, 0x00)) }, // Gold
    };

    /// <summary>
    /// Rendert das Innere eines einzelnen Workshops mit typ-spezifischer Szene.
    /// </summary>
    public void Render(SKCanvas canvas, SKRect bounds, Workshop workshop)
    {
        var colors = WorkshopColors.GetValueOrDefault(workshop.Type,
            (new SKColor(0xBC, 0xAA, 0x84), new SKColor(0xD7, 0xCC, 0xB7), new SKColor(0xA0, 0x52, 0x2D)));

        DrawFloor(canvas, bounds, colors.Item1, workshop.Type);
        DrawToolWall(canvas, bounds, colors.Item2, workshop.Type, colors.Item3);
        DrawWorkbenches(canvas, bounds, workshop, colors.Item3);
        DrawTypSpecificDecor(canvas, bounds, workshop.Type, colors.Item3);
        DrawWorkers(canvas, bounds, workshop);
    }

    private void DrawFloor(SKCanvas canvas, SKRect bounds, SKColor floorColor, WorkshopType type)
    {
        // Boden-Grundfarbe
        using (var floorPaint = new SKPaint { Color = floorColor, IsAntialias = false })
        {
            canvas.DrawRect(bounds, floorPaint);
        }

        // Boden-Muster je nach Typ
        using var linePaint = new SKPaint { IsAntialias = false, StrokeWidth = 1 };

        switch (type)
        {
            case WorkshopType.Carpenter:
                // Holz-Dielenbretter
                linePaint.Color = floorColor.WithAlpha(180);
                linePaint.Color = new SKColor(0xA6, 0x93, 0x72);
                for (float y = bounds.Top + 16; y < bounds.Bottom; y += 16)
                    canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
                break;

            case WorkshopType.Plumber:
                // Fliesen-Raster
                linePaint.Color = new SKColor(0x90, 0xA4, 0xAE);
                for (float y = bounds.Top + 20; y < bounds.Bottom; y += 20)
                    canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
                for (float x = bounds.Left + 20; x < bounds.Right; x += 20)
                    canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, linePaint);
                break;

            case WorkshopType.Electrician:
                // Industrieboden: Warnstreifen unten
                linePaint.Color = new SKColor(0xFB, 0xC0, 0x2D);
                linePaint.StrokeWidth = 3;
                for (float x = bounds.Left; x < bounds.Right; x += 12)
                    canvas.DrawLine(x, bounds.Bottom - 6, x + 6, bounds.Bottom, linePaint);
                break;

            case WorkshopType.Painter:
                // Farbspritzer auf dem Boden
                using (var splatPaint = new SKPaint { IsAntialias = false })
                {
                    var splatColors = new[] {
                        new SKColor(0xEC, 0x48, 0x99, 40), new SKColor(0x42, 0xA5, 0xF5, 40),
                        new SKColor(0x66, 0xBB, 0x6A, 40), new SKColor(0xFF, 0xCA, 0x28, 40)
                    };
                    for (int i = 0; i < 8; i++)
                    {
                        splatPaint.Color = splatColors[i % splatColors.Length];
                        float sx = bounds.Left + 20 + (i * 37) % (bounds.Width - 40);
                        float sy = bounds.Top + 30 + (i * 23) % (bounds.Height - 40);
                        canvas.DrawCircle(sx, sy, 5 + (i % 3) * 2, splatPaint);
                    }
                }
                break;

            case WorkshopType.Roofer:
                // Dachziegel-Muster (Wellenlinie)
                linePaint.Color = new SKColor(0x8D, 0x6E, 0x63);
                for (float y = bounds.Top + 14; y < bounds.Bottom; y += 14)
                    canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
                break;

            case WorkshopType.Contractor:
                // Beton mit Fugen
                linePaint.Color = new SKColor(0xB0, 0x9A, 0x78);
                for (float y = bounds.Top + 24; y < bounds.Bottom; y += 24)
                    canvas.DrawLine(bounds.Left, y, bounds.Right, y, linePaint);
                break;

            case WorkshopType.Architect:
                // Sauberer Marmorboden - diagonale Muster
                linePaint.Color = new SKColor(0xC0, 0xC0, 0xC0, 100);
                for (float d = bounds.Left - bounds.Height; d < bounds.Right; d += 30)
                {
                    canvas.DrawLine(d, bounds.Bottom, d + bounds.Height, bounds.Top, linePaint);
                }
                break;

            case WorkshopType.GeneralContractor:
                // Edler Parkettboden (Fischgrätmuster)
                linePaint.Color = new SKColor(0xA8, 0x90, 0x60);
                for (float y = bounds.Top + 12; y < bounds.Bottom; y += 12)
                {
                    for (float x = bounds.Left; x < bounds.Right; x += 24)
                    {
                        float offset = ((int)(y / 12) % 2 == 0) ? 0 : 12;
                        canvas.DrawLine(x + offset, y, x + offset + 12, y, linePaint);
                    }
                }
                break;
        }

        // Wände (oberer und linker Rand) - Workshop-spezifische Wand
        var wallColor = WorkshopColors.GetValueOrDefault(type,
            (floorColor, new SKColor(0xD7, 0xCC, 0xB7), floorColor)).Item2;
        using (var wallPaint = new SKPaint { Color = wallColor, IsAntialias = false })
        {
            canvas.DrawRect(bounds.Left, bounds.Top, bounds.Width, 8, wallPaint);
            canvas.DrawRect(bounds.Left, bounds.Top, 8, bounds.Height, wallPaint);
        }
    }

    private void DrawToolWall(SKCanvas canvas, SKRect bounds, SKColor wallColor, WorkshopType type, SKColor accent)
    {
        // Werkzeugwand rechts (typ-spezifische Farbe)
        float wallWidth = 24;
        float wallX = bounds.Right - wallWidth;
        using (var wallBgPaint = new SKPaint { Color = new SKColor(0x60, 0x50, 0x45), IsAntialias = false })
        {
            canvas.DrawRect(wallX, bounds.Top + 8, wallWidth, bounds.Height - 8, wallBgPaint);
        }

        // Typ-spezifische Werkzeuge an der Wand
        using var toolPaint = new SKPaint { IsAntialias = false };
        float toolX = wallX + 4;
        float toolY = bounds.Top + 16;

        switch (type)
        {
            case WorkshopType.Carpenter:
                // Hammer
                toolPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                canvas.DrawRect(toolX + 6, toolY, 4, 18, toolPaint);
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawRect(toolX + 2, toolY, 12, 5, toolPaint);
                toolY += 24;
                // Säge
                toolPaint.Color = new SKColor(0xB0, 0xB0, 0xB0);
                canvas.DrawRect(toolX, toolY, 16, 3, toolPaint);
                toolPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                canvas.DrawRect(toolX, toolY + 3, 6, 8, toolPaint);
                toolY += 18;
                // Hobel
                toolPaint.Color = new SKColor(0x8D, 0x6E, 0x63);
                canvas.DrawRect(toolX + 2, toolY, 12, 6, toolPaint);
                toolPaint.Color = new SKColor(0xA0, 0x52, 0x2D);
                canvas.DrawRect(toolX + 4, toolY + 6, 8, 4, toolPaint);
                break;

            case WorkshopType.Plumber:
                // Rohrzange
                toolPaint.Color = new SKColor(0xE0, 0x35, 0x35);
                canvas.DrawRect(toolX + 2, toolY, 4, 18, toolPaint);
                canvas.DrawRect(toolX + 10, toolY, 4, 18, toolPaint);
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawRect(toolX + 4, toolY + 6, 8, 4, toolPaint);
                toolY += 24;
                // Rohr-Stück
                toolPaint.Color = new SKColor(0x78, 0x90, 0x9C);
                canvas.DrawRect(toolX + 2, toolY, 12, 5, toolPaint);
                canvas.DrawRect(toolX + 5, toolY + 5, 6, 10, toolPaint);
                toolY += 20;
                // Dichtungsring
                toolPaint.Color = new SKColor(0x26, 0x32, 0x38);
                canvas.DrawCircle(toolX + 8, toolY + 6, 5, toolPaint);
                toolPaint.Color = new SKColor(0x60, 0x50, 0x45);
                canvas.DrawCircle(toolX + 8, toolY + 6, 3, toolPaint);
                break;

            case WorkshopType.Electrician:
                // Spannungsprüfer
                toolPaint.Color = new SKColor(0xF9, 0x73, 0x16);
                canvas.DrawRect(toolX + 5, toolY, 6, 16, toolPaint);
                toolPaint.Color = new SKColor(0xFF, 0xE0, 0x82);
                canvas.DrawRect(toolX + 6, toolY, 4, 3, toolPaint);
                toolY += 22;
                // Kabelrolle
                toolPaint.Color = new SKColor(0xF4, 0x43, 0x36);
                canvas.DrawCircle(toolX + 8, toolY + 6, 6, toolPaint);
                toolPaint.Color = new SKColor(0x60, 0x50, 0x45);
                canvas.DrawCircle(toolX + 8, toolY + 6, 2, toolPaint);
                toolY += 18;
                // Sicherung
                toolPaint.Color = new SKColor(0x21, 0x21, 0x21);
                canvas.DrawRect(toolX + 3, toolY, 10, 14, toolPaint);
                toolPaint.Color = new SKColor(0xFF, 0xC1, 0x07);
                canvas.DrawRect(toolX + 5, toolY + 2, 6, 3, toolPaint);
                break;

            case WorkshopType.Painter:
                // Pinsel
                toolPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                canvas.DrawRect(toolX + 6, toolY, 4, 14, toolPaint);
                toolPaint.Color = new SKColor(0xEC, 0x48, 0x99);
                canvas.DrawRect(toolX + 4, toolY + 14, 8, 6, toolPaint);
                toolY += 26;
                // Farbroller
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawRect(toolX + 6, toolY, 3, 12, toolPaint);
                toolPaint.Color = new SKColor(0x42, 0xA5, 0xF5);
                canvas.DrawRect(toolX + 2, toolY + 12, 12, 6, toolPaint);
                toolY += 24;
                // Farbpalette
                toolPaint.Color = new SKColor(0xD7, 0xCC, 0xB7);
                canvas.DrawCircle(toolX + 8, toolY + 6, 7, toolPaint);
                var paletteColors = new[] {
                    new SKColor(0xF4, 0x43, 0x36), new SKColor(0x42, 0xA5, 0xF5),
                    new SKColor(0xFF, 0xCA, 0x28), new SKColor(0x66, 0xBB, 0x6A) };
                for (int i = 0; i < 4; i++)
                {
                    toolPaint.Color = paletteColors[i];
                    canvas.DrawCircle(toolX + 4 + (i % 2) * 8, toolY + 3 + (i / 2) * 6, 2, toolPaint);
                }
                break;

            case WorkshopType.Roofer:
                // Dachhammer
                toolPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                canvas.DrawRect(toolX + 6, toolY, 4, 16, toolPaint);
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawRect(toolX + 2, toolY, 12, 4, toolPaint);
                toolY += 24;
                // Dachziegel
                toolPaint.Color = new SKColor(0xDC, 0x26, 0x26);
                canvas.DrawRect(toolX + 1, toolY, 14, 8, toolPaint);
                toolPaint.Color = new SKColor(0xBF, 0x20, 0x20);
                canvas.DrawLine(toolX + 1, toolY + 4, toolX + 15, toolY + 4, toolPaint);
                toolY += 14;
                // Leiter
                toolPaint.Color = new SKColor(0xA1, 0x88, 0x7F);
                canvas.DrawRect(toolX + 3, toolY, 2, 18, toolPaint);
                canvas.DrawRect(toolX + 11, toolY, 2, 18, toolPaint);
                for (int r = 0; r < 4; r++)
                    canvas.DrawRect(toolX + 3, toolY + 3 + r * 5, 10, 2, toolPaint);
                break;

            case WorkshopType.Contractor:
                // Wasserwaage
                toolPaint.Color = new SKColor(0xFF, 0xC1, 0x07);
                canvas.DrawRect(toolX + 1, toolY + 4, 14, 4, toolPaint);
                toolPaint.Color = new SKColor(0x66, 0xBB, 0x6A);
                canvas.DrawRect(toolX + 5, toolY + 5, 6, 2, toolPaint);
                toolY += 14;
                // Kelle
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawRect(toolX + 3, toolY, 10, 8, toolPaint);
                toolPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                canvas.DrawRect(toolX + 6, toolY + 8, 4, 10, toolPaint);
                toolY += 24;
                // Zollstock
                toolPaint.Color = new SKColor(0xFF, 0xC1, 0x07);
                canvas.DrawRect(toolX + 2, toolY, 12, 3, toolPaint);
                canvas.DrawRect(toolX + 2, toolY, 3, 12, toolPaint);
                break;

            case WorkshopType.Architect:
                // Lineal/Geodreieck
                toolPaint.Color = new SKColor(0xB0, 0xB0, 0xB0);
                canvas.DrawRect(toolX + 1, toolY, 14, 3, toolPaint);
                toolPaint.Color = new SKColor(0x90, 0x90, 0x90);
                canvas.DrawRect(toolX + 2, toolY + 5, 12, 12, toolPaint);
                toolY += 22;
                // Zirkel
                toolPaint.Color = new SKColor(0x78, 0x78, 0x78);
                canvas.DrawLine(toolX + 8, toolY, toolX + 3, toolY + 16, toolPaint);
                canvas.DrawLine(toolX + 8, toolY, toolX + 13, toolY + 16, toolPaint);
                toolY += 22;
                // Bleistift
                toolPaint.Color = new SKColor(0xFF, 0xCA, 0x28);
                canvas.DrawRect(toolX + 6, toolY, 4, 14, toolPaint);
                toolPaint.Color = new SKColor(0xF5, 0xCB, 0xA7);
                canvas.DrawRect(toolX + 7, toolY + 14, 2, 3, toolPaint);
                break;

            case WorkshopType.GeneralContractor:
                // Goldener Schlüssel
                toolPaint.Color = new SKColor(0xFF, 0xD7, 0x00);
                canvas.DrawCircle(toolX + 8, toolY + 6, 5, toolPaint);
                toolPaint.Color = new SKColor(0x60, 0x50, 0x45);
                canvas.DrawCircle(toolX + 8, toolY + 6, 2, toolPaint);
                toolPaint.Color = new SKColor(0xFF, 0xD7, 0x00);
                canvas.DrawRect(toolX + 7, toolY + 11, 3, 10, toolPaint);
                canvas.DrawRect(toolX + 10, toolY + 15, 3, 2, toolPaint);
                canvas.DrawRect(toolX + 10, toolY + 19, 3, 2, toolPaint);
                toolY += 28;
                // Aktenordner
                toolPaint.Color = new SKColor(0x78, 0x71, 0x6C);
                canvas.DrawRect(toolX + 2, toolY, 12, 14, toolPaint);
                toolPaint.Color = new SKColor(0xEE, 0xEE, 0xEE);
                canvas.DrawRect(toolX + 4, toolY + 2, 8, 10, toolPaint);
                toolPaint.Color = new SKColor(0x78, 0x71, 0x6C);
                canvas.DrawRect(toolX + 5, toolY + 4, 6, 1, toolPaint);
                canvas.DrawRect(toolX + 5, toolY + 6, 6, 1, toolPaint);
                canvas.DrawRect(toolX + 5, toolY + 8, 4, 1, toolPaint);
                break;
        }
    }

    /// <summary>
    /// Zeichnet typ-spezifische Dekorations-Elemente in der Werkstatt.
    /// </summary>
    private void DrawTypSpecificDecor(SKCanvas canvas, SKRect bounds, WorkshopType type, SKColor accent)
    {
        using var decorPaint = new SKPaint { IsAntialias = false };
        float midX = bounds.MidX;

        switch (type)
        {
            case WorkshopType.Carpenter:
                // Holzstapel unten links
                decorPaint.Color = new SKColor(0x8D, 0x65, 0x34);
                for (int i = 0; i < 3; i++)
                    canvas.DrawRect(bounds.Left + 12, bounds.Bottom - 16 + i * 4, 20, 3, decorPaint);
                // Sägemehl-Häufchen
                decorPaint.Color = new SKColor(0xD2, 0xB4, 0x8C, 120);
                canvas.DrawCircle(bounds.Left + 50, bounds.Bottom - 10, 6, decorPaint);
                canvas.DrawCircle(bounds.Left + 80, bounds.Bottom - 8, 4, decorPaint);
                break;

            case WorkshopType.Plumber:
                // Rohre an der oberen Wand
                decorPaint.Color = new SKColor(0x78, 0x90, 0x9C);
                canvas.DrawRect(bounds.Left + 12, bounds.Top + 10, bounds.Width * 0.5f, 4, decorPaint);
                decorPaint.Color = new SKColor(0x0E, 0x74, 0x90);
                canvas.DrawRect(bounds.Left + 12, bounds.Top + 10, 4, 20, decorPaint);
                // Wassertropfen
                decorPaint.Color = new SKColor(0x42, 0xA5, 0xF5, 150);
                canvas.DrawCircle(bounds.Left + 16, bounds.Top + 34, 2, decorPaint);
                // Eimer unten
                decorPaint.Color = new SKColor(0x60, 0x7D, 0x8B);
                canvas.DrawRect(bounds.Left + 12, bounds.Bottom - 18, 14, 14, decorPaint);
                decorPaint.Color = new SKColor(0x42, 0xA5, 0xF5, 100);
                canvas.DrawRect(bounds.Left + 14, bounds.Bottom - 14, 10, 8, decorPaint);
                break;

            case WorkshopType.Electrician:
                // Sicherungskasten an der oberen Wand
                decorPaint.Color = new SKColor(0x42, 0x42, 0x42);
                canvas.DrawRect(bounds.Left + 14, bounds.Top + 10, 28, 20, decorPaint);
                decorPaint.Color = new SKColor(0x21, 0x21, 0x21);
                canvas.DrawRect(bounds.Left + 16, bounds.Top + 12, 24, 16, decorPaint);
                // Schalter-Reihe
                decorPaint.Color = new SKColor(0x66, 0xBB, 0x6A); // Grün = an
                for (int i = 0; i < 4; i++)
                    canvas.DrawRect(bounds.Left + 18 + i * 6, bounds.Top + 14, 4, 6, decorPaint);
                decorPaint.Color = new SKColor(0xF4, 0x43, 0x36); // Rot = aus
                canvas.DrawRect(bounds.Left + 18 + 4 * 6, bounds.Top + 14, 4, 6, decorPaint);
                // Kabel am Boden
                decorPaint.Color = new SKColor(0xF9, 0x73, 0x16, 100);
                canvas.DrawLine(bounds.Left + 60, bounds.Bottom - 6, bounds.Right - 30, bounds.Bottom - 10, decorPaint);
                break;

            case WorkshopType.Painter:
                // Staffelei
                decorPaint.Color = new SKColor(0x5D, 0x40, 0x37);
                float eX = bounds.Left + 14;
                float eY = bounds.Bottom - 40;
                canvas.DrawLine(eX + 6, eY, eX, eY + 36, decorPaint);
                canvas.DrawLine(eX + 6, eY, eX + 12, eY + 36, decorPaint);
                // Leinwand
                decorPaint.Color = new SKColor(0xF5, 0xF5, 0xF5);
                canvas.DrawRect(eX + 1, eY + 4, 10, 14, decorPaint);
                // Farbklecks auf Leinwand
                decorPaint.Color = new SKColor(0xEC, 0x48, 0x99, 180);
                canvas.DrawCircle(eX + 5, eY + 10, 3, decorPaint);
                decorPaint.Color = new SKColor(0x42, 0xA5, 0xF5, 150);
                canvas.DrawCircle(eX + 8, eY + 12, 2, decorPaint);
                break;

            case WorkshopType.Roofer:
                // Dachziegel-Stapel
                decorPaint.Color = new SKColor(0xDC, 0x26, 0x26);
                for (int i = 0; i < 4; i++)
                {
                    canvas.DrawRect(bounds.Left + 12, bounds.Bottom - 20 + i * 4, 16, 3, decorPaint);
                }
                decorPaint.Color = new SKColor(0xBF, 0x20, 0x20);
                for (int i = 0; i < 4; i++)
                    canvas.DrawLine(bounds.Left + 12, bounds.Bottom - 18 + i * 4, bounds.Left + 28, bounds.Bottom - 18 + i * 4, decorPaint);
                break;

            case WorkshopType.Contractor:
                // Zementsäcke
                decorPaint.Color = new SKColor(0x9E, 0x9E, 0x9E);
                canvas.DrawRect(bounds.Left + 12, bounds.Bottom - 22, 18, 10, decorPaint);
                canvas.DrawRect(bounds.Left + 14, bounds.Bottom - 14, 18, 10, decorPaint);
                decorPaint.Color = new SKColor(0x75, 0x75, 0x75);
                using (var font = new SKFont(SKTypeface.Default, 6))
                    canvas.DrawText("ZMT", bounds.Left + 15, bounds.Bottom - 15, SKTextAlign.Left, font, decorPaint);
                break;

            case WorkshopType.Architect:
                // Zeichenbrett
                decorPaint.Color = new SKColor(0xEE, 0xEE, 0xEE);
                canvas.DrawRect(bounds.Left + 12, bounds.Top + 12, 30, 22, decorPaint);
                decorPaint.Color = new SKColor(0x42, 0xA5, 0xF5, 80);
                // Grundriss-Linien
                canvas.DrawRect(bounds.Left + 16, bounds.Top + 16, 12, 8, decorPaint);
                canvas.DrawRect(bounds.Left + 30, bounds.Top + 16, 8, 14, decorPaint);
                canvas.DrawRect(bounds.Left + 16, bounds.Top + 26, 6, 6, decorPaint);
                break;

            case WorkshopType.GeneralContractor:
                // Goldene Plakette/Auszeichnung an der Wand
                decorPaint.Color = new SKColor(0xFF, 0xD7, 0x00, 200);
                canvas.DrawCircle(bounds.Left + 30, bounds.Top + 22, 8, decorPaint);
                decorPaint.Color = new SKColor(0xFF, 0xB3, 0x00);
                canvas.DrawCircle(bounds.Left + 30, bounds.Top + 22, 5, decorPaint);
                // Stern in der Mitte
                decorPaint.Color = new SKColor(0xFF, 0xD7, 0x00);
                canvas.DrawRect(bounds.Left + 28, bounds.Top + 20, 4, 4, decorPaint);
                break;
        }
    }

    private void DrawWorkbenches(SKCanvas canvas, SKRect bounds, Workshop workshop, SKColor accent)
    {
        int benchCount = workshop.MaxWorkers;
        if (benchCount == 0) benchCount = 1;

        float workArea = bounds.Width - 40; // Links: 8 Wand + Rechts: 24 Werkzeugwand + 8 Abstand
        float workTop = bounds.Top + 16;
        float workHeight = bounds.Height - 24;

        // Bänke in 2 Reihen verteilen
        int cols = Math.Max(1, (int)Math.Ceiling(benchCount / 2.0));
        int rows = benchCount <= cols ? 1 : 2;
        float benchW = Math.Min(28, (workArea - (cols + 1) * 6) / cols);
        float benchH = Math.Min(20, (workHeight - (rows + 1) * 8) / rows);

        // Werkbank-Farbe: Basis-Braun mit leichtem Typ-Akzent
        var benchColor = new SKColor(0x6D, 0x4C, 0x41);
        var benchTopColor = new SKColor(
            (byte)((0x8D + accent.Red) / 2),
            (byte)((0x6E + accent.Green) / 2),
            (byte)((0x63 + accent.Blue) / 2));

        using (var benchPaint = new SKPaint { Color = benchColor, IsAntialias = false })
        using (var benchTopPaint = new SKPaint { Color = benchTopColor, IsAntialias = false })
        {
            for (int i = 0; i < benchCount; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float bx = bounds.Left + 16 + col * (benchW + 6);
                float by = workTop + row * (benchH + workHeight / 2);

                // Tisch-Körper
                canvas.DrawRect(bx, by, benchW, benchH, benchPaint);
                // Tisch-Oberfläche (typ-akzentuierter Streifen oben)
                canvas.DrawRect(bx, by, benchW, 3, benchTopPaint);
            }
        }
    }

    private void DrawWorkers(SKCanvas canvas, SKRect bounds, Workshop workshop)
    {
        if (workshop.Workers.Count == 0) return;

        int benchCount = workshop.MaxWorkers;
        if (benchCount == 0) benchCount = 1;
        int cols = Math.Max(1, (int)Math.Ceiling(benchCount / 2.0));

        float workArea = bounds.Width - 40;
        float workTop = bounds.Top + 16;
        float workHeight = bounds.Height - 24;
        float benchW = Math.Min(28, (workArea - (cols + 1) * 6) / cols);
        float benchH = Math.Min(20, (workHeight - 3 * 8) / 2);

        for (int i = 0; i < workshop.Workers.Count; i++)
        {
            var worker = workshop.Workers[i];
            int col = i % cols;
            int row = i / cols;

            float bx = bounds.Left + 16 + col * (benchW + 6);
            float by = workTop + row * (benchH + workHeight / 2);

            // Worker-Position: unterhalb des Tischs
            float workerX = bx + benchW / 2;
            float workerY = by + benchH + 10;
            float radius = 6;

            // Worker-Kreis in Tier-Farbe
            var tierColor = TierColors.GetValueOrDefault(worker.Tier, new SKColor(0x90, 0x90, 0x90));
            using (var workerPaint = new SKPaint { Color = tierColor, IsAntialias = false })
            {
                canvas.DrawCircle(workerX, workerY, radius, workerPaint);
            }

            // Status-Anzeige
            if (worker.IsResting)
            {
                // "Zzz" Text
                using var zzzPaint = new SKPaint { Color = new SKColor(0x64, 0xB5, 0xF6), IsAntialias = false };
                using var zzzFont = new SKFont(SKTypeface.Default, 8);
                canvas.DrawText("Zzz", workerX + radius + 2, workerY - 2, SKTextAlign.Left, zzzFont, zzzPaint);
            }
            else if (worker.IsTraining)
            {
                // Buch-Symbol
                using var bookPaint = new SKPaint { Color = new SKColor(0x42, 0xA5, 0xF5), IsAntialias = false };
                canvas.DrawRect(workerX + radius + 2, workerY - 4, 6, 8, bookPaint);
                using var spinePaint = new SKPaint { Color = new SKColor(0x1E, 0x88, 0xE5), IsAntialias = false };
                canvas.DrawRect(workerX + radius + 2, workerY - 4, 2, 8, spinePaint);
            }

            // Mood-Indikator (kleiner Punkt rechts oben am Worker)
            var moodColor = GetMoodColor(worker.Mood);
            using (var moodPaint = new SKPaint { Color = moodColor, IsAntialias = false })
            {
                canvas.DrawCircle(workerX + radius - 1, workerY - radius + 1, 3, moodPaint);
            }
        }
    }

    /// <summary>
    /// Grün/Gelb/Rot basierend auf Stimmungswert.
    /// </summary>
    private static SKColor GetMoodColor(decimal mood)
    {
        if (mood >= 70) return new SKColor(0x4C, 0xAF, 0x50); // Grün (zufrieden)
        if (mood >= 40) return new SKColor(0xFF, 0xC1, 0x07); // Gelb (neutral)
        return new SKColor(0xF4, 0x43, 0x36);                  // Rot (unzufrieden)
    }
}

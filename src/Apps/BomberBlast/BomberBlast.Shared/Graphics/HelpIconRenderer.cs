using BomberBlast.Models.Entities;
using SkiaSharp;

namespace BomberBlast.Graphics;

/// <summary>
/// Statische Render-Methoden für Gegner- und PowerUp-Icons in der HelpView.
/// Gleiche Farben, Formen und Proportionen wie GameRenderer (ohne Animationen).
/// </summary>
public static class HelpIconRenderer
{
    /// <summary>
    /// Zeichnet einen Gegner (statisch, ohne Wobble/Blink) - identisch zum Spiel.
    /// </summary>
    public static void DrawEnemy(SKCanvas canvas, float cx, float cy, float size, EnemyType type)
    {
        var (r, g, b) = type.GetColor();
        var bodyColor = new SKColor(r, g, b);

        float bodyW = size * 0.6f;
        float bodyH = size * 0.65f;

        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };

        // Ovaler Körper
        fillPaint.Color = bodyColor;
        canvas.DrawOval(cx, cy, bodyW * 0.45f, bodyH * 0.45f, fillPaint);

        // Weiße Augen
        float eyeY = cy - bodyH * 0.08f;
        float eyeSpacing = bodyW * 0.2f;
        float eyeR = size * 0.09f;
        float pupilR = size * 0.045f;

        fillPaint.Color = SKColors.White;
        canvas.DrawCircle(cx - eyeSpacing, eyeY, eyeR, fillPaint);
        canvas.DrawCircle(cx + eyeSpacing, eyeY, eyeR, fillPaint);

        // Schwarze Pupillen (mittig, keine Richtung)
        fillPaint.Color = SKColors.Black;
        canvas.DrawCircle(cx - eyeSpacing, eyeY, pupilR, fillPaint);
        canvas.DrawCircle(cx + eyeSpacing, eyeY, pupilR, fillPaint);

        // Böse Augenbrauen
        strokePaint.Color = new SKColor(40, 0, 0);
        strokePaint.StrokeWidth = size * 0.045f;
        float browY = eyeY - eyeR - size * 0.045f;
        float browLen = size * 0.06f;
        canvas.DrawLine(cx - eyeSpacing - browLen, browY - size * 0.03f,
                        cx - eyeSpacing + browLen, browY + size * 0.03f, strokePaint);
        canvas.DrawLine(cx + eyeSpacing + browLen, browY - size * 0.03f,
                        cx + eyeSpacing - browLen, browY + size * 0.03f, strokePaint);

        // Mund (variiert nach Gegnertyp)
        float mouthY = cy + bodyH * 0.15f;
        strokePaint.Color = new SKColor(60, 0, 0);
        strokePaint.StrokeWidth = size * 0.036f;
        float mouthW = size * 0.09f;

        if ((int)type % 3 == 0)
        {
            // Zahniges Grinsen
            canvas.DrawLine(cx - mouthW, mouthY, cx + mouthW, mouthY, strokePaint);
            fillPaint.Color = SKColors.White;
            float toothW = size * 0.045f;
            float toothH = size * 0.06f;
            canvas.DrawRect(cx - toothW * 1.3f, mouthY - toothH / 2, toothW, toothH, fillPaint);
            canvas.DrawRect(cx + toothW * 0.3f, mouthY - toothH / 2, toothW, toothH, fillPaint);
        }
        else if ((int)type % 3 == 1)
        {
            // Unzufriedener Mund (Kurve)
            using var path = new SKPath();
            path.MoveTo(cx - mouthW, mouthY);
            path.QuadTo(cx, mouthY + size * 0.09f, cx + mouthW, mouthY);
            canvas.DrawPath(path, strokePaint);
        }
        else
        {
            // Einfache Linie
            canvas.DrawLine(cx - mouthW, mouthY, cx + mouthW, mouthY, strokePaint);
        }
    }

    /// <summary>
    /// Zeichnet ein PowerUp (statisch, ohne Bobbing/Blink) - identisch zum Spiel.
    /// </summary>
    public static void DrawPowerUp(SKCanvas canvas, float cx, float cy, float size, PowerUpType type)
    {
        var color = GetPowerUpColor(type);
        float radius = size * 0.35f;

        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        using var strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke };

        // Farbiger Kreis-Hintergrund
        fillPaint.Color = color;
        canvas.DrawCircle(cx, cy, radius, fillPaint);

        // Weißes Icon
        fillPaint.Color = SKColors.White;
        DrawPowerUpIcon(canvas, type, cx, cy, radius * 0.6f, fillPaint, strokePaint);
    }

    /// <summary>
    /// Zeichnet das PowerUp-Icon (gleiche Logik wie GameRenderer.RenderPowerUpIcon).
    /// </summary>
    private static void DrawPowerUpIcon(SKCanvas canvas, PowerUpType type,
        float cx, float cy, float size, SKPaint fillPaint, SKPaint strokePaint)
    {
        strokePaint.Color = SKColors.White;
        strokePaint.StrokeWidth = size * 0.18f;

        switch (type)
        {
            case PowerUpType.BombUp:
                // Kleine Bombe
                canvas.DrawCircle(cx, cy + size * 0.08f, size * 0.5f, fillPaint);
                canvas.DrawLine(cx, cy - size * 0.3f, cx + size * 0.3f, cy - size * 0.6f, strokePaint);
                break;

            case PowerUpType.Fire:
                // Flammenform (Dreieck)
                using (var path = new SKPath())
                {
                    path.MoveTo(cx, cy - size * 0.7f);
                    path.LineTo(cx + size * 0.4f, cy + size * 0.5f);
                    path.LineTo(cx - size * 0.4f, cy + size * 0.5f);
                    path.Close();
                    canvas.DrawPath(path, fillPaint);
                }
                break;

            case PowerUpType.Speed:
                // Pfeil nach rechts
                using (var path = new SKPath())
                {
                    path.MoveTo(cx - size * 0.4f, cy - size * 0.3f);
                    path.LineTo(cx + size * 0.5f, cy);
                    path.LineTo(cx - size * 0.4f, cy + size * 0.3f);
                    path.Close();
                    canvas.DrawPath(path, fillPaint);
                }
                break;

            case PowerUpType.Wallpass:
                // Ghost-Form
                canvas.DrawCircle(cx, cy - size * 0.15f, size * 0.35f, fillPaint);
                canvas.DrawRect(cx - size * 0.35f, cy, size * 0.7f, size * 0.3f, fillPaint);
                break;

            case PowerUpType.Detonator:
                // Blitz
                using (var path = new SKPath())
                {
                    path.MoveTo(cx + size * 0.15f, cy - size * 0.6f);
                    path.LineTo(cx - size * 0.2f, cy + size * 0.05f);
                    path.LineTo(cx + size * 0.1f, cy + size * 0.05f);
                    path.LineTo(cx - size * 0.15f, cy + size * 0.6f);
                    canvas.DrawPath(path, strokePaint);
                }
                break;

            case PowerUpType.Bombpass:
                // Kreis mit Pfeil
                strokePaint.StrokeWidth = size * 0.14f;
                canvas.DrawCircle(cx, cy, size * 0.4f, strokePaint);
                canvas.DrawLine(cx - size * 0.6f, cy, cx + size * 0.6f, cy, strokePaint);
                break;

            case PowerUpType.Flamepass:
                // Schildform
                using (var path = new SKPath())
                {
                    path.MoveTo(cx, cy - size * 0.5f);
                    path.LineTo(cx + size * 0.4f, cy - size * 0.2f);
                    path.LineTo(cx + size * 0.3f, cy + size * 0.4f);
                    path.LineTo(cx, cy + size * 0.6f);
                    path.LineTo(cx - size * 0.3f, cy + size * 0.4f);
                    path.LineTo(cx - size * 0.4f, cy - size * 0.2f);
                    path.Close();
                    canvas.DrawPath(path, fillPaint);
                }
                break;

            case PowerUpType.Mystery:
                // Fragezeichen
                using (var font = new SKFont { Size = size * 1.4f })
                using (var textPaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
                {
                    canvas.DrawText("?", cx, cy + size * 0.25f, SKTextAlign.Center, font, textPaint);
                }
                break;

            case PowerUpType.Kick:
                // Schuh/Boot + Bombe
                using (var path = new SKPath())
                {
                    path.MoveTo(cx - size * 0.5f, cy - size * 0.3f);
                    path.LineTo(cx + size * 0.5f, cy);
                    path.LineTo(cx - size * 0.5f, cy + size * 0.3f);
                    path.Close();
                    canvas.DrawPath(path, fillPaint);
                }
                canvas.DrawCircle(cx + size * 0.3f, cy - size * 0.4f, size * 0.2f, fillPaint);
                break;

            case PowerUpType.LineBomb:
                // Drei Kreise in einer Reihe
                canvas.DrawCircle(cx - size * 0.4f, cy, size * 0.2f, fillPaint);
                canvas.DrawCircle(cx, cy, size * 0.2f, fillPaint);
                canvas.DrawCircle(cx + size * 0.4f, cy, size * 0.2f, fillPaint);
                break;

            case PowerUpType.PowerBomb:
                // Großer Kreis mit Stern
                canvas.DrawCircle(cx, cy, size * 0.4f, fillPaint);
                using (var starPaint = new SKPaint
                       {
                           IsAntialias = true, Style = SKPaintStyle.Stroke,
                           Color = new SKColor(255, 255, 100), StrokeWidth = size * 0.18f
                       })
                {
                    canvas.DrawLine(cx, cy - size * 0.3f, cx, cy + size * 0.3f, starPaint);
                    canvas.DrawLine(cx - size * 0.3f, cy, cx + size * 0.3f, cy, starPaint);
                }
                break;

            case PowerUpType.Skull:
                // Totenkopf (Kreis + Augenhöhlen + Kiefer)
                canvas.DrawCircle(cx, cy - size * 0.1f, size * 0.4f, fillPaint);
                fillPaint.Color = SKColors.Black;
                canvas.DrawCircle(cx - size * 0.15f, cy - size * 0.15f, size * 0.12f, fillPaint);
                canvas.DrawCircle(cx + size * 0.15f, cy - size * 0.15f, size * 0.12f, fillPaint);
                canvas.DrawRect(cx - size * 0.2f, cy + size * 0.15f, size * 0.4f, size * 0.08f, fillPaint);
                fillPaint.Color = SKColors.White;
                break;

            default:
                using (var font = new SKFont { Size = size * 1.4f })
                using (var textPaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
                {
                    canvas.DrawText("?", cx, cy + size * 0.25f, SKTextAlign.Center, font, textPaint);
                }
                break;
        }
    }

    /// <summary>
    /// Farbe pro PowerUp-Typ (identisch zu GameRenderer.GetPowerUpColor).
    /// </summary>
    private static SKColor GetPowerUpColor(PowerUpType type) => type switch
    {
        PowerUpType.BombUp => new SKColor(80, 80, 240),
        PowerUpType.Fire => new SKColor(240, 90, 40),
        PowerUpType.Speed => new SKColor(60, 220, 80),
        PowerUpType.Wallpass => new SKColor(150, 100, 50),
        PowerUpType.Detonator => new SKColor(240, 40, 40),
        PowerUpType.Bombpass => new SKColor(50, 50, 150),
        PowerUpType.Flamepass => new SKColor(240, 190, 40),
        PowerUpType.Mystery => new SKColor(180, 80, 240),
        PowerUpType.Kick => new SKColor(255, 165, 0),
        PowerUpType.LineBomb => new SKColor(0, 180, 255),
        PowerUpType.PowerBomb => new SKColor(255, 50, 50),
        PowerUpType.Skull => new SKColor(100, 0, 100),
        _ => SKColors.White
    };
}

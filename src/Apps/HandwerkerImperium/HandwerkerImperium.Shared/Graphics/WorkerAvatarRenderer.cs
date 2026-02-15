using SkiaSharp;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// Generates deterministic pixel-art worker avatars from a seed string.
/// Caches bitmaps by id+tier+mood_bucket to avoid re-rendering every frame.
/// Caller is responsible for disposing returned SKBitmaps.
/// </summary>
public class WorkerAvatarRenderer
{
    // 6 Hauttoene
    private static readonly SKColor[] SkinTones =
    [
        new SKColor(0xFF, 0xDB, 0xAC), // Light
        new SKColor(0xF1, 0xC2, 0x7D), // Fair
        new SKColor(0xE0, 0xAC, 0x69), // Medium
        new SKColor(0xC6, 0x8C, 0x53), // Tan
        new SKColor(0x8D, 0x5E, 0x3C), // Brown
        new SKColor(0x6E, 0x40, 0x20)  // Dark
    ];

    // Tier -> Helm/Hut-Farbe (alle 10 Tiers)
    private static readonly Dictionary<WorkerTier, SKColor> TierHatColors = new()
    {
        { WorkerTier.F, new SKColor(0x9E, 0x9E, 0x9E) },          // Grey
        { WorkerTier.E, new SKColor(0x4C, 0xAF, 0x50) },          // Green
        { WorkerTier.D, new SKColor(0x21, 0x96, 0xF3) },          // Blue
        { WorkerTier.C, new SKColor(0x9C, 0x27, 0xB0) },          // Purple
        { WorkerTier.B, new SKColor(0xFF, 0xC1, 0x07) },          // Gold
        { WorkerTier.A, new SKColor(0xF4, 0x43, 0x36) },          // Red
        { WorkerTier.S, new SKColor(0xFF, 0x98, 0x00) },          // Orange
        { WorkerTier.SS, new SKColor(0xE0, 0x40, 0xFB) },         // Pink
        { WorkerTier.SSS, new SKColor(0x7C, 0x4D, 0xFF) },        // DeepPurple
        { WorkerTier.Legendary, new SKColor(0xFF, 0xD7, 0x00) }   // Gold (glaenzend)
    };

    private enum MoodBucket { High, Mid, Low }

    // Cache: "id|tier|moodBucket" -> weak reference (bitmap kann GC'd werden)
    private static readonly Dictionary<string, WeakReference<SKBitmap>> _cache = new();
    private static readonly object _cacheLock = new();

    // Haarfarben fuer weibliche/maennliche Worker
    private static readonly SKColor[] HairColors =
    [
        new SKColor(0x3E, 0x27, 0x23), // Dunkelbraun
        new SKColor(0x5D, 0x40, 0x37), // Mittelbraun
        new SKColor(0x79, 0x55, 0x48), // Hellbraun
        new SKColor(0x21, 0x21, 0x21), // Schwarz
        new SKColor(0xBF, 0x36, 0x0C), // Rot
        new SKColor(0xF9, 0xA8, 0x25)  // Blond
    ];

    /// <summary>
    /// Renders a deterministic pixel-art avatar for the given worker parameters.
    /// Returns a new SKBitmap that the caller must dispose.
    /// </summary>
    /// <param name="idSeed">Worker ID used as seed for deterministic generation.</param>
    /// <param name="tier">Worker tier (determines hat color).</param>
    /// <param name="mood">Worker mood (0-100, determines expression).</param>
    /// <param name="size">Output size in pixels (32, 64, or 128).</param>
    /// <param name="isFemale">Geschlecht: true = weiblich (laengere Haare, Lippen), false = maennlich (breiterer Kiefer).</param>
    public static SKBitmap RenderAvatar(string idSeed, WorkerTier tier, decimal mood, int size, bool isFemale = false)
    {
        // Groesse auf erlaubte Werte begrenzen
        size = size switch
        {
            <= 32 => 32,
            <= 64 => 64,
            _ => 128
        };

        var moodBucket = GetMoodBucket(mood);
        string cacheKey = $"{idSeed}|{tier}|{moodBucket}|{size}|{(isFemale ? "f" : "m")}";

        // Cache pruefen
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var weakRef) &&
                weakRef.TryGetTarget(out var cached))
            {
                // Kopie zurueckgeben (Caller verwaltet Disposal)
                return cached.Copy();
            }
        }

        // Neues Bitmap erzeugen
        var bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);

            int hash = GetStableHash(idSeed);
            float scale = size / 32f;

            DrawHead(canvas, hash, scale, isFemale);
            DrawHair(canvas, hash, scale, isFemale);
            DrawHat(canvas, tier, scale);
            DrawEyes(canvas, hash, moodBucket, scale);
            DrawMouth(canvas, moodBucket, scale, isFemale);
        }

        // Im Cache speichern (als weak reference)
        lock (_cacheLock)
        {
            _cache[cacheKey] = new WeakReference<SKBitmap>(bitmap);

            // Cache-Groesse begrenzen
            if (_cache.Count > 200)
            {
                PruneCache();
            }
        }

        // Kopie zurueckgeben
        return bitmap.Copy();
    }

    private static void DrawHead(SKCanvas canvas, int hash, float scale, bool isFemale)
    {
        // Hautton aus Hash ableiten
        int skinIndex = Math.Abs(hash) % SkinTones.Length;
        var skinColor = SkinTones[skinIndex];

        using (var headPaint = new SKPaint { Color = skinColor, IsAntialias = false })
        {
            float cx = 16 * scale;
            float cy = 18 * scale;
            float radius = 10 * scale;
            canvas.DrawCircle(cx, cy, radius, headPaint);

            // Maennlich: Breiterer Kiefer (1 Pixel mehr an den Seiten unten)
            if (!isFemale)
            {
                float jawWidth = 1 * scale;
                float jawTop = 20 * scale;
                float jawHeight = 5 * scale;
                canvas.DrawRect(cx - radius - jawWidth, jawTop, jawWidth, jawHeight, headPaint);
                canvas.DrawRect(cx + radius, jawTop, jawWidth, jawHeight, headPaint);
            }
        }

        // Ohren
        using (var earPaint = new SKPaint { Color = SkinTones[Math.Abs(hash) % SkinTones.Length], IsAntialias = false })
        {
            float earRadius = 2.5f * scale;
            canvas.DrawCircle(5 * scale, 18 * scale, earRadius, earPaint);
            canvas.DrawCircle(27 * scale, 18 * scale, earRadius, earPaint);
        }
    }

    /// <summary>
    /// Zeichnet geschlechtsspezifische Haare.
    /// Weiblich: Laengere Straehnen links und rechts (2-3 Pixel unter dem Helm).
    /// Maennlich: Kurzhaar (dezente Pixel oben am Kopf unter dem Helm).
    /// </summary>
    private static void DrawHair(SKCanvas canvas, int hash, float scale, bool isFemale)
    {
        int hairIndex = Math.Abs(hash / 7) % HairColors.Length;
        var hairColor = HairColors[hairIndex];

        using var hairPaint = new SKPaint { Color = hairColor, IsAntialias = false };

        if (isFemale)
        {
            // Laengere Haar-Straehnen links und rechts vom Kopf
            float strandWidth = 2 * scale;
            float strandHeight = 8 * scale;
            float topY = 14 * scale;

            // Linke Straehne
            canvas.DrawRect(5 * scale, topY, strandWidth, strandHeight, hairPaint);
            canvas.DrawRect(3 * scale, topY + 1 * scale, strandWidth, strandHeight - 2 * scale, hairPaint);

            // Rechte Straehne
            canvas.DrawRect(25 * scale, topY, strandWidth, strandHeight, hairPaint);
            canvas.DrawRect(27 * scale, topY + 1 * scale, strandWidth, strandHeight - 2 * scale, hairPaint);
        }
        else
        {
            // Kurzhaar: Dezente Pixel oben am Kopf (unter dem Helm sichtbar)
            float hairTop = 13 * scale;
            float hairHeight = 2 * scale;

            // Links und rechts kurze Haar-Ansaetze
            canvas.DrawRect(7 * scale, hairTop, 3 * scale, hairHeight, hairPaint);
            canvas.DrawRect(22 * scale, hairTop, 3 * scale, hairHeight, hairPaint);
        }
    }

    private static void DrawHat(SKCanvas canvas, WorkerTier tier, float scale)
    {
        var hatColor = TierHatColors.GetValueOrDefault(tier, new SKColor(0x90, 0x90, 0x90));

        using (var hatPaint = new SKPaint { Color = hatColor, IsAntialias = false })
        {
            // Helm-Koerper (Halbkreis oben auf dem Kopf)
            float left = 8 * scale;
            float top = 5 * scale;
            float width = 16 * scale;
            float height = 10 * scale;
            canvas.DrawRect(left, top, width, height, hatPaint);

            // Helm-Krempe (breiterer Streifen)
            var brimColor = DarkenColor(hatColor, 0.2f);
            using (var brimPaint = new SKPaint { Color = brimColor, IsAntialias = false })
            {
                canvas.DrawRect((left - 2 * scale), (top + height - 2 * scale), (width + 4 * scale), 3 * scale, brimPaint);
            }
        }

        // S+ Tiers: Stern-Markierung auf dem Helm
        if (tier >= WorkerTier.S)
        {
            using (var starPaint = new SKPaint { Color = SKColors.White, IsAntialias = false })
            {
                float sx = 16 * scale;
                float sy = 9 * scale;
                float starSize = 2 * scale;
                canvas.DrawRect(sx - starSize / 2, sy - starSize / 2, starSize, starSize, starPaint);

                // SS+: Zweiter Stern rechts daneben
                if (tier >= WorkerTier.SS)
                {
                    canvas.DrawRect(sx + 2 * scale, sy - starSize / 2, starSize, starSize, starPaint);
                }

                // Legendary: Dritter Stern links
                if (tier == WorkerTier.Legendary)
                {
                    canvas.DrawRect(sx - 4 * scale, sy - starSize / 2, starSize, starSize, starPaint);
                }
            }
        }
    }

    private static void DrawEyes(SKCanvas canvas, int hash, MoodBucket mood, float scale)
    {
        float eyeY = 17 * scale;
        float leftEyeX = 13 * scale;
        float rightEyeX = 19 * scale;

        // Augenfarbe aus Hash
        bool hasBrownEyes = (hash % 3) == 0;
        var eyeColor = hasBrownEyes ? new SKColor(0x5D, 0x40, 0x37) : new SKColor(0x21, 0x21, 0x21);

        using (var eyePaint = new SKPaint { Color = eyeColor, IsAntialias = false })
        {
            float dotSize = 2 * scale;

            switch (mood)
            {
                case MoodBucket.High:
                    // Froehlich: einfache Punkte
                    canvas.DrawCircle(leftEyeX, eyeY, dotSize, eyePaint);
                    canvas.DrawCircle(rightEyeX, eyeY, dotSize, eyePaint);
                    break;

                case MoodBucket.Mid:
                    // Neutral: etwas groessere Punkte
                    canvas.DrawCircle(leftEyeX, eyeY, dotSize * 1.2f, eyePaint);
                    canvas.DrawCircle(rightEyeX, eyeY, dotSize * 1.2f, eyePaint);
                    break;

                case MoodBucket.Low:
                    // Traurig: Dreiecke (nach unten zeigend)
                    DrawSadEye(canvas, leftEyeX, eyeY, dotSize, eyePaint);
                    DrawSadEye(canvas, rightEyeX, eyeY, dotSize, eyePaint);
                    break;
            }
        }
    }

    private static void DrawSadEye(SKCanvas canvas, float cx, float cy, float size, SKPaint paint)
    {
        // Dreieck: nach unten zeigend fuer traurigen Ausdruck
        using var path = new SKPath();
        path.MoveTo(cx - size, cy - size);
        path.LineTo(cx + size, cy - size);
        path.LineTo(cx, cy + size);
        path.Close();
        canvas.DrawPath(path, paint);
    }

    private static void DrawMouth(SKCanvas canvas, MoodBucket mood, float scale, bool isFemale = false)
    {
        float mouthY = 22 * scale;
        float cx = 16 * scale;

        // Weiblich: Dezent rosafarbene Lippen
        var mouthColor = isFemale
            ? new SKColor(0xE0, 0x6B, 0x7A)   // Rosa
            : new SKColor(0x5D, 0x40, 0x37);   // Braun (Standard)

        using var mouthPaint = new SKPaint
        {
            Color = mouthColor,
            IsAntialias = false,
            StrokeWidth = Math.Max(1, scale * (isFemale ? 1.2f : 1f)),
            Style = SKPaintStyle.Stroke
        };

        float halfWidth = 3 * scale;

        switch (mood)
        {
            case MoodBucket.High:
                // Laecheln (Bogen nach oben)
                canvas.DrawLine(cx - halfWidth, mouthY, cx, mouthY + 2 * scale, mouthPaint);
                canvas.DrawLine(cx, mouthY + 2 * scale, cx + halfWidth, mouthY, mouthPaint);
                break;

            case MoodBucket.Mid:
                // Neutral (gerade Linie)
                canvas.DrawLine(cx - halfWidth, mouthY, cx + halfWidth, mouthY, mouthPaint);
                break;

            case MoodBucket.Low:
                // Traurig (Bogen nach unten)
                canvas.DrawLine(cx - halfWidth, mouthY + 2 * scale, cx, mouthY, mouthPaint);
                canvas.DrawLine(cx, mouthY, cx + halfWidth, mouthY + 2 * scale, mouthPaint);
                break;
        }
    }

    private static MoodBucket GetMoodBucket(decimal mood)
    {
        if (mood >= 65) return MoodBucket.High;
        if (mood >= 35) return MoodBucket.Mid;
        return MoodBucket.Low;
    }

    /// <summary>
    /// Stable hash from string (deterministic, not GetHashCode which varies per runtime).
    /// </summary>
    private static int GetStableHash(string input)
    {
        if (string.IsNullOrEmpty(input)) return 0;

        unchecked
        {
            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }

    private static SKColor DarkenColor(SKColor color, float amount)
    {
        float factor = 1.0f - amount;
        return new SKColor(
            (byte)(color.Red * factor),
            (byte)(color.Green * factor),
            (byte)(color.Blue * factor),
            color.Alpha);
    }

    /// <summary>
    /// Removes expired weak references from the cache.
    /// </summary>
    private static void PruneCache()
    {
        var deadKeys = new List<string>();
        foreach (var kvp in _cache)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                deadKeys.Add(kvp.Key);
            }
        }
        foreach (var key in deadKeys)
        {
            _cache.Remove(key);
        }
    }
}

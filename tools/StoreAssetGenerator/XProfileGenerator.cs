using SkiaSharp;

namespace StoreAssetGenerator;

/// <summary>
/// Generiert X/Twitter Profil-Assets für RS-Digital:
/// - Profilbild (400x400) - Schild/Badge mit "RS" Monogramm
/// - Banner (1500x500) - Marken-Banner mit Tagline
/// </summary>
static class XProfileGenerator
{
    // Midnight Indigo Palette
    static readonly SKColor DarkBg = SKColor.Parse("#0B0E1A");
    static readonly SKColor IndigoDark = SKColor.Parse("#1E1B4B");
    static readonly SKColor IndigoMid = SKColor.Parse("#312E81");
    static readonly SKColor IndigoBright = SKColor.Parse("#4F46E5");
    static readonly SKColor IndigoLight = SKColor.Parse("#6366F1");
    static readonly SKColor IndigoGlow = SKColor.Parse("#818CF8");
    static readonly SKColor White = SKColor.Parse("#F8FAFC");
    static readonly SKColor TextSub = SKColor.Parse("#CBD5E1");
    static readonly SKColor TextMuted = SKColor.Parse("#94A3B8");
    static readonly SKColor GoldAccent = SKColor.Parse("#FFD700");
    static readonly SKColor GoldDark = SKColor.Parse("#B8860B");

    public static void Generate(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        Gfx.OutputDir = outputDir;

        GenerateProfilePicture(outputDir);
        GenerateBanner(outputDir);

        Console.WriteLine($"\nX-Profil-Assets generiert in: {outputDir}");
    }

    /// <summary>
    /// 400x400 Profilbild: Schild/Badge mit "RS" Monogramm in Midnight Indigo
    /// </summary>
    static void GenerateProfilePicture(string outputDir)
    {
        const int size = 400;
        using var surface = SKSurface.Create(new SKImageInfo(size, size));
        var c = surface.Canvas;

        // Hintergrund: Tiefdunkles Indigo
        c.Clear(DarkBg);

        // Subtile Vignette
        using var vignette = new SKPaint { IsAntialias = true };
        using var vignetteShader = SKShader.CreateRadialGradient(
            new SKPoint(size / 2f, size / 2f), size * 0.7f,
            [IndigoDark, DarkBg],
            SKShaderTileMode.Clamp);
        vignette.Shader = vignetteShader;
        c.DrawRect(0, 0, size, size, vignette);

        // === Schild/Badge Form ===
        float shieldCx = size / 2f;
        float shieldTop = 40f;
        float shieldW = 260f;
        float shieldH = 320f;

        // Schild-Pfad: Oben abgerundet, unten spitz
        using var shieldPath = new SKPath();
        float cornerR = 30f;
        float halfW = shieldW / 2f;
        float left = shieldCx - halfW;
        float right = shieldCx + halfW;
        float top = shieldTop;
        float mid = shieldTop + shieldH * 0.65f;
        float bottom = shieldTop + shieldH;

        // Oben: Abgerundetes Rechteck
        shieldPath.MoveTo(left + cornerR, top);
        shieldPath.LineTo(right - cornerR, top);
        shieldPath.ArcTo(new SKRect(right - cornerR * 2, top, right, top + cornerR * 2), -90, 90, false);
        shieldPath.LineTo(right, mid);
        // Unten: Spitze
        shieldPath.LineTo(shieldCx, bottom);
        shieldPath.LineTo(left, mid);
        shieldPath.LineTo(left, top + cornerR);
        shieldPath.ArcTo(new SKRect(left, top, left + cornerR * 2, top + cornerR * 2), 180, 90, false);
        shieldPath.Close();

        // Schild-Gradient (Indigo dunkel nach mittel)
        using var shieldPaint = new SKPaint { IsAntialias = true };
        using var shieldGrad = SKShader.CreateLinearGradient(
            new SKPoint(shieldCx, top), new SKPoint(shieldCx, bottom),
            [IndigoMid, IndigoDark],
            SKShaderTileMode.Clamp);
        shieldPaint.Shader = shieldGrad;
        c.DrawPath(shieldPath, shieldPaint);

        // Schild-Rand (helles Indigo)
        using var borderPaint = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f, Color = IndigoBright
        };
        c.DrawPath(shieldPath, borderPaint);

        // Innerer Rand (subtil)
        c.Save();
        c.Scale(0.92f, 0.92f, shieldCx, shieldTop + shieldH * 0.45f);
        using var innerBorder = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f, Color = IndigoLight.WithAlpha(80)
        };
        c.DrawPath(shieldPath, innerBorder);
        c.Restore();

        // === "RS" Text ===
        using var rsPaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 120,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("RS", shieldCx, shieldTop + shieldH * 0.42f, rsPaint);

        // Trennlinie unter "RS"
        float lineY = shieldTop + shieldH * 0.48f;
        using var linePaint = new SKPaint
        {
            Color = IndigoGlow.WithAlpha(150), IsAntialias = true, StrokeWidth = 2f
        };
        c.DrawLine(shieldCx - 60, lineY, shieldCx + 60, lineY, linePaint);

        // "DIGITAL" Text
        using var digitalPaint = new SKPaint
        {
            Color = IndigoGlow, IsAntialias = true, TextSize = 32,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        // Buchstabenabstand simulieren
        var digitalText = "D I G I T A L";
        c.DrawText(digitalText, shieldCx, shieldTop + shieldH * 0.57f, digitalPaint);

        // Subtile Indigo-Akzentlinie am unteren Schild-Bereich
        using var accentLinePaint = new SKPaint
        {
            IsAntialias = true, StrokeWidth = 2f, Color = IndigoGlow.WithAlpha(80)
        };
        c.DrawLine(shieldCx - 30, shieldTop + shieldH * 0.70f, shieldCx + 30, shieldTop + shieldH * 0.70f, accentLinePaint);

        Gfx.SavePng(surface, "x_profile_400.png");
        Console.WriteLine("  Profilbild generiert: x_profile_400.png");
    }

    /// <summary>
    /// 1500x500 Banner: RS-Digital Branding mit Tagline
    /// </summary>
    static void GenerateBanner(string outputDir)
    {
        const int w = 1500;
        const int h = 500;
        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        var c = surface.Canvas;

        // Hintergrund: Gradient von dunkel nach Indigo
        using var bgPaint = new SKPaint { IsAntialias = true };
        using var bgGrad = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(w, h),
            [DarkBg, IndigoDark, IndigoMid.WithAlpha(180)],
            [0f, 0.5f, 1f],
            SKShaderTileMode.Clamp);
        bgPaint.Shader = bgGrad;
        c.DrawRect(0, 0, w, h, bgPaint);

        // Dekorative Kreise (subtil)
        using var decoPaint = new SKPaint { IsAntialias = true };

        // Großer Kreis rechts
        using var decoGrad1 = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.85f, h * 0.3f), 250,
            [IndigoBright.WithAlpha(25), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        decoPaint.Shader = decoGrad1;
        c.DrawCircle(w * 0.85f, h * 0.3f, 250, decoPaint);

        // Mittlerer Kreis links
        using var decoGrad2 = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.15f, h * 0.7f), 180,
            [IndigoBright.WithAlpha(20), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        decoPaint.Shader = decoGrad2;
        c.DrawCircle(w * 0.15f, h * 0.7f, 180, decoPaint);

        // Kleiner Akzent-Kreis
        using var decoGrad3 = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.6f, h * 0.15f), 120,
            [IndigoLight.WithAlpha(15), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        decoPaint.Shader = decoGrad3;
        c.DrawCircle(w * 0.6f, h * 0.15f, 120, decoPaint);

        // Subtile horizontale Linien als Textur
        using var linePaint = new SKPaint
        {
            Color = IndigoBright.WithAlpha(8), IsAntialias = true, StrokeWidth = 1
        };
        for (int y = 30; y < h; y += 40)
        {
            c.DrawLine(0, y, w, y, linePaint);
        }

        // === Mini-Schild links (konsistent mit Profilbild) ===
        float shieldScale = 0.35f;
        float shieldCx = 200f;
        float shieldTopY = 100f;
        DrawMiniShield(c, shieldCx, shieldTopY, shieldScale);

        // === Haupttext: "RS-Digital" ===
        float textX = 370f;
        using var namePaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 80,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("RS-Digital", textX, 220, namePaint);

        // Akzent-Linie unter dem Namen
        using var accentLine = new SKPaint
        {
            IsAntialias = true, StrokeWidth = 3f
        };
        using var lineGrad = SKShader.CreateLinearGradient(
            new SKPoint(textX, 0), new SKPoint(textX + 350, 0),
            [IndigoBright, IndigoGlow.WithAlpha(0)],
            SKShaderTileMode.Clamp);
        accentLine.Shader = lineGrad;
        c.DrawLine(textX, 235, textX + 350, 235, accentLine);

        // === Tagline ===
        using var tagPaint = new SKPaint
        {
            Color = TextSub, IsAntialias = true, TextSize = 30,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("Indie App Studio aus Deutschland", textX, 285, tagPaint);

        // === Feature-Punkte (rechte Seite) ===
        float fpX = 370f;
        float fpY = 340f;
        float fpSpacing = 46f;

        DrawFeaturePoint(c, fpX, fpY, "8 Apps", IndigoLight);
        DrawFeaturePoint(c, fpX + 220, fpY, "6 Sprachen", IndigoLight);
        DrawFeaturePoint(c, fpX + 480, fpY, "3 Plattformen", IndigoLight);

        // Plattformen kleiner darunter
        using var platformPaint = new SKPaint
        {
            Color = TextMuted, IsAntialias = true, TextSize = 20,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("Android  |  Windows  |  Linux", fpX, fpY + fpSpacing, platformPaint);

        // === Dekorative Ecken (Code-Klammern Stil) ===
        using var bracketPaint = new SKPaint
        {
            Color = IndigoBright.WithAlpha(40), IsAntialias = true,
            Style = SKPaintStyle.Stroke, StrokeWidth = 2f
        };
        // Oben links
        float bLen = 40;
        c.DrawLine(20, 20, 20, 20 + bLen, bracketPaint);
        c.DrawLine(20, 20, 20 + bLen, 20, bracketPaint);
        // Oben rechts
        c.DrawLine(w - 20, 20, w - 20, 20 + bLen, bracketPaint);
        c.DrawLine(w - 20, 20, w - 20 - bLen, 20, bracketPaint);
        // Unten links
        c.DrawLine(20, h - 20, 20, h - 20 - bLen, bracketPaint);
        c.DrawLine(20, h - 20, 20 + bLen, h - 20, bracketPaint);
        // Unten rechts
        c.DrawLine(w - 20, h - 20, w - 20, h - 20 - bLen, bracketPaint);
        c.DrawLine(w - 20, h - 20, w - 20 - bLen, h - 20, bracketPaint);

        Gfx.SavePng(surface, "x_banner_1500x500.png");
        Console.WriteLine("  Banner generiert: x_banner_1500x500.png");
    }

    /// <summary>
    /// Zeichnet ein kleines Schild-Badge (konsistent mit dem Profilbild)
    /// </summary>
    static void DrawMiniShield(SKCanvas c, float cx, float topY, float scale)
    {
        float shieldW = 260f * scale;
        float shieldH = 320f * scale;
        float cornerR = 30f * scale;
        float halfW = shieldW / 2f;
        float left = cx - halfW;
        float right = cx + halfW;
        float top = topY;
        float mid = topY + shieldH * 0.65f;
        float bottom = topY + shieldH;

        using var shieldPath = new SKPath();
        shieldPath.MoveTo(left + cornerR, top);
        shieldPath.LineTo(right - cornerR, top);
        shieldPath.ArcTo(new SKRect(right - cornerR * 2, top, right, top + cornerR * 2), -90, 90, false);
        shieldPath.LineTo(right, mid);
        shieldPath.LineTo(cx, bottom);
        shieldPath.LineTo(left, mid);
        shieldPath.LineTo(left, top + cornerR);
        shieldPath.ArcTo(new SKRect(left, top, left + cornerR * 2, top + cornerR * 2), 180, 90, false);
        shieldPath.Close();

        // Schild-Gradient
        using var shieldPaint = new SKPaint { IsAntialias = true };
        using var shieldGrad = SKShader.CreateLinearGradient(
            new SKPoint(cx, top), new SKPoint(cx, bottom),
            [IndigoMid, IndigoDark],
            SKShaderTileMode.Clamp);
        shieldPaint.Shader = shieldGrad;
        c.DrawPath(shieldPath, shieldPaint);

        // Rand
        using var borderPaint = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f, Color = IndigoBright
        };
        c.DrawPath(shieldPath, borderPaint);

        // "RS" Text
        using var rsPaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 120 * scale,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("RS", cx, top + shieldH * 0.42f, rsPaint);

        // Trennlinie
        float lineY = top + shieldH * 0.48f;
        using var linePt = new SKPaint
        {
            Color = IndigoGlow.WithAlpha(150), IsAntialias = true, StrokeWidth = 1.5f
        };
        c.DrawLine(cx - 60 * scale, lineY, cx + 60 * scale, lineY, linePt);

        // "DIGITAL"
        using var digitalPaint = new SKPaint
        {
            Color = IndigoGlow, IsAntialias = true, TextSize = 32 * scale,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("D I G I T A L", cx, top + shieldH * 0.57f, digitalPaint);

    }

    /// <summary>
    /// Zeichnet einen Feature-Punkt mit Bullet
    /// </summary>
    static void DrawFeaturePoint(SKCanvas c, float x, float y, string text, SKColor color)
    {
        // Bullet (kleiner gefüllter Kreis)
        using var bulletPaint = new SKPaint { Color = color, IsAntialias = true };
        c.DrawCircle(x + 6, y - 6, 5f, bulletPaint);

        // Text
        using var textPaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 24,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(text, x + 20, y, textPaint);
    }
}

using SkiaSharp;

namespace SocialPostGenerator;

/// <summary>
/// Generiert Promo-Bilder für Social Media (1200x675):
/// 1. App-Promo-Card: Icon + Name + Version + 3 Features + Preis
/// 2. Portfolio-Übersicht: Alle 8 Icons + "8 Apps | 3 Platforms | 1 Developer"
/// </summary>
static class ImageGenerator
{
    // RS-Digital Midnight Indigo Palette
    static readonly SKColor DarkBg = SKColor.Parse("#0B0E1A");
    static readonly SKColor IndigoDark = SKColor.Parse("#1E1B4B");
    static readonly SKColor IndigoMid = SKColor.Parse("#312E81");
    static readonly SKColor IndigoBright = SKColor.Parse("#4F46E5");
    static readonly SKColor IndigoLight = SKColor.Parse("#6366F1");
    static readonly SKColor IndigoGlow = SKColor.Parse("#818CF8");
    static readonly SKColor White = SKColor.Parse("#F8FAFC");
    static readonly SKColor TextSub = SKColor.Parse("#CBD5E1");
    static readonly SKColor TextMuted = SKColor.Parse("#94A3B8");

    const int Width = 1200;
    const int Height = 675;

    // === App-Promo-Card ===

    /// <summary>
    /// 1200x675 Promo-Card: Icon links + Name/Version + 3 Features + Preis/Plattformen
    /// </summary>
    public static void GeneratePromoCard(string outputDir, AppInfo app, string version, string? iconPath)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var c = surface.Canvas;

        var accent = SKColor.Parse(app.AccentColorHex);

        // Hintergrund: Diagonal-Gradient
        DrawBackground(c, accent);

        // Dekorative Elemente
        DrawDecorations(c, accent);

        // === App-Icon (links) ===
        float iconSize = 180;
        float iconX = 80;
        float iconY = (Height - iconSize) / 2f - 20;

        if (iconPath != null && File.Exists(iconPath))
        {
            using var iconBitmap = SKBitmap.Decode(iconPath);
            if (iconBitmap != null)
            {
                // Icon mit abgerundeten Ecken
                DrawRoundedIcon(c, iconBitmap, iconX, iconY, iconSize, 28f);
            }
        }
        else
        {
            // Platzhalter-Kreis wenn kein Icon
            using var placeholderPaint = new SKPaint
            {
                Color = accent.WithAlpha(80), IsAntialias = true
            };
            c.DrawRoundRect(iconX, iconY, iconSize, iconSize, 28, 28, placeholderPaint);

            using var letterPaint = new SKPaint
            {
                Color = White, IsAntialias = true, TextSize = 72,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Segoe UI",
                    SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            c.DrawText(app.Name[..2], iconX + iconSize / 2, iconY + iconSize / 2 + 24, letterPaint);
        }

        // Schatten unter dem Icon
        using var shadowPaint = new SKPaint { IsAntialias = true };
        using var shadowGrad = SKShader.CreateRadialGradient(
            new SKPoint(iconX + iconSize / 2, iconY + iconSize + 10), iconSize * 0.5f,
            [accent.WithAlpha(40), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        shadowPaint.Shader = shadowGrad;
        c.DrawOval(iconX + iconSize / 2, iconY + iconSize + 10, iconSize * 0.4f, 12, shadowPaint);

        // === Text-Bereich (rechts vom Icon) ===
        float textX = iconX + iconSize + 60;
        float maxTextW = Width - textX - 60;

        // App-Name
        using var namePaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 56,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(app.Name, textX, 140, namePaint);

        // Version-Badge
        var versionText = $"v{version}";
        float nameWidth = namePaint.MeasureText(app.Name);
        float badgeX = textX + nameWidth + 16;
        float badgeY = 118;

        using var badgePaint = new SKPaint
        {
            Color = accent.WithAlpha(180), IsAntialias = true
        };
        using var badgeTextPaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 22,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        float badgeW = badgeTextPaint.MeasureText(versionText) + 20;
        c.DrawRoundRect(badgeX, badgeY, badgeW, 30, 8, 8, badgePaint);
        c.DrawText(versionText, badgeX + badgeW / 2, badgeY + 22, badgeTextPaint);

        // Akzent-Linie unter Name
        using var accentLine = new SKPaint { IsAntialias = true, StrokeWidth = 3f };
        using var lineGrad = SKShader.CreateLinearGradient(
            new SKPoint(textX, 0), new SKPoint(textX + 300, 0),
            [accent, accent.WithAlpha(0)],
            SKShaderTileMode.Clamp);
        accentLine.Shader = lineGrad;
        c.DrawLine(textX, 155, textX + 300, 155, accentLine);

        // Kurzbeschreibung
        using var descPaint = new SKPaint
        {
            Color = TextSub, IsAntialias = true, TextSize = 24,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(app.Type[0..1].ToUpper() + app.Type[1..], textX, 190, descPaint);

        // === 3 Features (Bullet-Points) ===
        float featureY = 240;
        float featureSpacing = 50;

        var features = app.KeyFeatures.Take(3).ToArray();
        for (int i = 0; i < features.Length; i++)
        {
            float fy = featureY + i * featureSpacing;

            // Akzent-Bullet
            using var bulletPaint = new SKPaint { Color = accent, IsAntialias = true };
            c.DrawCircle(textX + 8, fy - 6, 6, bulletPaint);

            // Feature-Text (gekürzt wenn nötig)
            using var featurePaint = new SKPaint
            {
                Color = White.WithAlpha(220), IsAntialias = true, TextSize = 23,
                Typeface = SKTypeface.FromFamilyName("Segoe UI",
                    SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            var featureText = TruncateText(features[i], featurePaint, maxTextW - 30);
            c.DrawText(featureText, textX + 26, fy, featurePaint);
        }

        // === Preis + Plattformen (unten) ===
        float bottomY = Height - 80;

        // Plattform-Badges
        DrawPlatformBadges(c, textX, bottomY, accent);

        // Preis (rechts)
        using var pricePaint = new SKPaint
        {
            Color = accent, IsAntialias = true, TextSize = 26,
            TextAlign = SKTextAlign.Right,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(app.Price, Width - 60, bottomY + 8, pricePaint);

        // RS-Digital Watermark (unten rechts)
        DrawWatermark(c);

        // Speichern
        SavePng(surface, Path.Combine(outputDir, $"{app.Name}_promo.png"));
        Console.WriteLine($"  Promo-Card generiert: {app.Name}_promo.png");
    }

    // === Portfolio-Übersicht ===

    /// <summary>
    /// 1200x675 Portfolio: Alle 8 App-Icons + RS-Digital Branding
    /// </summary>
    public static void GeneratePortfolio(string outputDir, AppInfo[] apps, Dictionary<string, string> iconPaths)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var c = surface.Canvas;

        // Hintergrund
        DrawBackground(c, IndigoBright);
        DrawDecorations(c, IndigoBright);

        // === Titel ===
        using var titlePaint = new SKPaint
        {
            Color = White, IsAntialias = true, TextSize = 48,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("RS-Digital", Width / 2f, 70, titlePaint);

        // Tagline
        using var tagPaint = new SKPaint
        {
            Color = TextSub, IsAntialias = true, TextSize = 24,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("Indie App Studio from Germany", Width / 2f, 105, tagPaint);

        // Akzent-Linie
        using var lineP = new SKPaint { IsAntialias = true, StrokeWidth = 2f };
        using var lineG = SKShader.CreateLinearGradient(
            new SKPoint(Width / 2f - 150, 0), new SKPoint(Width / 2f + 150, 0),
            [IndigoBright.WithAlpha(0), IndigoGlow, IndigoBright.WithAlpha(0)],
            SKShaderTileMode.Clamp);
        lineP.Shader = lineG;
        c.DrawLine(Width / 2f - 150, 118, Width / 2f + 150, 118, lineP);

        // === 8 Icons im Grid (2 Reihen x 4) ===
        float iconSize = 100;
        float spacing = 30;
        float totalW = 4 * iconSize + 3 * spacing;
        float startX = (Width - totalW) / 2f;
        float startY = 150;
        float rowSpacing = 30;

        for (int i = 0; i < apps.Length && i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;

            float x = startX + col * (iconSize + spacing);
            float y = startY + row * (iconSize + rowSpacing + 30); // 30px für Name

            var appColor = SKColor.Parse(apps[i].AccentColorHex);

            if (iconPaths.TryGetValue(apps[i].Name, out var path) && File.Exists(path))
            {
                using var iconBitmap = SKBitmap.Decode(path);
                if (iconBitmap != null)
                {
                    DrawRoundedIcon(c, iconBitmap, x, y, iconSize, 20f);
                }
            }
            else
            {
                // Platzhalter
                using var phPaint = new SKPaint { Color = appColor.WithAlpha(120), IsAntialias = true };
                c.DrawRoundRect(x, y, iconSize, iconSize, 20, 20, phPaint);

                using var phText = new SKPaint
                {
                    Color = White, IsAntialias = true, TextSize = 36,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI",
                        SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                };
                c.DrawText(apps[i].Name[..2], x + iconSize / 2, y + iconSize / 2 + 12, phText);
            }

            // App-Name unter dem Icon
            using var appNamePaint = new SKPaint
            {
                Color = TextSub, IsAntialias = true, TextSize = 15,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Segoe UI",
                    SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            // Name kürzen wenn nötig
            var displayName = apps[i].Name.Length > 14 ? apps[i].Name[..13] + "..." : apps[i].Name;
            c.DrawText(displayName, x + iconSize / 2, y + iconSize + 20, appNamePaint);
        }

        // === Stats-Leiste (unten) ===
        float statsY = Height - 130;

        // Hintergrund-Band
        using var bandPaint = new SKPaint { IsAntialias = true };
        using var bandGrad = SKShader.CreateLinearGradient(
            new SKPoint(0, statsY - 10), new SKPoint(0, statsY + 70),
            [DarkBg.WithAlpha(0), DarkBg.WithAlpha(180), DarkBg.WithAlpha(0)],
            SKShaderTileMode.Clamp);
        bandPaint.Shader = bandGrad;
        c.DrawRect(0, statsY - 10, Width, 80, bandPaint);

        // Drei Stat-Blöcke: "8 Apps | 3 Platforms | 1 Developer"
        float statsCenterY = statsY + 35;
        DrawStatBlock(c, Width / 2f - 280, statsCenterY, "8", "Apps", IndigoGlow);
        DrawStatBlock(c, Width / 2f, statsCenterY, "3", "Platforms", IndigoGlow);
        DrawStatBlock(c, Width / 2f + 280, statsCenterY, "1", "Developer", IndigoGlow);

        // Trenner zwischen Stats
        using var divPaint = new SKPaint { Color = IndigoBright.WithAlpha(60), IsAntialias = true, StrokeWidth = 1.5f };
        c.DrawLine(Width / 2f - 140, statsCenterY - 15, Width / 2f - 140, statsCenterY + 15, divPaint);
        c.DrawLine(Width / 2f + 140, statsCenterY - 15, Width / 2f + 140, statsCenterY + 15, divPaint);

        // Plattformen
        using var platPaint = new SKPaint
        {
            Color = TextMuted, IsAntialias = true, TextSize = 18,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("Android  |  Windows  |  Linux", Width / 2f, Height - 40, platPaint);

        // Watermark
        DrawWatermark(c);

        SavePng(surface, Path.Combine(outputDir, "portfolio.png"));
        Console.WriteLine("  Portfolio-Bild generiert: portfolio.png");
    }

    // === Hilfsmethoden ===

    /// <summary>
    /// Zeichnet den dunkelindigo Hintergrund mit Akzent-Gradient
    /// </summary>
    static void DrawBackground(SKCanvas c, SKColor accent)
    {
        // Basis: Tiefdunkles Indigo
        c.Clear(DarkBg);

        // Subtiler Diagonal-Gradient
        using var bgPaint = new SKPaint { IsAntialias = true };
        using var bgGrad = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(Width, Height),
            [DarkBg, IndigoDark, IndigoMid.WithAlpha(100)],
            [0f, 0.6f, 1f],
            SKShaderTileMode.Clamp);
        bgPaint.Shader = bgGrad;
        c.DrawRect(0, 0, Width, Height, bgPaint);

        // Akzent-Glow oben rechts
        using var glowPaint = new SKPaint { IsAntialias = true };
        using var glowGrad = SKShader.CreateRadialGradient(
            new SKPoint(Width * 0.85f, Height * 0.15f), 300,
            [accent.WithAlpha(20), SKColors.Transparent],
            SKShaderTileMode.Clamp);
        glowPaint.Shader = glowGrad;
        c.DrawCircle(Width * 0.85f, Height * 0.15f, 300, glowPaint);
    }

    /// <summary>
    /// Dekorative Elemente: Subtile Linien + Code-Bracket-Ecken
    /// </summary>
    static void DrawDecorations(SKCanvas c, SKColor accent)
    {
        // Subtile horizontale Linien
        using var linePaint = new SKPaint
        {
            Color = IndigoBright.WithAlpha(8), IsAntialias = true, StrokeWidth = 1
        };
        for (int y = 25; y < Height; y += 35)
        {
            c.DrawLine(0, y, Width, y, linePaint);
        }

        // Code-Bracket-Ecken
        using var bracketPaint = new SKPaint
        {
            Color = accent.WithAlpha(35), IsAntialias = true,
            Style = SKPaintStyle.Stroke, StrokeWidth = 2f
        };
        float bLen = 30;
        // Oben links
        c.DrawLine(15, 15, 15, 15 + bLen, bracketPaint);
        c.DrawLine(15, 15, 15 + bLen, 15, bracketPaint);
        // Oben rechts
        c.DrawLine(Width - 15, 15, Width - 15, 15 + bLen, bracketPaint);
        c.DrawLine(Width - 15, 15, Width - 15 - bLen, 15, bracketPaint);
        // Unten links
        c.DrawLine(15, Height - 15, 15, Height - 15 - bLen, bracketPaint);
        c.DrawLine(15, Height - 15, 15 + bLen, Height - 15, bracketPaint);
        // Unten rechts
        c.DrawLine(Width - 15, Height - 15, Width - 15, Height - 15 - bLen, bracketPaint);
        c.DrawLine(Width - 15, Height - 15, Width - 15 - bLen, Height - 15, bracketPaint);
    }

    /// <summary>
    /// Zeichnet ein Icon mit abgerundeten Ecken
    /// </summary>
    static void DrawRoundedIcon(SKCanvas c, SKBitmap icon, float x, float y, float size, float cornerR)
    {
        using var resized = icon.Resize(new SKImageInfo((int)size, (int)size), SKFilterQuality.High);
        if (resized == null) return;

        c.Save();
        using var clipPath = new SKPath();
        clipPath.AddRoundRect(new SKRoundRect(new SKRect(x, y, x + size, y + size), cornerR, cornerR));
        c.ClipPath(clipPath, antialias: true);

        c.DrawBitmap(resized, x, y);
        c.Restore();

        // Rand um das Icon
        using var borderPaint = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f, Color = White.WithAlpha(30)
        };
        c.DrawRoundRect(x, y, size, size, cornerR, cornerR, borderPaint);
    }

    /// <summary>
    /// Plattform-Badges: Android | Windows | Linux
    /// </summary>
    static void DrawPlatformBadges(SKCanvas c, float x, float y, SKColor accent)
    {
        string[] platforms = ["Android", "Windows", "Linux"];
        float badgeH = 28;
        float gap = 12;
        float curX = x;

        foreach (var platform in platforms)
        {
            using var textPaint = new SKPaint
            {
                Color = White.WithAlpha(200), IsAntialias = true, TextSize = 17,
                Typeface = SKTypeface.FromFamilyName("Segoe UI",
                    SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            };
            float textW = textPaint.MeasureText(platform);
            float badgeW = textW + 18;

            using var bgPaint = new SKPaint
            {
                Color = IndigoMid.WithAlpha(180), IsAntialias = true
            };
            c.DrawRoundRect(curX, y - badgeH / 2 - 2, badgeW, badgeH, 6, 6, bgPaint);

            using var borderP = new SKPaint
            {
                Color = accent.WithAlpha(60), IsAntialias = true,
                Style = SKPaintStyle.Stroke, StrokeWidth = 1f
            };
            c.DrawRoundRect(curX, y - badgeH / 2 - 2, badgeW, badgeH, 6, 6, borderP);

            c.DrawText(platform, curX + 9, y + 4, textPaint);
            curX += badgeW + gap;
        }
    }

    /// <summary>
    /// Stat-Block für Portfolio (Zahl + Label)
    /// </summary>
    static void DrawStatBlock(SKCanvas c, float cx, float cy, string number, string label, SKColor color)
    {
        using var numPaint = new SKPaint
        {
            Color = color, IsAntialias = true, TextSize = 36,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(number, cx, cy - 2, numPaint);

        using var labelPaint = new SKPaint
        {
            Color = TextSub, IsAntialias = true, TextSize = 20,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText(label, cx, cy + 24, labelPaint);
    }

    /// <summary>
    /// RS-Digital Watermark (unten rechts)
    /// </summary>
    static void DrawWatermark(SKCanvas c)
    {
        using var wmPaint = new SKPaint
        {
            Color = TextMuted.WithAlpha(80), IsAntialias = true, TextSize = 14,
            TextAlign = SKTextAlign.Right,
            Typeface = SKTypeface.FromFamilyName("Segoe UI",
                SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };
        c.DrawText("RS-Digital", Width - 20, Height - 18, wmPaint);
    }

    /// <summary>
    /// Text kürzen wenn er zu breit wird
    /// </summary>
    static string TruncateText(string text, SKPaint paint, float maxWidth)
    {
        if (paint.MeasureText(text) <= maxWidth)
            return text;

        while (text.Length > 3 && paint.MeasureText(text + "...") > maxWidth)
            text = text[..^1];

        return text + "...";
    }

    /// <summary>
    /// PNG speichern
    /// </summary>
    static void SavePng(SKSurface surface, string path)
    {
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
    }
}

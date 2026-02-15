using SkiaSharp;

namespace HandwerkerImperium.Graphics;

/// <summary>
/// SkiaSharp-Renderer fuer das Grundriss-Raetsel Mini-Game.
/// Zeichnet einen Architektenplan: Weisses Papier, blaue Grundrisslinien,
/// Slots mit Hint-Icons, gefuellte Slots mit Emoji, Korrekt-Haekchen,
/// Fehler-Blinken, Massstab-Linien am Rand.
/// Pixel-Art Stil passend zu SawingGameRenderer/CityRenderer.
/// </summary>
public class DesignPuzzleRenderer
{
    // Animationszeit (wird intern hochgezaehlt)
    private float _time;

    // Fehler-Flash pro Slot (Index -> verbleibende Flash-Zeit)
    private readonly Dictionary<int, float> _errorFlash = new();

    // Farb-Palette (Architektenplan)
    private static readonly SKColor PaperWhite = new(0xF5, 0xF0, 0xE8);       // Warmes Papier-Weiss
    private static readonly SKColor BlueprintLine = new(0x1A, 0x6B, 0xAF);     // Blaupause-Linienfarbe
    private static readonly SKColor BlueprintLineLight = new(0x6B, 0xAE, 0xD6); // Hellere Hilfslinien
    private static readonly SKColor BlueprintGrid = new(0xBB, 0xD7, 0xEA);     // Feines Raster
    private static readonly SKColor SlotBorderEmpty = new(0x78, 0x90, 0x9C);   // Leerer Slot Rand
    private static readonly SKColor SlotFillEmpty = new(0xEC, 0xEF, 0xF1);     // Leerer Slot Fuellfarbe
    private static readonly SKColor CorrectGreen = new(0x4C, 0xAF, 0x50);      // Korrekt platziert
    private static readonly SKColor ErrorRed = new(0xF4, 0x43, 0x36);          // Fehler-Flash
    private static readonly SKColor HintTextColor = new(0x90, 0xA4, 0xAE);     // Hint-Icon Farbe (transparent)
    private static readonly SKColor QuestionMarkColor = new(0xB0, 0xBE, 0xC5); // "?" Overlay
    private static readonly SKColor CheckmarkWhite = new(0xFF, 0xFF, 0xFF);    // Haekchen-Farbe
    private static readonly SKColor ScaleLineColor = new(0x78, 0x90, 0x9C);    // Massstab-Linien
    private static readonly SKColor CraftOrange = new(0xEA, 0x58, 0x0C);       // Craft-Akzentfarbe

    /// <summary>
    /// Daten-Struct fuer einen einzelnen Slot (vom ViewModel befuellt).
    /// </summary>
    public struct RoomSlotData
    {
        public string HintIcon;
        public string DisplayEmoji;
        public uint BackgroundColor; // ARGB
        public uint BorderColor;     // ARGB
        public bool IsFilled;
        public bool IsCorrect;
        public bool HasError;
    }

    /// <summary>
    /// Rendert den gesamten Grundriss auf das Canvas.
    /// </summary>
    /// <param name="canvas">SkiaSharp Canvas zum Zeichnen.</param>
    /// <param name="bounds">Verfuegbarer Zeichenbereich.</param>
    /// <param name="slots">Slot-Daten-Array.</param>
    /// <param name="cols">Anzahl Spalten im Grid.</param>
    /// <param name="rows">Anzahl Zeilen im Grid.</param>
    /// <param name="deltaTime">Zeitdelta seit letztem Frame in Sekunden.</param>
    public void Render(SKCanvas canvas, SKRect bounds, RoomSlotData[] slots, int cols, int rows, float deltaTime)
    {
        _time += deltaTime;

        // Fehler-Flash Timer aktualisieren
        UpdateErrorFlash(slots, deltaTime);

        float padding = 12;
        float innerLeft = bounds.Left + padding;
        float innerTop = bounds.Top + padding;
        float innerWidth = bounds.Width - 2 * padding;
        float innerHeight = bounds.Height - 2 * padding;

        // 1. Papier-Hintergrund
        DrawPaperBackground(canvas, bounds);

        // 2. Blaupause-Raster (feines Hintergrundmuster)
        DrawBlueprintGrid(canvas, bounds);

        // 3. Massstab-Linien am Rand (Deko)
        DrawScaleMarks(canvas, bounds, innerLeft, innerTop, innerWidth, innerHeight);

        // 4. Grundriss-Slots berechnen und zeichnen
        if (slots.Length == 0) return;

        // Slot-Groesse berechnen (gleichmaessig verteilt)
        float slotSpacing = 6;
        float availW = innerWidth - 40; // Platz fuer Massstab-Linien links/rechts
        float availH = innerHeight - 40;
        float slotW = (availW - (cols - 1) * slotSpacing) / cols;
        float slotH = (availH - (rows - 1) * slotSpacing) / rows;
        float slotSize = Math.Min(slotW, slotH);

        // Grid zentrieren
        float totalW = cols * slotSize + (cols - 1) * slotSpacing;
        float totalH = rows * slotSize + (rows - 1) * slotSpacing;
        float gridLeft = bounds.Left + (bounds.Width - totalW) / 2;
        // Oben ausrichten statt vertikal zentrieren
        float gridTop = bounds.Top + padding + 20; // +20 fuer Massstab-Markierungen oben

        // 5. Grundriss-Umriss (dicker blauer Rahmen)
        DrawFloorplanOutline(canvas, gridLeft, gridTop, totalW, totalH);

        // 6. Slots zeichnen
        for (int i = 0; i < slots.Length && i < cols * rows; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float sx = gridLeft + col * (slotSize + slotSpacing);
            float sy = gridTop + row * (slotSize + slotSpacing);

            DrawSlot(canvas, sx, sy, slotSize, slotSize, slots[i], i);
        }

        // 7. Innere Trennlinien (Grundriss-Waende)
        DrawInnerWalls(canvas, gridLeft, gridTop, totalW, totalH, cols, rows, slotSize, slotSpacing);
    }

    /// <summary>
    /// HitTest: Gibt den Slot-Index zurueck (-1 wenn kein Treffer).
    /// </summary>
    public int HitTest(SKRect bounds, float touchX, float touchY, int cols, int rows, int slotCount)
    {
        if (slotCount == 0 || cols == 0 || rows == 0) return -1;

        float padding = 12;
        float innerWidth = bounds.Width - 2 * padding;
        float innerHeight = bounds.Height - 2 * padding;

        float slotSpacing = 6;
        float availW = innerWidth - 40;
        float availH = innerHeight - 40;
        float slotW = (availW - (cols - 1) * slotSpacing) / cols;
        float slotH = (availH - (rows - 1) * slotSpacing) / rows;
        float slotSize = Math.Min(slotW, slotH);

        float totalW = cols * slotSize + (cols - 1) * slotSpacing;
        float totalH = rows * slotSize + (rows - 1) * slotSpacing;
        float gridLeft = bounds.Left + (bounds.Width - totalW) / 2;
        // Oben ausrichten (identisch mit Render)
        float gridTop = bounds.Top + padding + 20; // +20 fuer Massstab-Markierungen oben

        for (int i = 0; i < slotCount && i < cols * rows; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float sx = gridLeft + col * (slotSize + slotSpacing);
            float sy = gridTop + row * (slotSize + slotSpacing);

            if (touchX >= sx && touchX <= sx + slotSize &&
                touchY >= sy && touchY <= sy + slotSize)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Setzt einen Fehler-Flash fuer einen bestimmten Slot.
    /// </summary>
    public void TriggerErrorFlash(int slotIndex)
    {
        _errorFlash[slotIndex] = 0.4f; // 400ms Flash
    }

    // =========================================================================
    // ZEICHENFUNKTIONEN
    // =========================================================================

    /// <summary>
    /// Zeichnet den Papier-Hintergrund mit leichter Textur.
    /// </summary>
    private void DrawPaperBackground(SKCanvas canvas, SKRect bounds)
    {
        // Basis-Papierfarbe
        using var paperPaint = new SKPaint { Color = PaperWhite, IsAntialias = false };
        canvas.DrawRect(bounds, paperPaint);

        // Leichte Papier-Textur (subtile horizontale Linien)
        using var texturePaint = new SKPaint
        {
            Color = new SKColor(0xE8, 0xE0, 0xD5, 30),
            IsAntialias = false,
            StrokeWidth = 1
        };
        for (float y = bounds.Top + 8; y < bounds.Bottom; y += 12)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, texturePaint);
        }

        // Papier-Schatten am Rand (dunkler Streifen)
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 15),
            IsAntialias = false
        };
        canvas.DrawRect(bounds.Left, bounds.Top, bounds.Width, 3, shadowPaint);
        canvas.DrawRect(bounds.Left, bounds.Top, 3, bounds.Height, shadowPaint);
        canvas.DrawRect(bounds.Right - 3, bounds.Top, 3, bounds.Height, shadowPaint);
        canvas.DrawRect(bounds.Left, bounds.Bottom - 3, bounds.Width, 3, shadowPaint);
    }

    /// <summary>
    /// Zeichnet das feine Blaupause-Raster im Hintergrund.
    /// </summary>
    private void DrawBlueprintGrid(SKCanvas canvas, SKRect bounds)
    {
        using var gridPaint = new SKPaint
        {
            Color = BlueprintGrid.WithAlpha(40),
            IsAntialias = false,
            StrokeWidth = 1
        };

        // Vertikale Linien
        for (float x = bounds.Left + 20; x < bounds.Right; x += 20)
        {
            canvas.DrawLine(x, bounds.Top, x, bounds.Bottom, gridPaint);
        }

        // Horizontale Linien
        for (float y = bounds.Top + 20; y < bounds.Bottom; y += 20)
        {
            canvas.DrawLine(bounds.Left, y, bounds.Right, y, gridPaint);
        }
    }

    /// <summary>
    /// Zeichnet Massstab-Markierungen am Rand (Architektenplan-Deko).
    /// </summary>
    private void DrawScaleMarks(SKCanvas canvas, SKRect bounds, float innerLeft, float innerTop, float innerWidth, float innerHeight)
    {
        using var scalePaint = new SKPaint
        {
            Color = ScaleLineColor,
            IsAntialias = false,
            StrokeWidth = 1
        };

        // Linker Rand: vertikale Massstab-Striche
        for (float y = innerTop + 10; y < innerTop + innerHeight; y += 15)
        {
            float tickLen = ((int)((y - innerTop) / 15) % 5 == 0) ? 8 : 4;
            canvas.DrawLine(innerLeft + 2, y, innerLeft + 2 + tickLen, y, scalePaint);
        }

        // Oberer Rand: horizontale Massstab-Striche
        for (float x = innerLeft + 20; x < innerLeft + innerWidth; x += 15)
        {
            float tickLen = ((int)((x - innerLeft - 20) / 15) % 5 == 0) ? 8 : 4;
            canvas.DrawLine(x, innerTop + 2, x, innerTop + 2 + tickLen, scalePaint);
        }

        // Massstab-Linie links (durchgehend)
        canvas.DrawLine(innerLeft + 2, innerTop + 10, innerLeft + 2, innerTop + innerHeight - 10, scalePaint);

        // Massstab-Linie oben (durchgehend)
        canvas.DrawLine(innerLeft + 20, innerTop + 2, innerLeft + innerWidth - 10, innerTop + 2, scalePaint);
    }

    /// <summary>
    /// Zeichnet den aeusseren Grundriss-Rahmen (dicke blaue Linien).
    /// </summary>
    private void DrawFloorplanOutline(SKCanvas canvas, float x, float y, float w, float h)
    {
        // Aeussere Wand (dick)
        using var wallPaint = new SKPaint
        {
            Color = BlueprintLine,
            IsAntialias = false,
            StrokeWidth = 4,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(x - 4, y - 4, w + 8, h + 8, wallPaint);

        // Schatten-Effekt (dezent)
        using var shadowPaint = new SKPaint
        {
            Color = BlueprintLine.WithAlpha(30),
            IsAntialias = false,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(x - 7, y - 7, w + 14, h + 14, shadowPaint);
    }

    /// <summary>
    /// Zeichnet innere Trennwaende zwischen den Slots (Grundriss-Stil).
    /// </summary>
    private void DrawInnerWalls(SKCanvas canvas, float gridLeft, float gridTop, float totalW, float totalH,
        int cols, int rows, float slotSize, float spacing)
    {
        using var wallPaint = new SKPaint
        {
            Color = BlueprintLine,
            IsAntialias = false,
            StrokeWidth = 2
        };

        // Vertikale Waende
        for (int c = 1; c < cols; c++)
        {
            float wx = gridLeft + c * (slotSize + spacing) - spacing / 2;
            canvas.DrawLine(wx, gridTop - 4, wx, gridTop + totalH + 4, wallPaint);
        }

        // Horizontale Waende
        for (int r = 1; r < rows; r++)
        {
            float wy = gridTop + r * (slotSize + spacing) - spacing / 2;
            canvas.DrawLine(gridLeft - 4, wy, gridLeft + totalW + 4, wy, wallPaint);
        }

        // Tuer-Oeffnungen (kleine Luecken in den Waenden fuer optisches Detail)
        using var doorPaint = new SKPaint { Color = PaperWhite, IsAntialias = false };
        float doorWidth = slotSize * 0.3f;

        // Horizontale Tuer-Luecken (in vertikalen Waenden)
        for (int c = 1; c < cols; c++)
        {
            float wx = gridLeft + c * (slotSize + spacing) - spacing / 2;
            for (int r = 0; r < rows; r++)
            {
                float doorY = gridTop + r * (slotSize + spacing) + slotSize / 2 - doorWidth / 2;
                canvas.DrawRect(wx - 2, doorY, 4, doorWidth, doorPaint);
            }
        }

        // Vertikale Tuer-Luecken (in horizontalen Waenden)
        for (int r = 1; r < rows; r++)
        {
            float wy = gridTop + r * (slotSize + spacing) - spacing / 2;
            for (int c = 0; c < cols; c++)
            {
                float doorX = gridLeft + c * (slotSize + spacing) + slotSize / 2 - doorWidth / 2;
                canvas.DrawRect(doorX, wy - 2, doorWidth, 4, doorPaint);
            }
        }
    }

    /// <summary>
    /// Zeichnet einen einzelnen Slot (leer oder gefuellt).
    /// </summary>
    private void DrawSlot(SKCanvas canvas, float x, float y, float w, float h, RoomSlotData slot, int index)
    {
        // Fehler-Flash pruefen
        bool isFlashing = _errorFlash.ContainsKey(index) && _errorFlash[index] > 0;

        if (slot.IsFilled)
        {
            DrawFilledSlot(canvas, x, y, w, h, slot);
        }
        else if (isFlashing)
        {
            DrawErrorSlot(canvas, x, y, w, h, slot, _errorFlash[index]);
        }
        else
        {
            DrawEmptySlot(canvas, x, y, w, h, slot);
        }
    }

    /// <summary>
    /// Zeichnet einen leeren Slot mit gestricheltem Rahmen und Hint-Icon.
    /// </summary>
    private void DrawEmptySlot(SKCanvas canvas, float x, float y, float w, float h, RoomSlotData slot)
    {
        // Hintergrund (leicht gefuellt)
        using var bgPaint = new SKPaint { Color = SlotFillEmpty, IsAntialias = false };
        canvas.DrawRect(x, y, w, h, bgPaint);

        // Gestrichelter Rahmen
        using var borderPaint = new SKPaint
        {
            Color = SlotBorderEmpty,
            IsAntialias = false,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, _time * 8)
        };
        canvas.DrawRect(x + 1, y + 1, w - 2, h - 2, borderPaint);

        // Pulsierender Glow-Effekt auf leerem Slot (subtil)
        float pulse = (float)(0.3 + 0.15 * Math.Sin(_time * 2));
        using var glowPaint = new SKPaint
        {
            Color = BlueprintLineLight.WithAlpha((byte)(pulse * 60)),
            IsAntialias = false
        };
        canvas.DrawRect(x + 2, y + 2, w - 4, h - 4, glowPaint);

        // Hint-Icon (gross, transparent in der Mitte)
        if (!string.IsNullOrEmpty(slot.HintIcon))
        {
            float emojiSize = Math.Min(w, h) * 0.45f;
            using var hintFont = new SKFont(SKTypeface.Default, emojiSize);
            using var hintPaint = new SKPaint { Color = HintTextColor.WithAlpha(80), IsAntialias = true };
            float textWidth = hintFont.MeasureText(slot.HintIcon);
            canvas.DrawText(slot.HintIcon, x + (w - textWidth) / 2, y + h / 2 + emojiSize / 3, SKTextAlign.Left, hintFont, hintPaint);
        }

        // "?" oben rechts
        using var questionFont = new SKFont(SKTypeface.Default, Math.Min(w, h) * 0.22f);
        using var questionPaint = new SKPaint { Color = QuestionMarkColor, IsAntialias = true };
        canvas.DrawText("?", x + w - 16, y + 16, SKTextAlign.Center, questionFont, questionPaint);
    }

    /// <summary>
    /// Zeichnet einen gefuellten Slot mit Hintergrundfarbe und Emoji.
    /// </summary>
    private void DrawFilledSlot(SKCanvas canvas, float x, float y, float w, float h, RoomSlotData slot)
    {
        // Hintergrundfarbe
        using var bgPaint = new SKPaint { Color = new SKColor(slot.BackgroundColor), IsAntialias = false };
        canvas.DrawRect(x, y, w, h, bgPaint);

        // Hellerer Innenbereich (leichte Tiefe)
        using var innerPaint = new SKPaint
        {
            Color = new SKColor(0xFF, 0xFF, 0xFF, 20),
            IsAntialias = false
        };
        canvas.DrawRect(x + 3, y + 3, w - 6, h - 6, innerPaint);

        // Rahmen in Slot-Farbe (solide)
        using var borderPaint = new SKPaint
        {
            Color = new SKColor(slot.BorderColor),
            IsAntialias = false,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(x + 1, y + 1, w - 2, h - 2, borderPaint);

        // Emoji in der Mitte
        if (!string.IsNullOrEmpty(slot.DisplayEmoji))
        {
            float emojiSize = Math.Min(w, h) * 0.5f;
            using var emojiFont = new SKFont(SKTypeface.Default, emojiSize);
            using var emojiPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            float textWidth = emojiFont.MeasureText(slot.DisplayEmoji);
            canvas.DrawText(slot.DisplayEmoji, x + (w - textWidth) / 2, y + h / 2 + emojiSize / 3, SKTextAlign.Left, emojiFont, emojiPaint);
        }

        // Korrekt-Haekchen unten rechts (gruener Kreis + weisses Haekchen)
        if (slot.IsCorrect)
        {
            float checkSize = Math.Min(w, h) * 0.22f;
            float cx = x + w - checkSize - 4;
            float cy = y + h - checkSize - 4;

            // Gruener Kreis
            using var checkBgPaint = new SKPaint { Color = CorrectGreen, IsAntialias = false };
            canvas.DrawCircle(cx + checkSize / 2, cy + checkSize / 2, checkSize / 2, checkBgPaint);

            // Weisses Haekchen (vereinfacht als 2 Linien)
            using var checkPaint = new SKPaint
            {
                Color = CheckmarkWhite,
                IsAntialias = false,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round
            };
            float midX = cx + checkSize / 2;
            float midY = cy + checkSize / 2;
            // Kurzer Strich nach links-unten
            canvas.DrawLine(midX - checkSize * 0.2f, midY, midX - checkSize * 0.05f, midY + checkSize * 0.2f, checkPaint);
            // Langer Strich nach rechts-oben
            canvas.DrawLine(midX - checkSize * 0.05f, midY + checkSize * 0.2f, midX + checkSize * 0.25f, midY - checkSize * 0.15f, checkPaint);

            // Subtiler Glow um den Haekchen-Kreis
            using var glowPaint = new SKPaint
            {
                Color = CorrectGreen.WithAlpha(40),
                IsAntialias = false
            };
            canvas.DrawCircle(cx + checkSize / 2, cy + checkSize / 2, checkSize / 2 + 3, glowPaint);
        }
    }

    /// <summary>
    /// Zeichnet einen Slot im Fehler-Zustand (rotes Blinken).
    /// </summary>
    private void DrawErrorSlot(SKCanvas canvas, float x, float y, float w, float h, RoomSlotData slot, float flashTimeLeft)
    {
        // Roter Flash-Hintergrund (Intensitaet basierend auf verbleibender Zeit)
        float intensity = flashTimeLeft / 0.4f; // 0-1
        float flashPulse = (float)(0.5 + 0.5 * Math.Sin(_time * 20)); // Schnelles Blinken
        byte alpha = (byte)(intensity * flashPulse * 180);

        using var errorBgPaint = new SKPaint { Color = ErrorRed.WithAlpha(alpha), IsAntialias = false };
        canvas.DrawRect(x, y, w, h, errorBgPaint);

        // Basis-Slot darunter (leerer Slot Hintergrund)
        using var basePaint = new SKPaint { Color = SlotFillEmpty.WithAlpha((byte)(255 - alpha)), IsAntialias = false };
        canvas.DrawRect(x, y, w, h, basePaint);

        // Roter Rahmen
        using var borderPaint = new SKPaint
        {
            Color = ErrorRed.WithAlpha((byte)(100 + intensity * 155)),
            IsAntialias = false,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(x + 1, y + 1, w - 2, h - 2, borderPaint);

        // "X" in der Mitte
        float xSize = Math.Min(w, h) * 0.2f;
        float cx = x + w / 2;
        float cy = y + h / 2;
        using var xPaint = new SKPaint
        {
            Color = ErrorRed.WithAlpha((byte)(intensity * 255)),
            IsAntialias = false,
            StrokeWidth = 3,
            StrokeCap = SKStrokeCap.Round
        };
        canvas.DrawLine(cx - xSize, cy - xSize, cx + xSize, cy + xSize, xPaint);
        canvas.DrawLine(cx + xSize, cy - xSize, cx - xSize, cy + xSize, xPaint);

        // Hint-Icon (leicht sichtbar)
        if (!string.IsNullOrEmpty(slot.HintIcon))
        {
            float emojiSize = Math.Min(w, h) * 0.35f;
            using var hintFont = new SKFont(SKTypeface.Default, emojiSize);
            using var hintPaint = new SKPaint { Color = HintTextColor.WithAlpha(40), IsAntialias = true };
            float textWidth = hintFont.MeasureText(slot.HintIcon);
            canvas.DrawText(slot.HintIcon, x + (w - textWidth) / 2, y + h / 2 + emojiSize / 3, SKTextAlign.Left, hintFont, hintPaint);
        }
    }

    /// <summary>
    /// Aktualisiert die Fehler-Flash Timer.
    /// </summary>
    private void UpdateErrorFlash(RoomSlotData[] slots, float deltaTime)
    {
        // Neue Fehler-Slots registrieren
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].HasError && !_errorFlash.ContainsKey(i))
            {
                _errorFlash[i] = 0.4f;
            }
        }

        // Timer herunterzaehlen
        var keysToRemove = new List<int>();
        foreach (var key in _errorFlash.Keys.ToList())
        {
            _errorFlash[key] -= deltaTime;
            if (_errorFlash[key] <= 0)
            {
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _errorFlash.Remove(key);
        }
    }
}

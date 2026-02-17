using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Avalonia.Threading;
using HandwerkerImperium.Graphics;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.ViewModels;
using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class WorkshopView : UserControl
{
    private WorkshopViewModel? _workshopVm;
    private readonly WorkshopInteriorRenderer _interiorRenderer = new();
    private readonly AnimationManager _animationManager = new();
    private DispatcherTimer? _renderTimer;
    private SKCanvasView? _workshopCanvas;
    private DateTime _lastRenderTime = DateTime.UtcNow;
    // Animations-Zustand fuer arbeitende Worker
    private float _workerAnimPhase;

    public WorkshopView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes VM abmelden
        if (_workshopVm != null)
        {
            _workshopVm.UpgradeEffectRequested -= OnUpgradeEffect;
            _workshopVm = null;
        }

        if (DataContext is WorkshopViewModel vm)
        {
            _workshopVm = vm;
            vm.UpgradeEffectRequested += OnUpgradeEffect;

            // Workshop-Canvas finden und Timer starten
            _workshopCanvas = this.FindControl<SKCanvasView>("WorkshopCanvas");
            if (_workshopCanvas != null)
            {
                _workshopCanvas.PaintSurface += OnWorkshopPaintSurface;
                StartRenderLoop();
            }
        }
    }

    private void StartRenderLoop()
    {
        _renderTimer?.Stop();
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; // 20fps
        _renderTimer.Tick += (_, _) =>
        {
            _workshopCanvas?.InvalidateSurface();
        };
        _renderTimer.Start();
    }

    private void OnWorkshopPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        // Workshop aus ViewModel holen
        var workshop = _workshopVm?.GetWorkshopForRendering();
        if (workshop != null)
        {
            _interiorRenderer.Render(canvas, bounds, workshop);

            // Animierte Elemente zeichnen
            DrawAnimatedElements(canvas, bounds, workshop);
        }

        // AnimationManager (Partikel)
        var now = DateTime.UtcNow;
        var delta = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;

        _workerAnimPhase += (float)delta;
        if (_workerAnimPhase > MathF.Tau) _workerAnimPhase -= MathF.Tau;

        _animationManager.Update(delta);
        _animationManager.Render(canvas);
    }

    // ====================================================================
    // Animierte Szenen - jeder Workshop-Typ hat eine große, erkennbare Szene
    // die den gesamten freien Canvas-Bereich nutzt
    // ====================================================================

    /// <summary>
    /// Hauptmethode: Zeichnet eine große erkennbare Szene über den statischen Workshop-Renderer.
    /// Jeder Workshop-Typ hat eine einzigartige Szene. Intensität skaliert mit Worker-Anzahl.
    /// </summary>
    private void DrawAnimatedElements(SKCanvas canvas, SKRect bounds, Workshop workshop)
    {
        // Aktive Worker zählen (nicht ruhend, nicht trainierend)
        int activeWorkers = workshop.Workers.Count(w => !w.IsResting && !w.IsTraining);
        if (activeWorkers == 0) return;

        // Geschwindigkeits-Multiplikator basierend auf Worker-Anzahl
        float speed = activeWorkers switch
        {
            1 => 0.7f,
            2 or 3 => 1.0f,
            _ => 1.3f
        };

        int particleRate = activeWorkers switch
        {
            1 => 1,
            2 or 3 => 2,
            _ => 3
        };

        int productCount = activeWorkers switch
        {
            1 => 1,
            2 or 3 => 2,
            _ => 3
        };

        // Nutzbare Fläche für Szene (über dem statischen Content)
        float left = bounds.Left + 16;
        float right = bounds.Right - 40;
        float top = bounds.Top + 16;
        float bottom = bounds.Bottom - 8;
        float w = right - left;
        float h = bottom - top;

        switch (workshop.Type)
        {
            case WorkshopType.Carpenter:
                DrawCarpenterScene(canvas, left, top, w, h, speed, particleRate, productCount, activeWorkers);
                break;
            case WorkshopType.Plumber:
                DrawPlumberScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.Electrician:
                DrawElectricianScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.Painter:
                DrawPainterScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.Roofer:
                DrawRooferScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.Contractor:
                DrawContractorScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.Architect:
                DrawArchitectScene(canvas, left, top, w, h, speed, particleRate, activeWorkers);
                break;
            case WorkshopType.GeneralContractor:
                DrawGeneralContractorScene(canvas, left, top, w, h, speed, particleRate, productCount, activeWorkers);
                break;
        }
    }

    // ====================================================================
    // Schreiner: Große Säge schneidet durch ein Brett
    // ====================================================================
    private void DrawCarpenterScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int productCount, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        // --- Großes Holzbrett quer in der Mitte ---
        float boardW = w * 0.75f;
        float boardH = 14;
        float boardX = left + (w - boardW) / 2;
        float boardY = top + h * 0.42f;

        // Brett-Körper
        paint.Color = new SKColor(0xA0, 0x52, 0x2D);
        canvas.DrawRect(boardX, boardY, boardW, boardH, paint);
        // Hellere Oberkante
        paint.Color = new SKColor(0xB8, 0x73, 0x3F);
        canvas.DrawRect(boardX, boardY, boardW, 3, paint);
        // Maserungslinien
        paint.Color = new SKColor(0x8B, 0x45, 0x13, 80);
        paint.StrokeWidth = 1;
        for (int m = 0; m < 3; m++)
        {
            float my = boardY + 5 + m * 3;
            canvas.DrawLine(boardX + 4, my, boardX + boardW - 4, my, paint);
        }
        // Astloch
        paint.Color = new SKColor(0x6D, 0x3A, 0x1A, 120);
        canvas.DrawCircle(boardX + boardW * 0.7f, boardY + boardH / 2, 3, paint);

        // --- Handsäge fährt horizontal über Brett ---
        float sawProgress = (MathF.Sin(phase * 2.5f) + 1) / 2; // 0..1 hin und her
        float sawX = boardX + 10 + sawProgress * (boardW - 50);
        float sawVibY = MathF.Sin(phase * 20) * 1; // Vibration

        // Sägeblatt (24x3px)
        paint.Color = new SKColor(0xC0, 0xC0, 0xC0);
        canvas.DrawRect(sawX, boardY - 4 + sawVibY, 28, 3, paint);
        // Sägezähne an Unterkante
        paint.Color = new SKColor(0xA0, 0xA0, 0xA0);
        for (int t = 0; t < 7; t++)
            canvas.DrawRect(sawX + t * 4, boardY - 1 + sawVibY, 2, 2, paint);
        // Holzgriff
        paint.Color = new SKColor(0x5D, 0x40, 0x37);
        canvas.DrawRect(sawX + 28, boardY - 10 + sawVibY, 8, 12, paint);
        paint.Color = new SKColor(0x4E, 0x34, 0x2E);
        canvas.DrawRect(sawX + 29, boardY - 9 + sawVibY, 6, 10, paint);

        // Schnittlinie im Brett unter der Säge
        paint.Color = new SKColor(0x3E, 0x27, 0x12);
        paint.StrokeWidth = 2;
        canvas.DrawLine(boardX + 10, boardY + boardH / 2, sawX + 14, boardY + boardH / 2, paint);

        // Sägemehl-Partikel fallen aus dem Schnitt
        float emitPhase = (phase * 3) % MathF.Tau;
        if (emitPhase > MathF.PI - 0.4f && emitPhase < MathF.PI + 0.4f)
        {
            for (int p = 0; p < particleRate; p++)
                _animationManager.AddWorkParticle(sawX + 14, boardY + boardH, new SKColor(0xD2, 0xB4, 0x8C));
        }

        // --- Fertige Bretter links unten ---
        for (int s = 0; s < productCount; s++)
        {
            paint.Color = (s % 2 == 0) ? new SKColor(0x8D, 0x65, 0x34, 200) : new SKColor(0xA0, 0x72, 0x3C, 200);
            canvas.DrawRect(left + 4, top + h - 10 - s * 7, 30, 5, paint);
        }

        // --- 4+ Worker: Zweite Säge versetzt ---
        if (activeWorkers >= 4)
        {
            float saw2Progress = (MathF.Sin(phase * 2.5f + MathF.PI) + 1) / 2;
            float saw2X = boardX + 10 + saw2Progress * (boardW - 50);
            float saw2VibY = MathF.Sin(phase * 20 + 1) * 1;
            paint.Color = new SKColor(0xD0, 0xD0, 0xD0);
            canvas.DrawRect(saw2X, boardY + boardH + 2 + saw2VibY, 24, 3, paint);
            paint.Color = new SKColor(0x5D, 0x40, 0x37);
            canvas.DrawRect(saw2X + 24, boardY + boardH - 4 + saw2VibY, 7, 10, paint);
        }
    }

    // ====================================================================
    // Klempner: L-förmiges Rohrsystem mit Wasserfluss
    // ====================================================================
    private void DrawPlumberScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        float pipeThick = 6;
        // Horizontales Rohr oben (Y=35%)
        float hPipeY = top + h * 0.32f;
        float hPipeX1 = left + 10;
        float hPipeX2 = left + w * 0.7f;

        // Vertikales Rohr links runter
        float vPipeX = left + 10;
        float vPipeY2 = top + h * 0.85f;

        // Vertikales Rohr rechts runter (L-Stück)
        float v2PipeX = hPipeX2;
        float v2PipeY2 = top + h * 0.85f;

        // --- Rohre zeichnen ---
        paint.Color = new SKColor(0x78, 0x90, 0x9C);
        // Horizontal
        canvas.DrawRect(hPipeX1, hPipeY, hPipeX2 - hPipeX1, pipeThick, paint);
        // Vertikal links
        canvas.DrawRect(vPipeX, hPipeY, pipeThick, vPipeY2 - hPipeY, paint);
        // Vertikal rechts
        canvas.DrawRect(v2PipeX, hPipeY, pipeThick, v2PipeY2 - hPipeY, paint);

        // Highlight oben (Lichtreflex)
        paint.Color = new SKColor(0x90, 0xA4, 0xAE);
        canvas.DrawRect(hPipeX1, hPipeY, hPipeX2 - hPipeX1, 2, paint);
        canvas.DrawRect(vPipeX, hPipeY, 2, vPipeY2 - hPipeY, paint);
        canvas.DrawRect(v2PipeX, hPipeY, 2, v2PipeY2 - hPipeY, paint);

        // Flansche an Verbindungspunkten
        paint.Color = new SKColor(0x60, 0x7D, 0x8B);
        canvas.DrawRect(hPipeX1 - 2, hPipeY - 2, pipeThick + 4, pipeThick + 4, paint);
        canvas.DrawRect(v2PipeX - 2, hPipeY - 2, pipeThick + 4, pipeThick + 4, paint);

        // --- Ventilrad rotiert zentral am T-Stück ---
        float valveX = (hPipeX1 + hPipeX2) / 2;
        float valveY = hPipeY + pipeThick / 2;
        float valveAngle = phase * 3;
        paint.Color = new SKColor(0xE0, 0x35, 0x35);
        // 4 Speichen
        for (int s = 0; s < 4; s++)
        {
            float a = valveAngle + s * MathF.PI / 2;
            float sx = valveX + MathF.Cos(a) * 8;
            float sy = valveY + MathF.Sin(a) * 8;
            paint.StrokeWidth = 2;
            canvas.DrawLine(valveX, valveY, sx, sy, paint);
        }
        // Radmitte
        paint.Color = new SKColor(0xC0, 0x28, 0x28);
        canvas.DrawCircle(valveX, valveY, 3, paint);

        // --- Wasser-Puls wandert durch Rohre ---
        float pulsePhase = (phase * 1.5f) % 3.0f; // 0..3 Zyklen: horizontal, runter links, runter rechts
        paint.Color = new SKColor(0x42, 0xA5, 0xF5, 160);

        if (pulsePhase < 1.0f)
        {
            // Puls auf horizontalem Rohr
            float px = hPipeX1 + pulsePhase * (hPipeX2 - hPipeX1);
            canvas.DrawRect(px - 8, hPipeY + 1, 20, pipeThick - 2, paint);
        }
        else if (pulsePhase < 2.0f)
        {
            // Puls auf linkem vertikalen Rohr (runter)
            float t = pulsePhase - 1.0f;
            float py = hPipeY + t * (vPipeY2 - hPipeY);
            canvas.DrawRect(vPipeX + 1, py - 8, pipeThick - 2, 20, paint);
        }
        else
        {
            // Puls auf rechtem vertikalen Rohr (runter)
            float t = pulsePhase - 2.0f;
            float py = hPipeY + t * (v2PipeY2 - hPipeY);
            canvas.DrawRect(v2PipeX + 1, py - 8, pipeThick - 2, 20, paint);
        }

        // --- Wassertropfen am Rohrende ---
        float dropPhase = (phase * 2) % MathF.Tau;
        if (dropPhase > MathF.PI - 0.3f && dropPhase < MathF.PI + 0.3f)
        {
            for (int p = 0; p < particleRate; p++)
            {
                _animationManager.AddWorkParticle(vPipeX + 3, vPipeY2, new SKColor(0x42, 0xA5, 0xF5));
                _animationManager.AddWorkParticle(v2PipeX + 3, v2PipeY2, new SKColor(0x42, 0xA5, 0xF5));
            }
        }

        // --- Rohrzange am rechten L-Stück ---
        float zangeY = hPipeY + (v2PipeY2 - hPipeY) * 0.5f;
        float zangeOpening = 3 + MathF.Sin(phase * 4) * 2;
        paint.Color = new SKColor(0xE0, 0x35, 0x35);
        paint.StrokeWidth = 2;
        canvas.DrawLine(v2PipeX + pipeThick + 4 - zangeOpening, zangeY - 8, v2PipeX + pipeThick + 2, zangeY + 4, paint);
        canvas.DrawLine(v2PipeX + pipeThick + 4 + zangeOpening, zangeY - 8, v2PipeX + pipeThick + 6, zangeY + 4, paint);
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        canvas.DrawCircle(v2PipeX + pipeThick + 4, zangeY - 4, 2, paint);

        // --- 4+ Worker: Dampfwolke + zweites Rohr ---
        if (activeWorkers >= 4)
        {
            float steamP = (phase * 1.2f) % 1.0f;
            float steamY = vPipeY2 - 8 - steamP * 25;
            byte steamA = (byte)Math.Clamp(140 - steamP * 130, 10, 140);
            paint.Color = new SKColor(0xFF, 0xFF, 0xFF, steamA);
            canvas.DrawCircle(vPipeX + 3, steamY, 5 + steamP * 3, paint);
            canvas.DrawCircle(vPipeX + 8, steamY - 5, 4 + steamP * 2, paint);
        }
    }

    // ====================================================================
    // Elektriker: 3 farbige Kabel mit Stromfluss
    // ====================================================================
    private void DrawElectricianScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        var cableColors = new[] {
            new SKColor(0xF4, 0x43, 0x36), // Rot
            new SKColor(0x42, 0xA5, 0xF5), // Blau
            new SKColor(0xFF, 0xC1, 0x07)  // Gelb
        };
        float[] cableYs = { top + h * 0.25f, top + h * 0.48f, top + h * 0.71f };

        // --- 3 Kabel mit Sinus-Welle ---
        for (int c = 0; c < 3; c++)
        {
            float cableY = cableYs[c];
            paint.Color = cableColors[c];
            paint.StrokeWidth = 3;

            // Kabel als Polylinie mit leichter Welle
            float prevX = left + 4;
            float prevY = cableY + MathF.Sin(phase * 0.8f + c) * 3;
            for (float x = left + 8; x < left + w - 4; x += 4)
            {
                float cy = cableY + MathF.Sin((x - left) * 0.04f + phase * 0.8f + c) * 3;
                canvas.DrawLine(prevX, prevY, x, cy, paint);
                prevX = x;
                prevY = cy;
            }

            // --- Strom-Puls wandert auf Kabel ---
            float pulsePos = ((phase * 2 + c * 1.2f) % 1.0f);
            float pulseX = left + 4 + pulsePos * (w - 8);
            float pulseY = cableY + MathF.Sin((pulseX - left) * 0.04f + phase * 0.8f + c) * 3;
            byte pulseAlpha = (byte)(180 + MathF.Sin(pulsePos * MathF.PI) * 75);
            paint.Color = new SKColor(0xFF, 0xFF, 0x80, pulseAlpha);
            canvas.DrawRect(pulseX - 7, pulseY - 3, 15, 6, paint);
            paint.Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(pulseAlpha / 2));
            canvas.DrawRect(pulseX - 3, pulseY - 1, 7, 3, paint);
        }

        // --- Verteilerdose zentral ---
        float boxX = left + w * 0.45f;
        float boxY = top + h * 0.38f;
        float boxW = 22;
        float boxH = 18;
        paint.Color = new SKColor(0x42, 0x42, 0x42);
        canvas.DrawRect(boxX, boxY, boxW, boxH, paint);
        paint.Color = new SKColor(0x55, 0x55, 0x55);
        canvas.DrawRect(boxX + 1, boxY + 1, boxW - 2, 2, paint);

        // 3 blinkende LEDs
        for (int led = 0; led < 3; led++)
        {
            bool on = MathF.Sin(phase * 5 + led * 2.1f) > 0;
            paint.Color = on ? new SKColor(0x00, 0xFF, 0x00, 220) : new SKColor(0x33, 0x55, 0x33, 120);
            canvas.DrawCircle(boxX + 6 + led * 5, boxY + boxH - 5, 2, paint);
        }

        // --- Schraubendreher dreht an Dose ---
        float screwAngle = phase * 4;
        float screwX = boxX + boxW + 4;
        float screwY = boxY + boxH / 2;
        // Griff
        paint.Color = new SKColor(0xF9, 0x73, 0x16);
        canvas.DrawRect(screwX + 6, screwY - 3, 10, 6, paint);
        // Schaft
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        canvas.DrawRect(screwX, screwY - 1, 8, 2, paint);
        // Spitze dreht (Kreisbewegung)
        float tipX = screwX - 2 + MathF.Cos(screwAngle) * 2;
        float tipY = screwY + MathF.Sin(screwAngle) * 2;
        paint.Color = new SKColor(0x90, 0x90, 0x90);
        canvas.DrawCircle(tipX, tipY, 2, paint);

        // Funken-Partikel an Schraubendreher-Spitze
        float sparkPhase = (phase * 4) % MathF.Tau;
        if (sparkPhase > MathF.PI - 0.3f && sparkPhase < MathF.PI + 0.3f)
        {
            for (int p = 0; p < particleRate; p++)
            {
                var sparkColor = (p % 2 == 0) ? new SKColor(0xFF, 0xC1, 0x07) : new SKColor(0xF9, 0x73, 0x16);
                _animationManager.AddWorkParticle(tipX, tipY, sparkColor);
            }
        }

        // --- 4+ Worker: Blitz-Flash ---
        if (activeWorkers >= 4)
        {
            float flashCycle = (phase * 0.5f) % 2.0f;
            if (flashCycle < 0.06f)
            {
                paint.Color = new SKColor(0xFF, 0xFF, 0xFF, 220);
                paint.StrokeWidth = 2;
                float fx = left + w * 0.2f;
                float fy = top + 8;
                canvas.DrawLine(fx, fy, fx + 5, fy + 10, paint);
                canvas.DrawLine(fx + 5, fy + 10, fx + 1, fy + 20, paint);
                canvas.DrawLine(fx + 1, fy + 20, fx + 6, fy + 30, paint);
            }
        }
    }

    // ====================================================================
    // Maler: Wand wird mit Roller gestrichen
    // ====================================================================
    private void DrawPainterScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        var wallColors = new[] {
            new SKColor(0xEC, 0x48, 0x99), new SKColor(0x42, 0xA5, 0xF5),
            new SKColor(0x66, 0xBB, 0x6A), new SKColor(0xFF, 0xCA, 0x28)
        };
        int colorIdx = ((int)(phase * 0.15f)) % wallColors.Length;
        var currentColor = wallColors[colorIdx];

        // --- Wand-Fläche rechts (70% Breite) ---
        float wallX = left + w * 0.2f;
        float wallW = w * 0.75f;
        float wallY = top + 4;
        float wallH = h * 0.82f;

        // Ungestrichene Wand (hell-grau)
        paint.Color = new SKColor(0xE0, 0xE0, 0xE0);
        canvas.DrawRect(wallX, wallY, wallW, wallH, paint);

        // Gestrichene Fläche wächst von links (zyklisch)
        float paintProgress = (phase * 0.3f) % 1.0f;
        float paintedW = paintProgress * wallW;
        paint.Color = currentColor.WithAlpha(180);
        canvas.DrawRect(wallX, wallY, paintedW, wallH, paint);

        // Zackiger Übergangsrand (nass-Look)
        paint.Color = currentColor.WithAlpha(120);
        for (int d = 0; d < (int)(wallH / 6); d++)
        {
            float dripX = wallX + paintedW + MathF.Sin(d * 1.3f + phase) * 4;
            float dripY = wallY + d * 6;
            canvas.DrawRect(dripX, dripY, 3 + MathF.Sin(d * 0.7f) * 2, 5, paint);
        }

        // --- Farbroller am Rand der gestrichenen Zone ---
        float rollerX = wallX + paintedW - 4;
        float rollerY = wallY + 8 + MathF.Sin(phase * 3) * (wallH - 24); // Vertikal-Oszillation
        // Roller-Walze
        paint.Color = currentColor;
        canvas.DrawRect(rollerX - 3, rollerY, 8, 22, paint);
        // Farb-Textur auf Walze
        paint.Color = currentColor.WithAlpha(220);
        canvas.DrawRect(rollerX - 2, rollerY + 2, 6, 2, paint);
        canvas.DrawRect(rollerX - 2, rollerY + 8, 6, 2, paint);
        canvas.DrawRect(rollerX - 2, rollerY + 14, 6, 2, paint);
        // Stiel
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        paint.StrokeWidth = 2;
        canvas.DrawLine(rollerX + 1, rollerY - 2, rollerX + 1, rollerY - 16, paint);
        canvas.DrawLine(rollerX + 1, rollerY - 16, rollerX + 10, rollerY - 22, paint);

        // --- Farbeimer unten links ---
        float bucketX = left + 4;
        float bucketY = top + h - 16;
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        canvas.DrawRect(bucketX, bucketY, 16, 14, paint);
        paint.Color = currentColor;
        canvas.DrawRect(bucketX + 2, bucketY + 2, 12, 6, paint);
        // Eimer-Henkel
        paint.Color = new SKColor(0x60, 0x60, 0x60);
        paint.StrokeWidth = 1;
        canvas.DrawLine(bucketX + 2, bucketY, bucketX + 8, bucketY - 6, paint);
        canvas.DrawLine(bucketX + 14, bucketY, bucketX + 8, bucketY - 6, paint);

        // --- Farbkleckse auf dem Boden ---
        for (int k = 0; k < 3; k++)
        {
            paint.Color = wallColors[(colorIdx + k + 1) % wallColors.Length].WithAlpha(100);
            canvas.DrawCircle(left + 30 + k * 25, top + h - 4, 4 + k, paint);
        }

        // Farbspritzer-Partikel bei Richtungswechsel
        float splatPhase = (phase * 3) % MathF.Tau;
        if (splatPhase > MathF.PI - 0.3f && splatPhase < MathF.PI + 0.3f)
        {
            for (int p = 0; p < particleRate; p++)
                _animationManager.AddWorkParticle(rollerX, rollerY + 10, currentColor);
        }

        // --- 4+ Worker: Zweiter Roller + Regenbogen-Streifen ---
        if (activeWorkers >= 4)
        {
            var color2 = wallColors[(colorIdx + 2) % wallColors.Length];
            float r2Y = wallY + wallH - 20 - MathF.Sin(phase * 2.5f + 1) * (wallH - 30);
            paint.Color = color2;
            canvas.DrawRect(rollerX + 12, r2Y, 6, 18, paint);
            paint.Color = new SKColor(0x78, 0x78, 0x78);
            paint.StrokeWidth = 2;
            canvas.DrawLine(rollerX + 15, r2Y - 2, rollerX + 15, r2Y - 14, paint);

            // Regenbogen-Streifen am oberen Rand
            for (int s = 0; s < 4; s++)
            {
                paint.Color = wallColors[s].WithAlpha(60);
                canvas.DrawRect(wallX, wallY - 6 + s * 2, wallW, 2, paint);
            }
        }
    }

    // ====================================================================
    // Dachdecker: Dachziegel werden auf schräge Fläche gelegt
    // ====================================================================
    private void DrawRooferScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        // --- Schräge Dachfläche (80% Szene) ---
        float roofLeft = left + 8;
        float roofRight = left + w - 8;
        float roofTop = top + 6;
        float roofBottom = top + h - 10;
        float roofW = roofRight - roofLeft;
        float roofH = roofBottom - roofTop;

        // Holzunterlage (schräg simuliert durch Farbe)
        paint.Color = new SKColor(0x8D, 0x6E, 0x63);
        canvas.DrawRect(roofLeft, roofTop, roofW, roofH, paint);

        // 4 Dachlatten horizontal
        paint.Color = new SKColor(0x6D, 0x4C, 0x41);
        paint.StrokeWidth = 2;
        for (int l = 0; l < 4; l++)
        {
            float latteY = roofTop + 10 + l * (roofH - 16) / 3;
            canvas.DrawLine(roofLeft, latteY, roofRight, latteY, paint);
        }

        // --- Dachziegel werden progressiv gelegt ---
        float tileW = 16;
        float tileH = 10;
        int tilesPerRow = (int)(roofW / (tileW + 2));
        int totalTiles = tilesPerRow * 4;
        float tileProgress = (phase * 0.4f) % 1.0f;
        int visibleTiles = (int)(tileProgress * totalTiles);

        for (int t = 0; t < Math.Min(visibleTiles, totalTiles); t++)
        {
            int row = t / tilesPerRow;
            int col = t % tilesPerRow;
            float tx = roofLeft + 2 + col * (tileW + 2) + (row % 2 == 1 ? tileW / 2 : 0);
            float ty = roofTop + 6 + row * (tileH - 2);

            if (tx + tileW > roofRight) continue;

            // Ziegel-Körper
            paint.Color = new SKColor(0xDC, 0x26, 0x26);
            canvas.DrawRect(tx, ty, tileW, tileH, paint);
            // Schatten-Unterkante
            paint.Color = new SKColor(0xBF, 0x20, 0x20);
            canvas.DrawRect(tx, ty + tileH - 2, tileW, 2, paint);
            // Glanz-Streifen oben
            paint.Color = new SKColor(0xEF, 0x53, 0x50, 100);
            canvas.DrawRect(tx + 1, ty + 1, tileW - 2, 2, paint);
        }

        // --- Nächster Ziegel schwebt von oben ein ---
        if (visibleTiles < totalTiles)
        {
            int nextRow = visibleTiles / tilesPerRow;
            int nextCol = visibleTiles % tilesPerRow;
            float ntx = roofLeft + 2 + nextCol * (tileW + 2) + (nextRow % 2 == 1 ? tileW / 2 : 0);
            float nty = roofTop + 6 + nextRow * (tileH - 2);
            float floatOffset = MathF.Abs(MathF.Sin(phase * 5)) * 12;

            paint.Color = new SKColor(0xDC, 0x26, 0x26, 200);
            canvas.DrawRect(ntx, nty - floatOffset, tileW, tileH, paint);
        }

        // --- Nagel-Hammer ---
        float hammerPhase = phase * 6;
        float hammerX = roofRight - 20;
        float hammerBaseY = roofTop + roofH * 0.3f;
        float hammerY = hammerBaseY - 4 + MathF.Sin(hammerPhase) * 5;
        // Stiel
        paint.Color = new SKColor(0x5D, 0x40, 0x37);
        paint.StrokeWidth = 2;
        canvas.DrawLine(hammerX, hammerY + 12, hammerX, hammerY, paint);
        // Kopf
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        canvas.DrawRect(hammerX - 5, hammerY - 4, 10, 5, paint);

        // Staub-Partikel bei Nagelschlag
        float hitPhase = hammerPhase % MathF.Tau;
        if (hitPhase > MathF.PI - 0.3f && hitPhase < MathF.PI + 0.3f)
        {
            for (int p = 0; p < particleRate; p++)
                _animationManager.AddWorkParticle(hammerX, hammerBaseY, new SKColor(0x9E, 0x9E, 0x9E, 180));
        }

        // --- 4+ Worker: Fließband-Effekt ---
        if (activeWorkers >= 4)
        {
            float beltPhase = (phase * 0.6f) % 1.0f;
            paint.Color = new SKColor(0xDC, 0x26, 0x26, 140);
            for (int b = 0; b < 3; b++)
            {
                float bx = roofRight + 5 - ((beltPhase + b * 0.33f) % 1.0f) * 30;
                canvas.DrawRect(bx, roofBottom - 6, tileW, tileH - 2, paint);
            }
        }
    }

    // ====================================================================
    // Bauunternehmer: Backsteinmauer wird hochgezogen
    // ====================================================================
    private void DrawContractorScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        float wallX = left + w * 0.15f;
        float wallW = w * 0.6f;
        float wallBottom = top + h - 6;
        float brickW = 14;
        float brickH = 7;
        float mortarGap = 2;
        int maxRows = (int)((h - 20) / (brickH + mortarGap));
        int bricksPerRow = (int)(wallW / (brickW + mortarGap));

        // Anzahl sichtbarer Reihen basiert auf Phase (zyklisch)
        float rowProgress = (phase * 0.25f) % 1.0f;
        int visibleRows = Math.Max(1, (int)(rowProgress * maxRows));

        // --- Backsteinmauer von unten ---
        for (int row = 0; row < visibleRows && row < maxRows; row++)
        {
            float rowY = wallBottom - (row + 1) * (brickH + mortarGap);
            float offset = (row % 2 == 1) ? brickW / 2 + 1 : 0; // Versatz

            for (int col = 0; col < bricksPerRow; col++)
            {
                float bx = wallX + offset + col * (brickW + mortarGap);
                if (bx + brickW > wallX + wallW + brickW / 2) continue;

                // Stein
                paint.Color = new SKColor(0xEA, 0x58, 0x0C);
                canvas.DrawRect(bx, rowY, brickW, brickH, paint);
                // Stein-Highlight oben
                paint.Color = new SKColor(0xF5, 0x73, 0x30, 80);
                canvas.DrawRect(bx, rowY, brickW, 2, paint);
            }

            // Fugen (Mörtel) horizontal
            if (row > 0)
            {
                paint.Color = new SKColor(0x9E, 0x9E, 0x9E);
                canvas.DrawRect(wallX, rowY + brickH, wallW, mortarGap, paint);
            }
        }

        // --- Kelle streicht Mörtel über oberste Reihe ---
        float topRowY = wallBottom - visibleRows * (brickH + mortarGap);
        float kellePhase = MathF.Sin(phase * 3);
        float kelleX = wallX + wallW * 0.3f + kellePhase * (wallW * 0.3f);

        // Mörtel-Spur
        paint.Color = new SKColor(0xBD, 0xBD, 0xBD, 150);
        canvas.DrawRect(wallX, topRowY - mortarGap, kelleX - wallX + 5, mortarGap + 1, paint);
        // Kelle
        paint.Color = new SKColor(0x78, 0x78, 0x78);
        canvas.DrawRect(kelleX - 5, topRowY - mortarGap - 3, 12, 4, paint);
        // Griff
        paint.Color = new SKColor(0x5D, 0x40, 0x37);
        canvas.DrawRect(kelleX, topRowY - mortarGap - 9, 3, 6, paint);

        // --- Neuer Stein schwebt von oben ein ---
        float stoneFloat = MathF.Abs(MathF.Sin(phase * 2)) * 18;
        paint.Color = new SKColor(0xEA, 0x58, 0x0C, 200);
        canvas.DrawRect(wallX + wallW * 0.5f, topRowY - brickH - 8 - stoneFloat, brickW, brickH, paint);

        // --- Zementsack rechts unten ---
        float sackX = left + w * 0.8f;
        float sackY = top + h - 14;
        paint.Color = new SKColor(0x9E, 0x9E, 0x9E);
        canvas.DrawRect(sackX, sackY, 18, 12, paint);
        paint.Color = new SKColor(0x8A, 0x8A, 0x8A);
        canvas.DrawRect(sackX + 3, sackY + 2, 12, 4, paint);

        // Staubwolken bei Platzierung
        float dustPhase = (phase * 3) % MathF.Tau;
        if (dustPhase > MathF.PI - 0.3f && dustPhase < MathF.PI + 0.3f)
        {
            for (int p = 0; p < particleRate; p++)
                _animationManager.AddWorkParticle(kelleX, topRowY - 2, new SKColor(0xBD, 0xBD, 0xBD));
        }

        // --- 4+ Worker: Wasserwaage + zweite Kelle ---
        if (activeWorkers >= 4)
        {
            // Wasserwaage
            float lvlX = wallX + wallW + 8;
            float lvlY = top + h * 0.4f;
            paint.Color = new SKColor(0xFF, 0xC1, 0x07);
            canvas.DrawRect(lvlX, lvlY, 6, 30, paint);
            float bubbleY = lvlY + 15 + MathF.Sin(phase * 1.5f) * 6;
            paint.Color = new SKColor(0x66, 0xBB, 0x6A, 200);
            canvas.DrawRect(lvlX + 1, bubbleY - 3, 4, 6, paint);
        }
    }

    // ====================================================================
    // Architekt: Bauplan wird progressiv gezeichnet
    // ====================================================================
    private void DrawArchitectScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        // --- Blaupause als große blaue Fläche ---
        float bpX = left + 6;
        float bpY = top + 4;
        float bpW = w * 0.82f;
        float bpH = h * 0.85f;

        // Papier-Hintergrund
        paint.Color = new SKColor(0x1E, 0x88, 0xE5, 50);
        canvas.DrawRect(bpX, bpY, bpW, bpH, paint);

        // Weißer Rand
        paint.Color = new SKColor(0xFF, 0xFF, 0xFF, 40);
        paint.StrokeWidth = 1;
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawRect(bpX, bpY, bpW, bpH, paint);
        paint.Style = SKPaintStyle.Fill;

        // Feines Raster
        paint.Color = new SKColor(0x42, 0xA5, 0xF5, 25);
        float gridSize = 16;
        for (float gx = bpX + gridSize; gx < bpX + bpW; gx += gridSize)
            canvas.DrawRect(gx, bpY, 1, bpH, paint);
        for (float gy = bpY + gridSize; gy < bpY + bpH; gy += gridSize)
            canvas.DrawRect(bpX, gy, bpW, 1, paint);

        // --- Grundriss wird progressiv gezeichnet ---
        float drawProgress = (phase * 0.2f) % 1.0f;
        paint.Color = new SKColor(0x42, 0xA5, 0xF5, 180);
        paint.StrokeWidth = 2;

        // Innenbereich für Grundriss
        float gpX = bpX + 14;
        float gpY = bpY + 10;
        float gpW = bpW - 28;
        float gpH = bpH - 20;

        // Außenrechteck (4 Linien, progressiv)
        // Linie 1: oben (0-0.2)
        float p1 = Math.Clamp(drawProgress / 0.2f, 0, 1);
        if (p1 > 0)
            canvas.DrawLine(gpX, gpY, gpX + gpW * p1, gpY, paint);

        // Linie 2: rechts (0.2-0.4)
        float p2 = Math.Clamp((drawProgress - 0.2f) / 0.2f, 0, 1);
        if (p2 > 0)
            canvas.DrawLine(gpX + gpW, gpY, gpX + gpW, gpY + gpH * p2, paint);

        // Linie 3: unten (0.4-0.6)
        float p3 = Math.Clamp((drawProgress - 0.4f) / 0.2f, 0, 1);
        if (p3 > 0)
            canvas.DrawLine(gpX + gpW, gpY + gpH, gpX + gpW - gpW * p3, gpY + gpH, paint);

        // Linie 4: links (0.6-0.8)
        float p4 = Math.Clamp((drawProgress - 0.6f) / 0.2f, 0, 1);
        if (p4 > 0)
            canvas.DrawLine(gpX, gpY + gpH, gpX, gpY + gpH - gpH * p4, paint);

        // Innen-Trennwände (0.8-1.0)
        float p5 = Math.Clamp((drawProgress - 0.8f) / 0.2f, 0, 1);
        if (p5 > 0)
        {
            // Vertikale Trennwand
            canvas.DrawLine(gpX + gpW * 0.45f, gpY, gpX + gpW * 0.45f, gpY + gpH * p5, paint);
            // Horizontale Trennwand (mit Tür-Öffnung)
            if (p5 > 0.3f)
            {
                float hw = gpW * 0.45f * Math.Clamp((p5 - 0.3f) / 0.7f, 0, 1);
                canvas.DrawLine(gpX, gpY + gpH * 0.55f, gpX + hw * 0.4f, gpY + gpH * 0.55f, paint);
                // Lücke (Tür)
                canvas.DrawLine(gpX + hw * 0.6f, gpY + gpH * 0.55f, gpX + hw, gpY + gpH * 0.55f, paint);
            }
            // Zweite Trennwand rechts
            if (p5 > 0.5f)
            {
                float vw = gpH * Math.Clamp((p5 - 0.5f) / 0.5f, 0, 1);
                canvas.DrawLine(gpX + gpW * 0.7f, gpY + gpH, gpX + gpW * 0.7f, gpY + gpH - vw * 0.6f, paint);
            }
        }

        // --- Bleistift am Endpunkt der aktuellen Linie ---
        float pencilX, pencilY;
        if (drawProgress < 0.2f)
        { pencilX = gpX + gpW * p1; pencilY = gpY; }
        else if (drawProgress < 0.4f)
        { pencilX = gpX + gpW; pencilY = gpY + gpH * p2; }
        else if (drawProgress < 0.6f)
        { pencilX = gpX + gpW - gpW * p3; pencilY = gpY + gpH; }
        else if (drawProgress < 0.8f)
        { pencilX = gpX; pencilY = gpY + gpH - gpH * p4; }
        else
        { pencilX = gpX + gpW * 0.45f; pencilY = gpY + gpH * p5; }

        // Bleistift
        paint.Color = new SKColor(0xFF, 0xCA, 0x28);
        canvas.DrawRect(pencilX - 1, pencilY - 12, 3, 8, paint);
        paint.Color = new SKColor(0xF5, 0xCB, 0xA7);
        canvas.DrawRect(pencilX - 0.5f, pencilY - 4, 2, 4, paint);

        // --- Lineal neben Bleistift ---
        paint.Color = new SKColor(0xB0, 0xB0, 0xB0, 80);
        canvas.DrawRect(pencilX + 6, pencilY - 8, 40, 4, paint);
        // Lineal-Markierungen
        paint.Color = new SKColor(0x80, 0x80, 0x80, 60);
        for (int m = 0; m < 8; m++)
            canvas.DrawRect(pencilX + 8 + m * 5, pencilY - 7, 1, 2, paint);

        // Radiergummi-Krümel bei Linienwechsel
        float crumbPhase = (phase * 2) % MathF.Tau;
        if (crumbPhase > MathF.PI - 0.2f && crumbPhase < MathF.PI + 0.2f)
        {
            for (int p = 0; p < particleRate; p++)
                _animationManager.AddWorkParticle(pencilX, pencilY, new SKColor(0xF5, 0xF5, 0xF5));
        }

        // --- 4+ Worker: Zirkel-Bogen ---
        if (activeWorkers >= 4)
        {
            float arcProgress = (phase * 0.3f) % 1.0f;
            float arcCx = gpX + gpW * 0.7f;
            float arcCy = gpY + gpH * 0.35f;
            float arcRadius = 14;
            paint.Color = new SKColor(0x00, 0xBC, 0xD4, 100);
            paint.StrokeWidth = 1;
            paint.Style = SKPaintStyle.Stroke;
            using (var path = new SKPath())
            {
                path.AddArc(new SKRect(arcCx - arcRadius, arcCy - arcRadius,
                    arcCx + arcRadius, arcCy + arcRadius), 0, arcProgress * 300);
                canvas.DrawPath(path, paint);
            }
            paint.Style = SKPaintStyle.Fill;
        }
    }

    // ====================================================================
    // Generalunternehmer: Goldener Vertrag mit Stempel + Shimmer
    // ====================================================================
    private void DrawGeneralContractorScene(SKCanvas canvas, float left, float top, float w, float h,
        float speed, int particleRate, int productCount, int activeWorkers)
    {
        using var paint = new SKPaint { IsAntialias = false };
        float phase = _workerAnimPhase * speed;

        // --- Goldener Vertrag zentral ---
        float docW = 56;
        float docH = 40;
        float docX = left + (w - docW) / 2;
        float docY = top + (h - docH) / 2 - 6;

        // Papier
        paint.Color = new SKColor(0xFF, 0xF8, 0xE1);
        canvas.DrawRect(docX, docY, docW, docH, paint);
        // Goldener Rand
        paint.Color = new SKColor(0xFF, 0xD7, 0x00);
        paint.StrokeWidth = 2;
        paint.Style = SKPaintStyle.Stroke;
        canvas.DrawRect(docX, docY, docW, docH, paint);
        paint.Style = SKPaintStyle.Fill;

        // Text-Linien (graue Streifen)
        paint.Color = new SKColor(0xBD, 0xBD, 0xBD, 120);
        for (int l = 0; l < 4; l++)
        {
            float lw = (l == 3) ? docW * 0.5f : docW * 0.7f; // Letzte Zeile kürzer
            canvas.DrawRect(docX + 6, docY + 6 + l * 7, lw, 2, paint);
        }

        // Siegel unten-rechts (goldener Kreis + Stern)
        float sealX = docX + docW - 12;
        float sealY = docY + docH - 10;
        paint.Color = new SKColor(0xFF, 0xD7, 0x00, 180);
        canvas.DrawCircle(sealX, sealY, 6, paint);
        paint.Color = new SKColor(0xFF, 0xB3, 0x00);
        // Stern-Zacken (vereinfacht als 4 Linien)
        paint.StrokeWidth = 1;
        for (int s = 0; s < 4; s++)
        {
            float angle = s * MathF.PI / 2 + MathF.PI / 4;
            canvas.DrawLine(sealX, sealY,
                sealX + MathF.Cos(angle) * 4, sealY + MathF.Sin(angle) * 4, paint);
        }

        // --- Stempel drückt auf Dokument ---
        float stampCycle = (phase * 1.5f % MathF.Tau) / MathF.Tau; // 0..1
        float stampX = docX + docW * 0.35f;
        float stampBaseY = docY + docH * 0.5f;
        float stampY;
        if (stampCycle < 0.35f)
            stampY = stampBaseY - 30 - stampCycle * 20; // Hebt sich hoch
        else if (stampCycle < 0.45f)
            stampY = stampBaseY - 30 - 7 + (stampCycle - 0.35f) * 370; // Schnell runter (Bump)
        else
            stampY = stampBaseY; // Unten (auf dem Papier)

        // Stempel-Griff
        paint.Color = new SKColor(0xFF, 0xD7, 0x00);
        canvas.DrawRect(stampX - 3, stampY - 14, 6, 14, paint);
        // Stempel-Fläche
        paint.Color = new SKColor(0xFF, 0xB3, 0x00);
        canvas.DrawRect(stampX - 7, stampY, 14, 4, paint);

        // Gold-Abdruck erscheint nach Stempel
        if (stampCycle > 0.45f)
        {
            byte abdAlpha = (byte)Math.Clamp((stampCycle - 0.45f) * 300, 0, 160);
            paint.Color = new SKColor(0xFF, 0xD7, 0x00, abdAlpha);
            canvas.DrawCircle(stampX, stampBaseY + 2, 5, paint);
            paint.Color = new SKColor(0xFF, 0xB3, 0x00, (byte)(abdAlpha / 2));
            canvas.DrawRect(stampX - 4, stampBaseY, 8, 4, paint);
        }

        // Gold-Partikel bei Stempel-Aufschlag
        if (stampCycle > 0.43f && stampCycle < 0.52f)
        {
            for (int p = 0; p < particleRate + 1; p++)
                _animationManager.AddWorkParticle(stampX, stampBaseY, new SKColor(0xFF, 0xD7, 0x00));
        }

        // --- Gold-Shimmer wandert diagonal über Szene ---
        float shimmerPhase = (phase * 0.8f) % 1.0f;
        float shimmerX = left + shimmerPhase * w;
        float shimmerY = top + shimmerPhase * h;
        byte shimmerAlpha = (byte)(60 + MathF.Sin(shimmerPhase * MathF.PI) * 60);
        paint.Color = new SKColor(0xFF, 0xF0, 0x70, shimmerAlpha);
        // Diagonaler Shimmer-Streifen
        paint.StrokeWidth = 3;
        canvas.DrawLine(shimmerX - 10, shimmerY - 10, shimmerX + 10, shimmerY + 10, paint);
        paint.Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(shimmerAlpha / 3));
        paint.StrokeWidth = 1;
        canvas.DrawLine(shimmerX - 8, shimmerY - 8, shimmerX + 8, shimmerY + 8, paint);

        // --- Goldmünzen rechts ---
        for (int c = 0; c < productCount; c++)
        {
            float cx = left + w - 18;
            float cy = top + h - 10 - c * 8;
            paint.Color = new SKColor(0xFF, 0xD7, 0x00);
            canvas.DrawCircle(cx, cy, 5, paint);
            paint.Color = new SKColor(0xFF, 0xB3, 0x00);
            canvas.DrawCircle(cx, cy, 3, paint);
            // "$" Symbol
            paint.Color = new SKColor(0xCC, 0x99, 0x00);
            paint.StrokeWidth = 1;
            canvas.DrawLine(cx, cy - 3, cx, cy + 3, paint);
        }

        // --- 4+ Worker: Gold-Regen + zweiter Stempel + Stern ---
        if (activeWorkers >= 4)
        {
            // Gold-Regen (permanente Partikel)
            float rainPhase = (phase * 3) % MathF.Tau;
            if (rainPhase < 0.5f)
            {
                for (int p = 0; p < 2; p++)
                    _animationManager.AddWorkParticle(
                        left + 20 + p * 30, top + 4, new SKColor(0xFF, 0xD7, 0x00));
            }

            // Zweiter Stempel (kleiner, links)
            float s2Cycle = ((phase * 1.5f + MathF.PI) % MathF.Tau) / MathF.Tau;
            float s2X = docX + 8;
            float s2Y = s2Cycle < 0.45f ? stampBaseY - 20 + MathF.Sin(s2Cycle * 7) * 8 : stampBaseY;
            paint.Color = new SKColor(0xFF, 0xD7, 0x00, 180);
            canvas.DrawRect(s2X - 2, s2Y - 10, 4, 10, paint);
            paint.Color = new SKColor(0xFF, 0xB3, 0x00, 180);
            canvas.DrawRect(s2X - 5, s2Y, 10, 3, paint);

            // Stern-Effekt oben links
            float starPhase = phase * 2;
            float starX = left + 14;
            float starY = top + 14;
            float starSize = 6 + MathF.Sin(starPhase) * 2;
            paint.Color = new SKColor(0xFF, 0xD7, 0x00, 160);
            for (int s = 0; s < 4; s++)
            {
                float a = starPhase * 0.3f + s * MathF.PI / 2;
                canvas.DrawLine(starX, starY,
                    starX + MathF.Cos(a) * starSize, starY + MathF.Sin(a) * starSize, paint);
            }
        }
    }

    // ====================================================================
    // SkiaSharp LinearProgress Handler
    // ====================================================================

    private void OnPaintWorkshopMainLevelProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is WorkshopViewModel vm)
            progress = (float)vm.LevelProgress;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: true);
    }

    private async void OnUpgradeEffect(object? sender, EventArgs e)
    {
        // Level-Badge Scale-Pop Animation
        var badge = this.FindControl<Border>("LevelBadge");
        if (badge != null)
        {
            await AnimationHelper.ScaleUpDownAsync(badge, 1.0, 1.25, TimeSpan.FromMilliseconds(250));
        }

        // Konfetti-Partikel bei Upgrade
        if (_workshopCanvas != null)
        {
            var bounds = _workshopCanvas.Bounds;
            _animationManager.AddLevelUpConfetti((float)bounds.Width / 2, (float)bounds.Height / 2);
        }
    }
}

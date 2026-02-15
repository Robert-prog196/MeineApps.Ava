using System;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Avalonia.Threading;
using HandwerkerImperium.Graphics;
using HandwerkerImperium.ViewModels;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class ResearchView : UserControl
{
    // Forschungslabor Rendering
    private readonly ResearchLabRenderer _labRenderer = new();
    private DispatcherTimer? _renderTimer;
    private SKCanvasView? _researchCanvas;
    private ResearchViewModel? _vm;
    private DateTime _lastRenderTime = DateTime.UtcNow;

    public ResearchView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes VM abmelden
        if (_vm != null)
        {
            _vm = null;
        }

        // Neues VM abonnieren + Canvas finden
        if (DataContext is ResearchViewModel vm)
        {
            _vm = vm;

            _researchCanvas = this.FindControl<SKCanvasView>("ResearchCanvas");
            if (_researchCanvas != null)
            {
                _researchCanvas.PaintSurface += OnResearchPaintSurface;
                StartRenderLoop();
            }
        }
        else
        {
            // Kein passendes VM → Render-Loop stoppen
            StopRenderLoop();
        }
    }

    /// <summary>
    /// Startet den Render-Timer für das Forschungslabor (20 fps).
    /// </summary>
    private void StartRenderLoop()
    {
        _renderTimer?.Stop();
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) }; // 20 fps
        _renderTimer.Tick += (_, _) =>
        {
            _researchCanvas?.InvalidateSurface();
        };
        _renderTimer.Start();
    }

    /// <summary>
    /// Stoppt den Render-Loop (z.B. bei View-Wechsel).
    /// </summary>
    private void StopRenderLoop()
    {
        _renderTimer?.Stop();
        _renderTimer = null;
    }

    /// <summary>
    /// PaintSurface-Handler: Zeichnet das Forschungslabor mit aktuellen Daten vom ViewModel.
    /// </summary>
    private void OnResearchPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        // Delta-Zeit berechnen
        var now = DateTime.UtcNow;
        var delta = (now - _lastRenderTime).TotalSeconds;
        _lastRenderTime = now;

        // Daten vom ViewModel holen
        bool hasActive = _vm?.HasActiveResearch ?? false;
        float progress = (float)(_vm?.ActiveResearchProgress ?? 0.0);

        _labRenderer.Render(canvas, bounds, hasActive, progress, (float)delta);
    }
}

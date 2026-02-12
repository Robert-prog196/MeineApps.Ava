using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using BomberBlast.ViewModels;
using SkiaSharp;
using Avalonia.Labs.Controls;

namespace BomberBlast.Views;

public partial class GameView : UserControl
{
    private int _renderWidth, _renderHeight;
    private GameViewModel? _subscribedVm;
    private DispatcherTimer? _renderTimer;

    public GameView()
    {
        InitializeComponent();

        GameCanvas.PaintSurface += OnPaintSurface;
        GameCanvas.PointerPressed += OnPointerPressed;
        GameCanvas.PointerMoved += OnPointerMoved;
        GameCanvas.PointerReleased += OnPointerReleased;

        // Keyboard input for desktop
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        // InvalidateCanvasRequested bei DataContext-Wechsel abonnieren
        DataContextChanged += OnDataContextChanged;
    }

    private GameViewModel? ViewModel => DataContext as GameViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes ViewModel abmelden
        if (_subscribedVm != null)
        {
            _subscribedVm.InvalidateCanvasRequested -= OnInvalidateRequested;
            _subscribedVm = null;
        }

        // Neues ViewModel abonnieren
        if (DataContext is GameViewModel vm)
        {
            _subscribedVm = vm;
            vm.InvalidateCanvasRequested += OnInvalidateRequested;
        }
    }

    private void OnInvalidateRequested()
    {
        // Initialen Frame rendern + Render-Timer starten
        GameCanvas.InvalidateSurface();
        StartRenderTimer();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RENDER-TIMER (~60fps, selbes Pattern wie CelebrationOverlay)
    // ═══════════════════════════════════════════════════════════════════════

    private void StartRenderTimer()
    {
        if (_renderTimer != null) return;

        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();
    }

    private void StopRenderTimer()
    {
        if (_renderTimer == null) return;
        _renderTimer.Stop();
        _renderTimer.Tick -= OnRenderTick;
        _renderTimer = null;
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        if (ViewModel?.IsGameLoopRunning == true)
        {
            GameCanvas.InvalidateSurface();
        }
        else
        {
            // Game-Loop gestoppt → Timer stoppen
            StopRenderTimer();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RENDERING
    // ═══════════════════════════════════════════════════════════════════════

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;

        // canvas.LocalClipBounds statt e.Info fuer korrekte DPI-Dimensionen
        var bounds = canvas.LocalClipBounds;
        int width = (int)bounds.Width;
        int height = (int)bounds.Height;

        // Fallback auf e.Info falls Bounds ungueltig
        if (width <= 0 || height <= 0)
        {
            width = e.Info.Width;
            height = e.Info.Height;
        }

        // Fuer Touch-Koordinaten-Konvertierung speichern
        _renderWidth = width;
        _renderHeight = height;

        ViewModel?.OnPaintSurface(canvas, width, height);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INPUT
    // ═══════════════════════════════════════════════════════════════════════

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel is null) return;

        var point = e.GetPosition(GameCanvas);
        var boundsWidth = GameCanvas.Bounds.Width;
        var boundsHeight = GameCanvas.Bounds.Height;
        float scaleX = boundsWidth > 0 ? _renderWidth / (float)boundsWidth : 1f;
        float scaleY = boundsHeight > 0 ? _renderHeight / (float)boundsHeight : 1f;

        float x = (float)(point.X * scaleX);
        float y = (float)(point.Y * scaleY);

        ViewModel.OnPointerPressed(x, y, _renderWidth, _renderHeight);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (ViewModel is null) return;

        var point = e.GetPosition(GameCanvas);
        var boundsWidth = GameCanvas.Bounds.Width;
        var boundsHeight = GameCanvas.Bounds.Height;
        float scaleX = boundsWidth > 0 ? _renderWidth / (float)boundsWidth : 1f;
        float scaleY = boundsHeight > 0 ? _renderHeight / (float)boundsHeight : 1f;

        float x = (float)(point.X * scaleX);
        float y = (float)(point.Y * scaleY);

        ViewModel.OnPointerMoved(x, y);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ViewModel is null) return;

        ViewModel.OnPointerReleased();
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.OnKeyDown(e.Key);
        e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.OnKeyUp(e.Key);
        e.Handled = true;
    }
}

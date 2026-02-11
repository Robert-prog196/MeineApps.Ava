using Avalonia.Controls;
using Avalonia.Input;
using BomberBlast.ViewModels;
using SkiaSharp;
using Avalonia.Labs.Controls;

namespace BomberBlast.Views;

public partial class GameView : UserControl
{
    private int _renderWidth, _renderHeight;

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

        // Render-Loop wird vom GameViewModel gesteuert via InvalidateCanvasRequested
        // Kein separater DispatcherTimer noetig (vermeidet doppeltes Rendering)
    }

    private GameViewModel? ViewModel => DataContext as GameViewModel;

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;

        // Use canvas.LocalClipBounds instead of e.Info to get correct dimensions.
        // e.Info.Width/Height reports physical pixels (DPI-scaled), but the canvas
        // coordinate system may be in logical pixels (if DPI transform is applied).
        var bounds = canvas.LocalClipBounds;
        int width = (int)bounds.Width;
        int height = (int)bounds.Height;

        // Fallback to e.Info if bounds are invalid
        if (width <= 0 || height <= 0)
        {
            width = e.Info.Width;
            height = e.Info.Height;
        }

        // Store for touch coordinate conversion
        _renderWidth = width;
        _renderHeight = height;

        ViewModel?.OnPaintSurface(canvas, width, height);
    }

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

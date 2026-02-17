using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.ViewModels;
using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class OrderView : UserControl
{
    public OrderView()
    {
        InitializeComponent();
    }

    // ====================================================================
    // SkiaSharp LinearProgress Handler
    // ====================================================================

    private void OnPaintOrderProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is OrderViewModel vm)
            progress = (float)vm.Progress;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: true);
    }
}

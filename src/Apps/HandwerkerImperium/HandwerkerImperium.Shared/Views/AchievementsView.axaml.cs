using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.ViewModels;
using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class AchievementsView : UserControl
{
    public AchievementsView()
    {
        InitializeComponent();
    }

    // ====================================================================
    // SkiaSharp LinearProgress Handler
    // ====================================================================

    private void OnPaintOverallProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is AchievementsViewModel vm)
            progress = (float)vm.OverallProgress;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: true);
    }

    private void OnPaintAchievementProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (sender is SKCanvasView canvasView && canvasView.DataContext is AchievementDisplayModel model)
            progress = (float)model.ProgressFraction;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: false);
    }
}

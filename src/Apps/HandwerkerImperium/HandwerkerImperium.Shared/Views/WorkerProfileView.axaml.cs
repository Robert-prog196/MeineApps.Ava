using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.ViewModels;
using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class WorkerProfileView : UserControl
{
    public WorkerProfileView()
    {
        InitializeComponent();
    }

    // ====================================================================
    // SkiaSharp LinearProgress Handler
    // ====================================================================

    private void OnPaintMoodProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is WorkerProfileViewModel vm)
            progress = (float)vm.MoodPercent / 100f;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0x22, 0xC5, 0x5E), new SKColor(0x16, 0xA3, 0x4A),
            showText: false, glowEnabled: true);
    }

    private void OnPaintFatigueProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is WorkerProfileViewModel vm)
            progress = (float)vm.FatiguePercent / 100f;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xEF, 0x44, 0x44), new SKColor(0xDC, 0x26, 0x26),
            showText: false, glowEnabled: true);
    }

    private void OnPaintXpProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is WorkerProfileViewModel vm)
            progress = (float)vm.XpPercent / 100f;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: true);
    }

    private void OnPaintTrainingProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        float progress = 0f;
        if (DataContext is WorkerProfileViewModel vm)
            progress = (float)vm.TrainingProgressPercent / 100f;

        LinearProgressVisualization.Render(canvas, bounds, progress,
            new SKColor(0xD9, 0x77, 0x06), new SKColor(0xF5, 0x9E, 0x0B),
            showText: false, glowEnabled: true);
    }
}

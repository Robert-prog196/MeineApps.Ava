using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using FitnessRechner.ViewModels;
using MeineApps.UI.SkiaSharp;
using SkiaSharp;

namespace FitnessRechner.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        try
        {
            if (DataContext is MainViewModel vm)
                await vm.OnAppearingAsync();
        }
        catch (Exception)
        {
            // async void darf keine Exception werfen → App-Crash verhindern
        }
    }

    /// <summary>
    /// XP-Level-Fortschritt zeichnen (grün → accent).
    /// </summary>
    private void OnPaintLevelProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var bounds = canvas.LocalClipBounds;

        if (DataContext is not MainViewModel vm) return;

        LinearProgressVisualization.Render(canvas, bounds,
            (float)vm.LevelProgress,
            new SKColor(0x22, 0xC5, 0x5E), // Grün Start
            new SKColor(0x16, 0xA3, 0x4A), // Grün End
            showText: false, glowEnabled: true);
    }

    /// <summary>
    /// Challenge-Fortschritt zeichnen (indigo → lila).
    /// </summary>
    private void OnPaintChallengeProgress(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var bounds = canvas.LocalClipBounds;

        if (DataContext is not MainViewModel vm) return;

        LinearProgressVisualization.Render(canvas, bounds,
            (float)vm.ChallengeProgressValue,
            new SKColor(0x63, 0x66, 0xF1), // Indigo Start
            new SKColor(0x8B, 0x5C, 0xF6), // Lila End
            showText: false, glowEnabled: false);
    }
}

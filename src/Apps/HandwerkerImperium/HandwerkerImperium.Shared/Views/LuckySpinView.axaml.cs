using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.Graphics;
using HandwerkerImperium.ViewModels;
using SkiaSharp;

namespace HandwerkerImperium.Views;

public partial class LuckySpinView : UserControl
{
    private readonly LuckySpinWheelRenderer _wheelRenderer = new();

    public LuckySpinView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Zeichnet das Glücksrad via LuckySpinWheelRenderer.
    /// </summary>
    private void OnPaintWheel(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var bounds = canvas.LocalClipBounds;

        if (DataContext is not LuckySpinViewModel vm) return;

        // Bestimme hervorgehobenes Segment (nur wenn Spin fertig UND Gewinn angezeigt)
        int? highlightedSegment = null;
        if (vm.ShowPrize && vm.LastPrizeType != null)
        {
            highlightedSegment = (int)vm.LastPrizeType.Value;
        }

        _wheelRenderer.Render(canvas, bounds, vm.SpinAngle, highlightedSegment);
    }
}

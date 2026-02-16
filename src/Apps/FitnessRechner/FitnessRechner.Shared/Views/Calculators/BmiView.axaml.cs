using Avalonia.Controls;
using Avalonia.Labs.Controls;
using FitnessRechner.Graphics;
using FitnessRechner.ViewModels.Calculators;

namespace FitnessRechner.Views.Calculators;

public partial class BmiView : UserControl
{
    public BmiView()
    {
        InitializeComponent();
    }

    private void OnPaintBmiGauge(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        if (DataContext is BmiViewModel vm && vm.HasResult)
        {
            BmiGaugeRenderer.Render(canvas, canvas.LocalClipBounds,
                (float)vm.BmiValue, vm.HasResult);
        }
    }
}

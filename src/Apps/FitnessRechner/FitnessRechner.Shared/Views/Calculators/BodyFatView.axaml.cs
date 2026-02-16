using Avalonia.Controls;
using Avalonia.Labs.Controls;
using FitnessRechner.Graphics;
using FitnessRechner.ViewModels.Calculators;

namespace FitnessRechner.Views.Calculators;

public partial class BodyFatView : UserControl
{
    public BodyFatView()
    {
        InitializeComponent();
    }

    private void OnPaintBodyFat(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        if (DataContext is BodyFatViewModel vm && vm.HasResult)
        {
            BodyFatRenderer.Render(canvas, canvas.LocalClipBounds,
                (float)vm.BodyFatValue, vm.IsMale, vm.HasResult);
        }
    }
}

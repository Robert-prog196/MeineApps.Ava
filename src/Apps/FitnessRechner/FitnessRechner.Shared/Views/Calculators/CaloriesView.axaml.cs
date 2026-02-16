using Avalonia.Controls;
using Avalonia.Labs.Controls;
using FitnessRechner.Graphics;
using FitnessRechner.ViewModels.Calculators;

namespace FitnessRechner.Views.Calculators;

public partial class CaloriesView : UserControl
{
    public CaloriesView()
    {
        InitializeComponent();
    }

    private void OnPaintCalorieRings(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);
        if (DataContext is CaloriesViewModel vm && vm.HasResult && vm.Result != null)
        {
            CalorieRingRenderer.Render(canvas, canvas.LocalClipBounds,
                (float)vm.Result.Bmr, (float)vm.Result.Tdee,
                (float)vm.Result.WeightLossCalories, (float)vm.Result.WeightGainCalories,
                vm.HasResult);
        }
    }
}

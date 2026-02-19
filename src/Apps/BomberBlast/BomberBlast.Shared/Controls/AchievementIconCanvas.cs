using Avalonia;
using Avalonia.Labs.Controls;
using BomberBlast.Graphics;

namespace BomberBlast.Controls;

/// <summary>
/// Bindbare SKCanvasView fuer Achievement-Icons im ItemsControl.
/// Rendert Kategorie-farbige Trophy (freigeschaltet) oder Schloss mit Fortschrittsring (gesperrt).
/// </summary>
public class AchievementIconCanvas : SKCanvasView
{
    public static readonly StyledProperty<int> CategoryIndexProperty =
        AvaloniaProperty.Register<AchievementIconCanvas, int>(nameof(CategoryIndex));

    public static readonly StyledProperty<bool> IsAchievementUnlockedProperty =
        AvaloniaProperty.Register<AchievementIconCanvas, bool>(nameof(IsAchievementUnlocked));

    public static readonly StyledProperty<float> ProgressFractionProperty =
        AvaloniaProperty.Register<AchievementIconCanvas, float>(nameof(ProgressFraction));

    public int CategoryIndex
    {
        get => GetValue(CategoryIndexProperty);
        set => SetValue(CategoryIndexProperty, value);
    }

    public bool IsAchievementUnlocked
    {
        get => GetValue(IsAchievementUnlockedProperty);
        set => SetValue(IsAchievementUnlockedProperty, value);
    }

    public float ProgressFraction
    {
        get => GetValue(ProgressFractionProperty);
        set => SetValue(ProgressFractionProperty, value);
    }

    static AchievementIconCanvas()
    {
        CategoryIndexProperty.Changed.AddClassHandler<AchievementIconCanvas>((x, _) => x.InvalidateSurface());
        IsAchievementUnlockedProperty.Changed.AddClassHandler<AchievementIconCanvas>((x, _) => x.InvalidateSurface());
        ProgressFractionProperty.Changed.AddClassHandler<AchievementIconCanvas>((x, _) => x.InvalidateSurface());
    }

    public AchievementIconCanvas()
    {
        PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        var bounds = canvas.LocalClipBounds;
        float cx = bounds.MidX;
        float cy = bounds.MidY;
        float size = Math.Min(bounds.Width, bounds.Height);
        AchievementIconRenderer.Render(canvas, cx, cy, size,
            CategoryIndex, IsAchievementUnlocked, ProgressFraction);
    }
}

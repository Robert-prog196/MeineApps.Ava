using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace MeineApps.UI.Behaviors;

/// <summary>
/// Skaliert ein Control beim Drücken herunter (Micro-Animation für taktiles Feedback).
/// </summary>
public class TapScaleBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> PressedScaleProperty =
        AvaloniaProperty.Register<TapScaleBehavior, double>(nameof(PressedScale), 0.92);

    public double PressedScale
    {
        get => GetValue(PressedScaleProperty);
        set => SetValue(PressedScaleProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null) return;

        AssociatedObject.RenderTransformOrigin = RelativePoint.Center;
        AssociatedObject.RenderTransform ??= new ScaleTransform(1, 1);

        AssociatedObject.PointerPressed += OnPointerPressed;
        AssociatedObject.PointerReleased += OnPointerReleased;
        AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        }
        base.OnDetaching();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject?.RenderTransform is ScaleTransform st)
        {
            st.ScaleX = PressedScale;
            st.ScaleY = PressedScale;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => ResetScale();
    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => ResetScale();

    private void ResetScale()
    {
        if (AssociatedObject?.RenderTransform is ScaleTransform st)
        {
            st.ScaleX = 1;
            st.ScaleY = 1;
        }
    }
}

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Xaml.Interactivity;

namespace MeineApps.Core.Ava.Behaviors;

/// <summary>
/// Behavior that scales a control when pressed/tapped with an elastic bounce effect
/// </summary>
public class TapScaleBehavior : Behavior<Control>
{
    /// <summary>
    /// Scale factor when pressed (0.95 = 95% of original size)
    /// </summary>
    public static readonly StyledProperty<double> PressedScaleProperty =
        AvaloniaProperty.Register<TapScaleBehavior, double>(nameof(PressedScale), 0.95);

    /// <summary>
    /// Animation duration in milliseconds
    /// </summary>
    public static readonly StyledProperty<int> DurationProperty =
        AvaloniaProperty.Register<TapScaleBehavior, int>(nameof(Duration), 100);

    /// <summary>
    /// Whether to use elastic easing for bounce effect
    /// </summary>
    public static readonly StyledProperty<bool> UseElasticEasingProperty =
        AvaloniaProperty.Register<TapScaleBehavior, bool>(nameof(UseElasticEasing), true);

    public double PressedScale
    {
        get => GetValue(PressedScaleProperty);
        set => SetValue(PressedScaleProperty, value);
    }

    public int Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public bool UseElasticEasing
    {
        get => GetValue(UseElasticEasingProperty);
        set => SetValue(UseElasticEasingProperty, value);
    }

    private ScaleTransform? _scaleTransform;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null) return;

        // Create scale transform if not exists
        _scaleTransform = new ScaleTransform(1, 1);
        AssociatedObject.RenderTransform = _scaleTransform;
        AssociatedObject.RenderTransformOrigin = RelativePoint.Center;

        // Subscribe to pointer events
        AssociatedObject.PointerPressed += OnPointerPressed;
        AssociatedObject.PointerReleased += OnPointerReleased;
        AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject == null) return;

        AssociatedObject.PointerPressed -= OnPointerPressed;
        AssociatedObject.PointerReleased -= OnPointerReleased;
        AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AnimateScale(PressedScale);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        AnimateScale(1.0);
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        AnimateScale(1.0);
    }

    private async void AnimateScale(double targetScale)
    {
        if (_scaleTransform == null || AssociatedObject == null) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(Duration),
            Easing = UseElasticEasing && targetScale == 1.0
                ? new ElasticEaseOut()
                : new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, _scaleTransform.ScaleX),
                        new Setter(ScaleTransform.ScaleYProperty, _scaleTransform.ScaleY)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, targetScale),
                        new Setter(ScaleTransform.ScaleYProperty, targetScale)
                    }
                }
            }
        };

        await animation.RunAsync(_scaleTransform);
    }
}

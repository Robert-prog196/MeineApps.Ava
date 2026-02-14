using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace MeineApps.UI.Behaviors;

/// <summary>
/// Behavior das Elemente mit versetztem Delay einblendet.
/// Erkennt automatisch den Index im übergeordneten ItemsControl/Panel
/// und berechnet Delay = Index × StaggerDelay.
/// </summary>
public class StaggerFadeInBehavior : Behavior<Control>
{
    public static readonly StyledProperty<int> StaggerDelayProperty =
        AvaloniaProperty.Register<StaggerFadeInBehavior, int>(nameof(StaggerDelay), 50);

    public static readonly StyledProperty<int> BaseDurationProperty =
        AvaloniaProperty.Register<StaggerFadeInBehavior, int>(nameof(BaseDuration), 300);

    public static readonly StyledProperty<int> FixedIndexProperty =
        AvaloniaProperty.Register<StaggerFadeInBehavior, int>(nameof(FixedIndex), -1);

    /// <summary>Verzögerung pro Element in Millisekunden (Standard: 50).</summary>
    public int StaggerDelay
    {
        get => GetValue(StaggerDelayProperty);
        set => SetValue(StaggerDelayProperty, value);
    }

    /// <summary>Animationsdauer in Millisekunden (Standard: 300).</summary>
    public int BaseDuration
    {
        get => GetValue(BaseDurationProperty);
        set => SetValue(BaseDurationProperty, value);
    }

    /// <summary>
    /// Fester Index statt Auto-Erkennung (-1 = automatisch).
    /// Nützlich wenn das Element nicht in einem ItemsControl ist.
    /// </summary>
    public int FixedIndex
    {
        get => GetValue(FixedIndexProperty);
        set => SetValue(FixedIndexProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject == null) return;

        AssociatedObject.Opacity = 0;
        AssociatedObject.AttachedToVisualTree += OnAttachedToVisualTree;

        // Fallback: Falls Control bereits im Visual Tree ist, wird AttachedToVisualTree
        // nicht mehr gefeuert → Animation direkt starten
        if (AssociatedObject.GetVisualRoot() != null)
        {
            _ = RunFadeInAsync();
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject == null) return;
        AssociatedObject.AttachedToVisualTree -= OnAttachedToVisualTree;

        // Sicherstellen dass Element sichtbar bleibt wenn Behavior entfernt wird
        AssociatedObject.Opacity = 1;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        await RunFadeInAsync();
    }

    /// <summary>
    /// Führt die Fade-In-Animation aus und stellt sicher, dass Opacity am Ende IMMER 1 ist.
    /// </summary>
    private async Task RunFadeInAsync()
    {
        if (AssociatedObject == null) return;

        try
        {
            var index = FixedIndex >= 0 ? FixedIndex : DetectIndex();
            var delay = index * StaggerDelay;

            if (delay > 0)
                await Task.Delay(delay);

            if (AssociatedObject == null) return;

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(BaseDuration),
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 0.0),
                            new Setter(TranslateTransform.YProperty, 15.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 1.0),
                            new Setter(TranslateTransform.YProperty, 0.0)
                        }
                    }
                }
            };

            // TranslateTransform setzen falls noch nicht vorhanden
            AssociatedObject.RenderTransform ??= new TranslateTransform();

            await animation.RunAsync(AssociatedObject);

            // Sicherheits-Fallback: Opacity IMMER auf 1 setzen nach Animation
            // (RunAsync kann ohne Exception enden aber Opacity nicht korrekt setzen)
            if (AssociatedObject != null)
            {
                AssociatedObject.Opacity = 1;
                if (AssociatedObject.RenderTransform is TranslateTransform tt)
                    tt.Y = 0;
            }
        }
        catch
        {
            // Bei Fehler (z.B. Control detached) Element sichtbar machen
            if (AssociatedObject != null)
            {
                AssociatedObject.Opacity = 1;
                if (AssociatedObject.RenderTransform is TranslateTransform tt)
                    tt.Y = 0;
            }
        }
    }

    private int DetectIndex()
    {
        if (AssociatedObject?.Parent is Panel panel)
        {
            return panel.Children.IndexOf(AssociatedObject);
        }
        return 0;
    }
}

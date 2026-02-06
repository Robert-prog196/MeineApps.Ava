using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;

namespace HandwerkerImperium.Helpers;

/// <summary>
/// Helper class for common UI animations using Avalonia animations.
/// Provides simplified animation methods as stubs/implementations for cross-platform use.
/// </summary>
public static class AnimationHelper
{
    /// <summary>
    /// Fades in an element.
    /// </summary>
    public static async Task FadeInAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(250);
        element.Opacity = 0;
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = dur,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(element);
    }

    /// <summary>
    /// Fades out an element.
    /// </summary>
    public static async Task FadeOutAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(200);

        var animation = new Animation
        {
            Duration = dur,
            Easing = new CubicEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                }
            }
        };

        await animation.RunAsync(element);
        element.IsVisible = false;
    }

    /// <summary>
    /// Scales up from 0.8 to 1 with fade (dialog entrance).
    /// </summary>
    public static async Task ShowDialogAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(300);
        element.Opacity = 0;
        element.RenderTransform = new Avalonia.Media.ScaleTransform(0.8, 0.8);
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = dur,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                    }
                }
            }
        };

        await animation.RunAsync(element);
        element.RenderTransform = null;
    }

    /// <summary>
    /// Hides dialog with fade out.
    /// </summary>
    public static async Task HideDialogAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(200);

        var animation = new Animation
        {
            Duration = dur,
            Easing = new CubicEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                }
            }
        };

        await animation.RunAsync(element);
        element.IsVisible = false;
        element.RenderTransform = null;
    }

    /// <summary>
    /// Bounce animation (opacity pulse as simple substitute).
    /// </summary>
    public static async Task BounceAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(150);

        var animation = new Animation
        {
            Duration = dur,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.5),
                    Setters = { new Setter(Visual.OpacityProperty, 0.7) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(element);
    }

    /// <summary>
    /// Small pulse animation for feedback.
    /// </summary>
    public static async Task PulseAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(100);

        var animation = new Animation
        {
            Duration = dur,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.5),
                    Setters = { new Setter(Visual.OpacityProperty, 0.8) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(element);
    }

    /// <summary>
    /// Slide in from bottom with fade.
    /// </summary>
    public static async Task SlideInFromBottomAsync(Control element, TimeSpan? duration = null)
    {
        var dur = duration ?? TimeSpan.FromMilliseconds(300);
        element.Opacity = 0;
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = dur,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(element);
    }
}

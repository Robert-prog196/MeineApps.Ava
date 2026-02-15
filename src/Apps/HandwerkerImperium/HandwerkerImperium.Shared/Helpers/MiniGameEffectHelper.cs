using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace HandwerkerImperium.Helpers;

/// <summary>
/// Wiederverwendbare visuelle Effekte fuer Mini-Games.
/// Staggered Stars, Result-Glow, Countdown-Pulse, Confetti-Trigger.
/// </summary>
public static class MiniGameEffectHelper
{
    /// <summary>
    /// Zeigt Sterne einzeln mit Bounce-Effekt an (staggered).
    /// Erwartet 3 Controls (Star1, Star2, Star3).
    /// rating: 0-3 (Anzahl gefuellter Sterne)
    /// </summary>
    public static async Task ShowStarsStaggeredAsync(Control star1, Control star2, Control star3, int rating)
    {
        var stars = new[] { star1, star2, star3 };

        // Alle Sterne initial unsichtbar
        foreach (var star in stars)
        {
            star.Opacity = 0;
        }

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(200); // 200ms Verzoegerung zwischen Sternen

            var star = stars[i];
            bool isFilled = i < rating;

            // Fade-In mit Opacity-Pulse als Bounce-Ersatz
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
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
                        Cue = new Cue(0.5),
                        // "Overshoot" simuliert fuer gefuellte Sterne
                        Setters = { new Setter(Visual.OpacityProperty, isFilled ? 1.0 : 0.5) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(Visual.OpacityProperty, isFilled ? 1.0 : 0.3) }
                    }
                }
            };

            await animation.RunAsync(star);
        }
    }

    /// <summary>
    /// Rating-spezifische Farbe fuer den Ergebnis-Text.
    /// Perfect=Gold, Good=Gruen, Ok=Orange, Miss=Rot
    /// </summary>
    public static SolidColorBrush GetRatingBrush(string rating)
    {
        return rating switch
        {
            "Perfect" => new SolidColorBrush(Color.Parse("#FFD700")),  // Gold
            "Good" => new SolidColorBrush(Color.Parse("#22C55E")),     // Gruen
            "Ok" => new SolidColorBrush(Color.Parse("#F59E0B")),       // Orange/Amber
            _ => new SolidColorBrush(Color.Parse("#EF4444"))           // Rot fuer Miss
        };
    }

    /// <summary>
    /// Pulsierende Glow-Animation auf dem Result-Border (Opacity-basiert).
    /// </summary>
    public static async Task PulseResultBorderAsync(Control border, int rating)
    {
        if (rating <= 0) return;

        int pulseCount = rating; // 1-3 Pulse je nach Rating

        for (int i = 0; i < pulseCount; i++)
        {
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
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
                        Cue = new Cue(1.0),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    }
                }
            };
            await animation.RunAsync(border);
        }
    }

    /// <summary>
    /// Countdown "3-2-1-GO!" Animation mit Opacity-Pulse pro Schritt.
    /// Aktualisiert das TextBlock.Text und pulst es.
    /// </summary>
    public static async Task AnimateCountdownAsync(TextBlock countdownText, string goText = "GO!")
    {
        countdownText.IsVisible = true;

        string[] steps = ["3", "2", "1", goText];
        double[] fontSizes = [48, 48, 48, 56];

        for (int i = 0; i < steps.Length; i++)
        {
            countdownText.Text = steps[i];
            countdownText.FontSize = fontSizes[i];

            // Pulse-Animation: erscheint und verschwindet
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(i < 3 ? 600 : 400),
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
                        Cue = new Cue(0.3),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.7),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                    }
                }
            };

            await animation.RunAsync(countdownText);

            if (i < steps.Length - 1)
                await Task.Delay(100); // Kurze Pause zwischen Schritten
        }

        countdownText.IsVisible = false;
    }

    /// <summary>
    /// Belohnungs-Text Einblendung: "+500 EUR" und "+25 XP" nacheinander.
    /// </summary>
    public static async Task AnimateRewardTextAsync(TextBlock rewardText, string text)
    {
        rewardText.Text = text;
        rewardText.IsVisible = true;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(800),
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
                    Cue = new Cue(0.2),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.8),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.7) }
                }
            }
        };

        await animation.RunAsync(rewardText);
    }

    /// <summary>
    /// Kurzer Farb-Flash auf einem Border (Opacity blinkt kurz auf).
    /// Fuer Zonen-Treffer-Feedback.
    /// </summary>
    public static async Task FlashZoneAsync(Control zone)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.3),
                    Setters = { new Setter(Visual.OpacityProperty, 0.4) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await animation.RunAsync(zone);
    }
}

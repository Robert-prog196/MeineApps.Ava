using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace MeineApps.UI.Controls;

/// <summary>
/// Onboarding-Tooltip mit Pfeil (oben/unten). FadeIn + Scale-Animation.
/// Tap-to-Dismiss: PointerPressed blendet den Tooltip aus und feuert Dismissed-Event.
/// </summary>
public class TooltipBubble : Border
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TooltipBubble, string>(nameof(Text), "");

    public static readonly StyledProperty<ArrowPosition> ArrowProperty =
        AvaloniaProperty.Register<TooltipBubble, ArrowPosition>(nameof(Arrow), ArrowPosition.Bottom);

    /// <summary>Wird gefeuert wenn der Tooltip durch Tap geschlossen wird.</summary>
    public event EventHandler? Dismissed;

    private readonly TextBlock _textBlock;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ArrowPosition Arrow
    {
        get => GetValue(ArrowProperty);
        set => SetValue(ArrowProperty, value);
    }

    public TooltipBubble()
    {
        // Visuelles Setup
        CornerRadius = new CornerRadius(12);
        Padding = new Thickness(16, 12);
        IsHitTestVisible = true;
        Opacity = 0;
        IsVisible = false;
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        RenderTransform = new ScaleTransform(0.9, 0.9);

        _textBlock = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 260,
            Foreground = Brushes.White
        };

        Child = _textBlock;

        // Tap-to-Dismiss
        PointerPressed += (_, _) => Hide();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
            _textBlock.Text = Text;
    }

    /// <summary>Zeigt den Tooltip mit FadeIn+Scale-Animation (300ms).</summary>
    public void Show()
    {
        // Theme-Farbe laden
        if (Application.Current?.TryGetResource("PrimaryBrush", ActualThemeVariant, out var brush) == true
            && brush is IBrush primaryBrush)
            Background = primaryBrush;
        else
            Background = new SolidColorBrush(Color.Parse("#6366F1"));

        _textBlock.Text = Text;
        IsVisible = true;
        Opacity = 0;
        RenderTransform = new ScaleTransform(0.9, 0.9);

        // Animation via DispatcherTimer (ein Frame Delay fuer Transition)
        DispatcherTimer.RunOnce(() =>
        {
            Opacity = 1;
            RenderTransform = new ScaleTransform(1, 1);
        }, TimeSpan.FromMilliseconds(16));
    }

    /// <summary>Blendet den Tooltip aus und feuert Dismissed.</summary>
    public void Hide()
    {
        Opacity = 0;
        RenderTransform = new ScaleTransform(0.9, 0.9);

        DispatcherTimer.RunOnce(() =>
        {
            IsVisible = false;
            Dismissed?.Invoke(this, EventArgs.Empty);
        }, TimeSpan.FromMilliseconds(300));
    }
}

public enum ArrowPosition
{
    Top,
    Bottom
}

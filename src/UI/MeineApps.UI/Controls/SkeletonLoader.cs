using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MeineApps.UI.Controls;

/// <summary>
/// Skeleton-Loader Control mit Shimmer-Animation.
/// Zeigt graue Platzhalter-Rechtecke die pulsieren während Daten geladen werden.
/// </summary>
public class SkeletonLoader : StackPanel
{
    private DispatcherTimer? _timer;
    private double _opacity = 0.3;
    private bool _increasing = true;

    public static readonly StyledProperty<int> LinesProperty =
        AvaloniaProperty.Register<SkeletonLoader, int>(nameof(Lines), 3);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<SkeletonLoader, double>(nameof(LineHeight), 56);

    /// <summary>Anzahl der Platzhalter-Zeilen (Standard: 3).</summary>
    public int Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    /// <summary>Höhe jeder Zeile in Pixel (Standard: 56).</summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public SkeletonLoader()
    {
        Spacing = 8;
        Orientation = Orientation.Vertical;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BuildLines();
        StartShimmer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopShimmer();
    }

    private void BuildLines()
    {
        Children.Clear();
        for (var i = 0; i < Lines; i++)
        {
            var line = new Border
            {
                Height = LineHeight,
                CornerRadius = new CornerRadius(8),
                Opacity = 0.3,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Abwechselnde Breiten für natürliches Aussehen
            if (i == Lines - 1)
                line.Width = double.NaN; // letzte Zeile etwas kürzer via MaxWidth
            line.MaxWidth = i == Lines - 1 ? 250 : double.PositiveInfinity;
            line.HorizontalAlignment = i == Lines - 1 ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;

            // Versuche Theme-Brush, Fallback auf Grau
            if (Application.Current?.TryGetResource("BorderSubtleBrush", Avalonia.Styling.ThemeVariant.Default, out var brush) == true
                && brush is IBrush themeBrush)
            {
                line.Background = themeBrush;
            }
            else
            {
                line.Background = new SolidColorBrush(Color.Parse("#333333"));
            }

            Children.Add(line);
        }
    }

    private void StartShimmer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += OnShimmerTick;
        _timer.Start();
    }

    private void OnShimmerTick(object? sender, EventArgs e)
    {
        // Pulsieren: 0.15 ↔ 0.45
        if (_increasing)
        {
            _opacity += 0.015;
            if (_opacity >= 0.45)
            {
                _opacity = 0.45;
                _increasing = false;
            }
        }
        else
        {
            _opacity -= 0.015;
            if (_opacity <= 0.15)
            {
                _opacity = 0.15;
                _increasing = true;
            }
        }

        foreach (var child in Children)
        {
            if (child is Border border)
                border.Opacity = _opacity;
        }
    }

    private void StopShimmer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnShimmerTick;
            _timer = null;
        }
    }
}

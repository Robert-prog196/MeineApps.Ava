using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace MeineApps.UI.Controls;

/// <summary>
/// TextBlock der Zahlenwerte sanft interpoliert statt zu springen.
/// CubicEaseOut-Interpolation bei ~60fps, konfigurierbares Format.
/// </summary>
public class AnimatedNumberText : TextBlock
{
    public static readonly StyledProperty<decimal> TargetValueProperty =
        AvaloniaProperty.Register<AnimatedNumberText, decimal>(nameof(TargetValue), 0m);

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<AnimatedNumberText, TimeSpan>(nameof(Duration),
            TimeSpan.FromMilliseconds(300));

    public static readonly StyledProperty<string> FormatStringProperty =
        AvaloniaProperty.Register<AnimatedNumberText, string>(nameof(FormatString), "{0:N0}");

    /// <summary>Zielwert zu dem interpoliert wird.</summary>
    public decimal TargetValue
    {
        get => GetValue(TargetValueProperty);
        set => SetValue(TargetValueProperty, value);
    }

    /// <summary>Dauer der Interpolation (Default: 300ms).</summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>Format-String fuer die Anzeige (Default: "{0:N0}").</summary>
    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    private decimal _displayValue;
    private decimal _startValue;
    private decimal _animTarget;
    private DateTime _animStart;
    private DispatcherTimer? _timer;
    private bool _isAnimating;

    static AnimatedNumberText()
    {
        TargetValueProperty.Changed.AddClassHandler<AnimatedNumberText>(
            (ctrl, _) => ctrl.OnTargetValueChanged());
    }

    private void OnTargetValueChanged()
    {
        var newTarget = TargetValue;

        // Kleiner Unterschied → direkt setzen
        if (Math.Abs(newTarget - _displayValue) < 0.5m)
        {
            _displayValue = newTarget;
            UpdateText();
            StopAnimation();
            return;
        }

        // Animation starten
        _startValue = _displayValue;
        _animTarget = newTarget;
        _animStart = DateTime.UtcNow;
        StartAnimation();
    }

    private void StartAnimation()
    {
        if (_timer == null)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += OnTimerTick;
        }

        if (!_isAnimating)
        {
            _isAnimating = true;
            _timer.Start();
        }
    }

    private void StopAnimation()
    {
        if (_isAnimating)
        {
            _isAnimating = false;
            _timer?.Stop();
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.UtcNow - _animStart).TotalMilliseconds;
        var durationMs = Duration.TotalMilliseconds;
        var progress = Math.Min(elapsed / durationMs, 1.0);

        // CubicEaseOut: 1 - (1 - t)^3
        var eased = 1.0 - Math.Pow(1.0 - progress, 3);

        _displayValue = _startValue + (decimal)eased * (_animTarget - _startValue);
        UpdateText();

        if (progress >= 1.0)
        {
            _displayValue = _animTarget;
            UpdateText();
            StopAnimation();
        }
    }

    private void UpdateText()
    {
        try
        {
            Text = string.Format(FormatString, _displayValue);
        }
        catch
        {
            Text = _displayValue.ToString("N0");
        }
    }
}

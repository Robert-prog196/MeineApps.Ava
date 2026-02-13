using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace MeineApps.UI.Behaviors;

/// <summary>
/// Behavior das ein Control nach links verschiebt um eine Aktion dahinter freizulegen (Swipe-to-Reveal).
/// Nur horizontale Swipes werden erkannt. Vertikale Gesten werden ignoriert (ScrollViewer-Kompatibilität).
/// </summary>
public class SwipeToRevealBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> SwipeThresholdProperty =
        AvaloniaProperty.Register<SwipeToRevealBehavior, double>(nameof(SwipeThreshold), 80);

    public static readonly StyledProperty<double> RevealWidthProperty =
        AvaloniaProperty.Register<SwipeToRevealBehavior, double>(nameof(RevealWidth), 80);

    /// <summary>Ab welcher Distanz eingerastet wird (Standard: 80).</summary>
    public double SwipeThreshold
    {
        get => GetValue(SwipeThresholdProperty);
        set => SetValue(SwipeThresholdProperty, value);
    }

    /// <summary>Breite des freigelegten Bereichs (Standard: 80).</summary>
    public double RevealWidth
    {
        get => GetValue(RevealWidthProperty);
        set => SetValue(RevealWidthProperty, value);
    }

    private Point _startPoint;
    private bool _isTracking;
    private bool _isRevealed;
    private bool _directionLocked;
    private bool _isHorizontal;
    private TranslateTransform? _transform;

    // Spring-Back Animation
    private DispatcherTimer? _animTimer;
    private double _animFrom;
    private double _animTo;
    private int _animFrame;
    private const int AnimFrames = 10;
    private const int AnimIntervalMs = 16;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;

        _transform = new TranslateTransform();
        AssociatedObject.RenderTransform = _transform;

        AssociatedObject.PointerPressed += OnPointerPressed;
        AssociatedObject.PointerMoved += OnPointerMoved;
        AssociatedObject.PointerReleased += OnPointerReleased;
        AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject == null) return;

        AssociatedObject.PointerPressed -= OnPointerPressed;
        AssociatedObject.PointerMoved -= OnPointerMoved;
        AssociatedObject.PointerReleased -= OnPointerReleased;
        AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        StopAnimation();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
            return;

        _startPoint = e.GetPosition(AssociatedObject);
        _isTracking = true;
        _directionLocked = false;
        _isHorizontal = false;
        StopAnimation();
        e.Pointer.Capture(AssociatedObject);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isTracking || _transform == null || AssociatedObject == null) return;

        var current = e.GetPosition(AssociatedObject);
        var deltaX = current.X - _startPoint.X;
        var deltaY = current.Y - _startPoint.Y;

        // Richtungserkennung beim ersten signifikanten Bewegung
        if (!_directionLocked)
        {
            var absDx = Math.Abs(deltaX);
            var absDy = Math.Abs(deltaY);

            // Mindestens 8px Bewegung bevor Richtung festgelegt wird
            if (absDx < 8 && absDy < 8) return;

            _directionLocked = true;
            _isHorizontal = absDx > absDy;

            if (!_isHorizontal)
            {
                // Vertikale Geste → abbrechen, ScrollViewer übernimmt
                _isTracking = false;
                e.Pointer.Capture(null);
                return;
            }
        }

        if (!_isHorizontal) return;

        // Berechne neue Position basierend auf aktuellem Zustand
        var baseOffset = _isRevealed ? -RevealWidth : 0;
        var newX = baseOffset + deltaX;

        // Grenzen: maximal 0 (geschlossen) und -RevealWidth (offen)
        newX = Math.Clamp(newX, -RevealWidth, 0);
        _transform.X = newX;

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isTracking || _transform == null)
        {
            _isTracking = false;
            return;
        }

        _isTracking = false;
        e.Pointer.Capture(null);

        if (!_isHorizontal) return;

        var currentX = _transform.X;

        // Entscheide ob einrasten oder zurückspringen
        if (_isRevealed)
        {
            // Wenn schon offen: bei leichtem Swipe nach rechts schließen
            if (currentX > -SwipeThreshold * 0.5)
            {
                AnimateTo(0);
                _isRevealed = false;
            }
            else
            {
                AnimateTo(-RevealWidth);
            }
        }
        else
        {
            // Wenn geschlossen: bei genügend Swipe nach links öffnen
            if (currentX < -SwipeThreshold * 0.5)
            {
                AnimateTo(-RevealWidth);
                _isRevealed = true;
            }
            else
            {
                AnimateTo(0);
            }
        }

        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isTracking) return;
        _isTracking = false;

        // Zurück zur nächsten Snap-Position
        if (_transform != null)
        {
            var target = _isRevealed ? -RevealWidth : 0;
            AnimateTo(target);
        }
    }

    private void AnimateTo(double target)
    {
        if (_transform == null) return;

        StopAnimation();
        _animFrom = _transform.X;
        _animTo = target;
        _animFrame = 0;

        // Wenn schon am Ziel, nichts tun
        if (Math.Abs(_animFrom - _animTo) < 0.5)
        {
            _transform.X = _animTo;
            return;
        }

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AnimIntervalMs) };
        _animTimer.Tick += OnAnimTick;
        _animTimer.Start();
    }

    private void OnAnimTick(object? sender, EventArgs e)
    {
        if (_transform == null)
        {
            StopAnimation();
            return;
        }

        _animFrame++;

        if (_animFrame >= AnimFrames)
        {
            _transform.X = _animTo;
            StopAnimation();
            return;
        }

        // CubicEaseOut
        var t = (double)_animFrame / AnimFrames;
        var eased = 1 - Math.Pow(1 - t, 3);
        _transform.X = _animFrom + (_animTo - _animFrom) * eased;
    }

    private void StopAnimation()
    {
        if (_animTimer != null)
        {
            _animTimer.Stop();
            _animTimer.Tick -= OnAnimTick;
            _animTimer = null;
        }
    }
}

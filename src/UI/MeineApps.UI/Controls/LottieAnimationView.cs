using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace MeineApps.UI.Controls;

/// <summary>
/// Wrapper-Control für Avalonia.Labs.Lottie mit zusätzlichen Features:
/// - OneShot-Modus (einmal abspielen, dann ausblenden)
/// - Event-basierte Steuerung (Play/Stop von ViewModel)
/// - Auto-Dispose nach Abschluss
/// Nutzung: LottieAnimationView in AXAML einbinden, Path auf JSON-Datei setzen.
/// </summary>
public class LottieAnimationView : ContentControl
{
    // Avalonia.Labs.Lottie Control wird dynamisch erstellt
    private Control? _lottieControl;
    private DispatcherTimer? _oneShotTimer;

    /// <summary>
    /// Pfad zur Lottie-JSON-Datei (AvaloniaResource).
    /// </summary>
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<LottieAnimationView, string?>(nameof(Path));

    /// <summary>
    /// Anzahl Wiederholungen (-1 = unendlich, 1 = einmal).
    /// </summary>
    public static readonly StyledProperty<int> RepeatCountProperty =
        AvaloniaProperty.Register<LottieAnimationView, int>(nameof(RepeatCount), -1);

    /// <summary>
    /// Automatisch abspielen beim Laden.
    /// </summary>
    public static readonly StyledProperty<bool> AutoPlayProperty =
        AvaloniaProperty.Register<LottieAnimationView, bool>(nameof(AutoPlay), true);

    /// <summary>
    /// OneShot-Modus: Nach einmaliger Wiedergabe automatisch ausblenden.
    /// </summary>
    public static readonly StyledProperty<bool> OneShotProperty =
        AvaloniaProperty.Register<LottieAnimationView, bool>(nameof(OneShot), false);

    /// <summary>
    /// Dauer der Animation in Millisekunden (für OneShot-Timer).
    /// Muss manuell gesetzt werden da Lottie keine Duration-Property exponiert.
    /// </summary>
    public static readonly StyledProperty<int> DurationMsProperty =
        AvaloniaProperty.Register<LottieAnimationView, int>(nameof(DurationMs), 2000);

    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public int RepeatCount
    {
        get => GetValue(RepeatCountProperty);
        set => SetValue(RepeatCountProperty, value);
    }

    public bool AutoPlay
    {
        get => GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    public bool OneShot
    {
        get => GetValue(OneShotProperty);
        set => SetValue(OneShotProperty, value);
    }

    public int DurationMs
    {
        get => GetValue(DurationMsProperty);
        set => SetValue(DurationMsProperty, value);
    }

    /// <summary>
    /// Event wenn OneShot-Animation abgeschlossen ist.
    /// </summary>
    public event EventHandler? AnimationCompleted;

    public LottieAnimationView()
    {
        IsHitTestVisible = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PathProperty)
        {
            UpdateLottieControl();
        }
    }

    private void UpdateLottieControl()
    {
        if (string.IsNullOrEmpty(Path)) return;

        try
        {
            // Avalonia.Labs.Lottie dynamisch instanziieren
            var lottieType = Type.GetType("Avalonia.Labs.Lottie.Lottie, Avalonia.Labs.Lottie");
            if (lottieType == null) return;

            var lottie = Activator.CreateInstance(lottieType) as Control;
            if (lottie == null) return;

            // Properties via Reflection setzen (vermeidet harte Abhängigkeit)
            SetPropertyValue(lottie, "Path", Path);
            SetPropertyValue(lottie, "RepeatCount", OneShot ? 1 : RepeatCount);
            SetPropertyValue(lottie, "AutoPlay", AutoPlay);
            SetPropertyValue(lottie, "Stretch", Stretch.Uniform);

            _lottieControl = lottie;
            Content = lottie;

            // OneShot-Timer starten
            if (OneShot && AutoPlay)
            {
                StartOneShotTimer();
            }
        }
        catch
        {
            // Fallback: Lottie nicht verfügbar - stille Degradation
        }
    }

    /// <summary>
    /// Startet die Animation (für programmatische Steuerung).
    /// </summary>
    public void Play()
    {
        if (_lottieControl == null) return;

        IsVisible = true;
        InvokeMethod(_lottieControl, "Start");

        if (OneShot)
        {
            StartOneShotTimer();
        }
    }

    /// <summary>
    /// Stoppt die Animation.
    /// </summary>
    public void Stop()
    {
        _oneShotTimer?.Stop();
        InvokeMethod(_lottieControl, "Stop");

        if (OneShot)
        {
            IsVisible = false;
        }
    }

    private void StartOneShotTimer()
    {
        _oneShotTimer?.Stop();
        _oneShotTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DurationMs)
        };
        _oneShotTimer.Tick += (_, _) =>
        {
            _oneShotTimer.Stop();
            IsVisible = false;
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        };
        _oneShotTimer.Start();
    }

    private static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        obj.GetType().GetProperty(propertyName)?.SetValue(obj, value);
    }

    private static void InvokeMethod(object? obj, string methodName)
    {
        obj?.GetType().GetMethod(methodName)?.Invoke(obj, null);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using ZeitManager.Models;
using ZeitManager.Services;

namespace ZeitManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;
    private readonly ITimerService _timerService;
    private readonly IAlarmSchedulerService _alarmScheduler;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTimerActive))]
    [NotifyPropertyChangedFor(nameof(IsStopwatchActive))]
    [NotifyPropertyChangedFor(nameof(IsPomodoroActive))]
    [NotifyPropertyChangedFor(nameof(IsAlarmActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private int _selectedTabIndex;

    [ObservableProperty]
    private TimerViewModel _timerViewModel;

    [ObservableProperty]
    private StopwatchViewModel _stopwatchViewModel;

    [ObservableProperty]
    private PomodoroViewModel _pomodoroViewModel;

    [ObservableProperty]
    private AlarmViewModel _alarmViewModel;

    [ObservableProperty]
    private SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private AlarmOverlayViewModel _alarmOverlayViewModel;

    [ObservableProperty]
    private bool _isAlarmOverlayVisible;

    [ObservableProperty]
    private string? _snackbarMessage;

    [ObservableProperty]
    private bool _isSnackbarVisible;

    public event Action<string, string>? FloatingTextRequested;
    public event Action? CelebrationRequested;

    /// <summary>Wird ausgelöst um einen Exit-Hinweis anzuzeigen (z.B. Toast "Nochmal drücken zum Beenden").</summary>
    public event Action<string>? ExitHintRequested;

    // Localized tab labels
    public string NavTimerText => _localization.GetString("Timer");
    public string NavStopwatchText => _localization.GetString("Stopwatch");
    public string NavPomodoroText => _localization.GetString("Pomodoro");
    public string NavAlarmText => _localization.GetString("Alarm");
    public string NavSettingsText => _localization.GetString("Settings");

    // Active tab indicators
    public bool IsTimerActive => SelectedTabIndex == 0;
    public bool IsStopwatchActive => SelectedTabIndex == 1;
    public bool IsPomodoroActive => SelectedTabIndex == 2;
    public bool IsAlarmActive => SelectedTabIndex == 3;
    public bool IsSettingsActive => SelectedTabIndex == 4;

    public MainViewModel(
        IThemeService themeService,
        ILocalizationService localization,
        ITimerService timerService,
        IAlarmSchedulerService alarmScheduler,
        AlarmOverlayViewModel alarmOverlayViewModel,
        TimerViewModel timerViewModel,
        StopwatchViewModel stopwatchViewModel,
        PomodoroViewModel pomodoroViewModel,
        AlarmViewModel alarmViewModel,
        SettingsViewModel settingsViewModel)
    {
        _themeService = themeService;
        _localization = localization;
        _timerService = timerService;
        _alarmScheduler = alarmScheduler;
        _timerViewModel = timerViewModel;
        _stopwatchViewModel = stopwatchViewModel;
        _pomodoroViewModel = pomodoroViewModel;
        _alarmViewModel = alarmViewModel;
        _settingsViewModel = settingsViewModel;
        _alarmOverlayViewModel = alarmOverlayViewModel;

        _localization.LanguageChanged += OnLanguageChanged;

        // Wire up timer finished event to show overlay
        _timerService.TimerFinished += OnTimerFinished;

        // Wire up alarm triggered event to show overlay
        _alarmScheduler.AlarmTriggered += OnAlarmTriggered;

        // Wire up alarm permission missing
        _alarmScheduler.AlarmPermissionMissing += (_, _) =>
            ShowSnackbar(_localization.GetString("AlarmPermissionMissing"));

        // Wire up overlay dismiss
        _alarmOverlayViewModel.DismissRequested += (_, _) => IsAlarmOverlayVisible = false;

        // Floating Text Events von Kind-ViewModels weiterleiten
        _stopwatchViewModel.FloatingTextRequested += (text, cat) => FloatingTextRequested?.Invoke(text, cat);
        _pomodoroViewModel.FloatingTextRequested += (text, cat) => FloatingTextRequested?.Invoke(text, cat);
        _pomodoroViewModel.CelebrationRequested += () => CelebrationRequested?.Invoke();

        // Wire up MessageRequested from child ViewModels
        _settingsViewModel.MessageRequested += OnChildMessageRequested;
        _timerViewModel.MessageRequested += (_, msg) => ShowSnackbar(msg);
    }

    private void OnTimerFinished(object? sender, TimerItem timer)
    {
        FloatingTextRequested?.Invoke(_localization.GetString("TimerDone"), "success");
        CelebrationRequested?.Invoke();
        AlarmOverlayViewModel.ShowForTimer(timer);
        IsAlarmOverlayVisible = true;
    }

    private void OnAlarmTriggered(object? sender, AlarmItem alarm)
    {
        AlarmOverlayViewModel.ShowForAlarm(alarm);
        IsAlarmOverlayVisible = true;
    }

    private void OnChildMessageRequested(object? sender, string message)
    {
        ShowSnackbar(message);
    }

    private CancellationTokenSource? _snackbarCts;

    private async void ShowSnackbar(string message)
    {
        _snackbarCts?.Cancel();
        _snackbarCts?.Dispose();
        _snackbarCts = new CancellationTokenSource();
        var token = _snackbarCts.Token;

        SnackbarMessage = message;
        IsSnackbarVisible = true;

        try
        {
            await Task.Delay(3000, token);
            IsSnackbarVisible = false;
        }
        catch (OperationCanceledException) { }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavTimerText));
        OnPropertyChanged(nameof(NavStopwatchText));
        OnPropertyChanged(nameof(NavPomodoroText));
        OnPropertyChanged(nameof(NavAlarmText));
        OnPropertyChanged(nameof(NavSettingsText));
        PomodoroViewModel.UpdateLocalizedTexts();
    }

    #region Back-Navigation (Double-Back-to-Exit)

    private DateTime _lastBackPress = DateTime.MinValue;
    private const int BackPressIntervalMs = 2000;

    /// <summary>
    /// Behandelt die Zurück-Taste. Gibt true zurück wenn konsumiert (App bleibt offen),
    /// false wenn die App geschlossen werden darf (Double-Back).
    /// Reihenfolge: Alarm-Overlay → Tab-Overlays → Tab zurück → Double-Back-to-Exit.
    /// </summary>
    public bool HandleBackPressed()
    {
        // 1. Alarm-Overlay hat höchste Priorität (blockiert alles andere)
        if (IsAlarmOverlayVisible)
        {
            // Alarm-Overlay nicht per Back schließen - Benutzer muss Dismiss/Snooze drücken
            return true;
        }

        // 2. Overlays im aktiven Tab schließen
        switch (SelectedTabIndex)
        {
            case 0: // Timer
                if (TimerViewModel.IsDeleteAllConfirmVisible)
                {
                    TimerViewModel.IsDeleteAllConfirmVisible = false;
                    return true;
                }
                if (TimerViewModel.IsDeleteConfirmVisible)
                {
                    TimerViewModel.IsDeleteConfirmVisible = false;
                    return true;
                }
                if (TimerViewModel.IsCreatingTimer)
                {
                    TimerViewModel.IsCreatingTimer = false;
                    return true;
                }
                break;

            case 2: // Pomodoro
                if (PomodoroViewModel.IsConfigVisible)
                {
                    PomodoroViewModel.IsConfigVisible = false;
                    return true;
                }
                if (PomodoroViewModel.IsStatisticsView)
                {
                    PomodoroViewModel.IsStatisticsView = false;
                    return true;
                }
                break;

            case 3: // Alarm
                if (AlarmViewModel.IsPauseDialogVisible)
                {
                    AlarmViewModel.IsPauseDialogVisible = false;
                    return true;
                }
                if (AlarmViewModel.IsDeleteConfirmVisible)
                {
                    AlarmViewModel.IsDeleteConfirmVisible = false;
                    return true;
                }
                if (AlarmViewModel.IsEditMode)
                {
                    AlarmViewModel.IsEditMode = false;
                    return true;
                }
                if (AlarmViewModel.IsShiftScheduleMode)
                {
                    AlarmViewModel.IsShiftScheduleMode = false;
                    return true;
                }
                break;
        }

        // 3. Wenn nicht auf dem ersten Tab → zurück zum ersten Tab
        if (SelectedTabIndex != 0)
        {
            SelectedTabIndex = 0;
            return true;
        }

        // 4. Auf Startseite: Double-Back-to-Exit
        var now = DateTime.UtcNow;
        if ((now - _lastBackPress).TotalMilliseconds < BackPressIntervalMs)
            return false; // App beenden lassen

        _lastBackPress = now;
        var msg = _localization.GetString("PressBackAgainToExit") ?? "Erneut drücken zum Beenden";
        ExitHintRequested?.Invoke(msg);
        return true; // Konsumiert
    }

    #endregion

    [RelayCommand]
    private void NavigateToTimer() => SelectedTabIndex = 0;

    [RelayCommand]
    private void NavigateToStopwatch() => SelectedTabIndex = 1;

    [RelayCommand]
    private void NavigateToPomodoro() => SelectedTabIndex = 2;

    [RelayCommand]
    private void NavigateToAlarm() => SelectedTabIndex = 3;

    [RelayCommand]
    private void NavigateToSettings() => SelectedTabIndex = 4;
}

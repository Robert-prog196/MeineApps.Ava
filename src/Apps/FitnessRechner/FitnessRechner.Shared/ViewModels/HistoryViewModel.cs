using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;
using FitnessRechner.Services;
using System.Globalization;
using MeineApps.Core.Premium.Ava.Services;
using FitnessRechner.Graphics;

namespace FitnessRechner.ViewModels;

public partial class HistoryViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    // Undo-Timeout zentral in PreferenceKeys.cs
    private readonly ITrackingService _trackingService;
    private readonly IPurchaseService _purchaseService;

    /// <summary>
    /// Raised when the VM wants to navigate
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Raised when the VM wants to show a message (title, message).
    /// </summary>
    public event Action<string, string>? MessageRequested;

    public HistoryViewModel(ITrackingService trackingService, IPurchaseService purchaseService)
    {
        _trackingService = trackingService;
        _purchaseService = purchaseService;
    }

    [ObservableProperty] private ObservableCollection<TrackingEntry> _bmiEntries = [];
    [ObservableProperty] private ObservableCollection<TrackingEntry> _bodyFatEntries = [];
    [ObservableProperty] private TrackingStats? _bmiStats;
    [ObservableProperty] private TrackingStats? _bodyFatStats;
    [ObservableProperty] private bool _hasBmiEntries;
    [ObservableProperty] private bool _hasBodyFatEntries;
    [ObservableProperty] private bool _isLoading;

    // Ads
    [ObservableProperty] private bool _showAds;

    // SkiaSharp Chart-Daten
    [ObservableProperty] private HealthTrendVisualization.DataPoint[] _bmiChartData = [];
    [ObservableProperty] private HealthTrendVisualization.DataPoint[] _bodyFatChartData = [];

    // Undo functionality
    [ObservableProperty] private bool _showUndoBanner;
    [ObservableProperty] private string _undoMessage = string.Empty;
    private TrackingEntry? _recentlyDeletedEntry;
    private CancellationTokenSource? _undoCancellation;

    // Tab Selection (true = BMI, false = BodyFat)
    [ObservableProperty] private bool _selectedTab = true;

    // Add Entry Panel
    [ObservableProperty] private bool _showAddPanel;
    [ObservableProperty] private double _newValue;
    [ObservableProperty] private string _newNote = "";
    [ObservableProperty] private DateTime _newDate = DateTime.Today;

    partial void OnSelectedTabChanged(bool value)
    {
        ShowAddPanel = false;

        // Pendenten Undo committen bei Tab-Wechsel
        if (_recentlyDeletedEntry != null)
        {
            _undoCancellation?.Cancel();
            _recentlyDeletedEntry = null;
            ShowUndoBanner = false;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            var bmiEntries = await _trackingService.GetEntriesAsync(TrackingType.Bmi, 30);
            // Pending-Delete-Eintrag filtern (verhindert Flicker während Undo-Phase)
            var pendingDeleteId = _recentlyDeletedEntry?.Id;
            BmiEntries = new ObservableCollection<TrackingEntry>(
                bmiEntries.Where(e => e.Id != pendingDeleteId).OrderByDescending(e => e.Date));
            HasBmiEntries = BmiEntries.Count > 0;

            var bodyFatEntries = await _trackingService.GetEntriesAsync(TrackingType.BodyFat, 30);
            BodyFatEntries = new ObservableCollection<TrackingEntry>(
                bodyFatEntries.Where(e => e.Id != pendingDeleteId).OrderByDescending(e => e.Date));
            HasBodyFatEntries = BodyFatEntries.Count > 0;

            BmiStats = await _trackingService.GetStatsAsync(TrackingType.Bmi, 30);
            BodyFatStats = await _trackingService.GetStatsAsync(TrackingType.BodyFat, 30);

            // Computed Display-Properties notifizieren
            OnPropertyChanged(nameof(BmiCurrentDisplay));
            OnPropertyChanged(nameof(BmiAverageDisplay));
            OnPropertyChanged(nameof(BmiMinDisplay));
            OnPropertyChanged(nameof(BmiMaxDisplay));
            OnPropertyChanged(nameof(BmiTrendDisplay));
            OnPropertyChanged(nameof(BodyFatCurrentDisplay));
            OnPropertyChanged(nameof(BodyFatAverageDisplay));
            OnPropertyChanged(nameof(BodyFatMinDisplay));
            OnPropertyChanged(nameof(BodyFatMaxDisplay));
            OnPropertyChanged(nameof(BodyFatTrendDisplay));

            UpdateCharts();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCharts()
    {
        // BMI Chart-Daten
        if (BmiEntries.Count > 0)
        {
            BmiChartData = BmiEntries
                .OrderBy(e => e.Date)
                .Select(e => new HealthTrendVisualization.DataPoint { Date = e.Date, Value = (float)e.Value })
                .ToArray();
        }
        else
        {
            BmiChartData = [];
        }

        // BodyFat Chart-Daten
        if (BodyFatEntries.Count > 0)
        {
            BodyFatChartData = BodyFatEntries
                .OrderBy(e => e.Date)
                .Select(e => new HealthTrendVisualization.DataPoint { Date = e.Date, Value = (float)e.Value })
                .ToArray();
        }
        else
        {
            BodyFatChartData = [];
        }
    }

    [RelayCommand]
    private void ShowAddBmi()
    {
        SelectedTab = true;
        NewValue = 0;
        NewNote = "";
        NewDate = DateTime.Today;
        ShowAddPanel = true;
    }

    [RelayCommand]
    private void ShowAddBodyFat()
    {
        SelectedTab = false;
        NewValue = 0;
        NewNote = "";
        NewDate = DateTime.Today;
        ShowAddPanel = true;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        ShowAddPanel = false;
        NewValue = 0;
        NewNote = "";
    }

    [RelayCommand]
    private async Task SaveEntryAsync()
    {
        // Bereichsvalidierung: BMI ≤ 100, BodyFat ≤ 100%
        var maxValue = SelectedTab ? 100.0 : 100.0;
        if (NewValue <= 0 || NewValue > maxValue)
        {
            MessageRequested?.Invoke(AppStrings.Error, AppStrings.InvalidValueEntered);
            return;
        }

        var type = SelectedTab ? TrackingType.Bmi : TrackingType.BodyFat;

        var entry = new TrackingEntry
        {
            Date = NewDate,
            Type = type,
            Value = NewValue,
            Note = string.IsNullOrWhiteSpace(NewNote) ? null : NewNote
        };

        await _trackingService.AddEntryAsync(entry);

        ShowAddPanel = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(TrackingEntry entry)
    {
        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();
        _undoCancellation = new CancellationTokenSource();

        _recentlyDeletedEntry = entry;

        if (SelectedTab)
        {
            BmiEntries.Remove(entry);
        }
        else
        {
            BodyFatEntries.Remove(entry);
        }

        UndoMessage = string.Format(AppStrings.EntryDeletedOn, entry.Date.ToString("d", CultureInfo.CurrentCulture));
        ShowUndoBanner = true;

        try
        {
            await Task.Delay(PreferenceKeys.UndoTimeoutMs, _undoCancellation.Token);
            await _trackingService.DeleteEntryAsync(entry.Id);
            _recentlyDeletedEntry = null;
            await LoadDataAsync();
        }
        catch (TaskCanceledException)
        {
            // Undo was triggered
        }
        finally
        {
            ShowUndoBanner = false;
        }
    }

    [RelayCommand]
    private void UndoDelete()
    {
        if (_recentlyDeletedEntry != null)
        {
            _undoCancellation?.Cancel();

            if (SelectedTab)
            {
                var entries = BmiEntries.ToList();
                entries.Add(_recentlyDeletedEntry);
                BmiEntries = new ObservableCollection<TrackingEntry>(entries.OrderByDescending(e => e.Date));
            }
            else
            {
                var entries = BodyFatEntries.ToList();
                entries.Add(_recentlyDeletedEntry);
                BodyFatEntries = new ObservableCollection<TrackingEntry>(entries.OrderByDescending(e => e.Date));
            }

            _recentlyDeletedEntry = null;
            ShowUndoBanner = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    public void OnAppearing()
    {
        ShowAds = !_purchaseService.IsPremium;
        _ = SafeLoadOnAppearingAsync();
    }

    private async Task SafeLoadOnAppearingAsync()
    {
        try
        {
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Silently handle - data will be loaded on next appearance
        }
    }

    // Helper properties for display
    public string BmiCurrentDisplay => BmiStats != null ? $"{BmiStats.CurrentValue:F1}" : "-";
    public string BmiAverageDisplay => BmiStats != null ? $"{AppStrings.Average} {BmiStats.AverageValue:F1}" : "-";
    public string BmiMinDisplay => BmiStats != null ? $"{AppStrings.Min} {BmiStats.MinValue:F1}" : "-";
    public string BmiMaxDisplay => BmiStats != null ? $"{AppStrings.Max} {BmiStats.MaxValue:F1}" : "-";
    public string BmiTrendDisplay => BmiStats != null
        ? (BmiStats.TrendValue > 0 ? $"+{BmiStats.TrendValue:F1}" : $"{BmiStats.TrendValue:F1}")
        : "-";

    public string BodyFatCurrentDisplay => BodyFatStats != null ? $"{BodyFatStats.CurrentValue:F1}%" : "-";
    public string BodyFatAverageDisplay => BodyFatStats != null ? $"{AppStrings.Average} {BodyFatStats.AverageValue:F1}%" : "-";
    public string BodyFatMinDisplay => BodyFatStats != null ? $"{AppStrings.Min} {BodyFatStats.MinValue:F1}%" : "-";
    public string BodyFatMaxDisplay => BodyFatStats != null ? $"{AppStrings.Max} {BodyFatStats.MaxValue:F1}%" : "-";
    public string BodyFatTrendDisplay => BodyFatStats != null
        ? (BodyFatStats.TrendValue > 0 ? $"+{BodyFatStats.TrendValue:F1}%" : $"{BodyFatStats.TrendValue:F1}%")
        : "-";

    // IDisposable für saubere Aufräumung
    public void Dispose()
    {
        if (_disposed) return;

        BmiChartData = [];
        BodyFatChartData = [];

        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();
        _undoCancellation = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;
using FitnessRechner.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MeineApps.Core.Premium.Ava.Services;
using SkiaSharp;

namespace FitnessRechner.ViewModels;

public partial class HistoryViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private const int UNDO_TIMEOUT_MS = 8000;
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

    // LiveCharts Series
    [ObservableProperty] private IEnumerable<ISeries> _bmiChartSeries = [];
    [ObservableProperty] private IEnumerable<ISeries> _bodyFatChartSeries = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxesBmi = [];
    [ObservableProperty] private Axis[] _yAxesBodyFat = [];

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
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            var bmiEntries = await _trackingService.GetEntriesAsync(TrackingType.Bmi, 30);
            BmiEntries = new ObservableCollection<TrackingEntry>(bmiEntries.OrderByDescending(e => e.Date));
            HasBmiEntries = BmiEntries.Count > 0;

            var bodyFatEntries = await _trackingService.GetEntriesAsync(TrackingType.BodyFat, 30);
            BodyFatEntries = new ObservableCollection<TrackingEntry>(bodyFatEntries.OrderByDescending(e => e.Date));
            HasBodyFatEntries = BodyFatEntries.Count > 0;

            BmiStats = await _trackingService.GetStatsAsync(TrackingType.Bmi, 30);
            BodyFatStats = await _trackingService.GetStatsAsync(TrackingType.BodyFat, 30);

            UpdateCharts();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCharts()
    {
        // X axis configuration (date)
        XAxes =
        [
            new Axis
            {
                Labeler = value =>
                {
                    try
                    {
                        var ticks = (long)value;
                        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                            return "";
                        return new DateTime(ticks).ToString("dd.MM");
                    }
                    catch
                    {
                        return "";
                    }
                },
                LabelsRotation = -45,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                TextSize = 10,
                UnitWidth = TimeSpan.FromDays(1).Ticks
            }
        ];

        // BMI Chart (Blue)
        if (BmiEntries.Count > 0)
        {
            var bmiData = BmiEntries
                .OrderBy(e => e.Date)
                .Select(e => new DateTimePoint(e.Date, e.Value))
                .ToList();

            BmiChartSeries =
            [
                new LineSeries<DateTimePoint>
                {
                    Values = bmiData,
                    Fill = new SolidColorPaint(new SKColor(33, 150, 243, 50)),
                    Stroke = new SolidColorPaint(new SKColor(33, 150, 243)) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(new SKColor(33, 150, 243)),
                    GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                    GeometrySize = 10,
                    LineSmoothness = 0.3,
                    Name = "BMI"
                }
            ];

            YAxesBmi =
            [
                new Axis
                {
                    MinLimit = 15,
                    MaxLimit = 40,
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    Labeler = value => $"{value:F1}"
                }
            ];
        }

        // BodyFat Chart (Orange)
        if (BodyFatEntries.Count > 0)
        {
            var bodyFatData = BodyFatEntries
                .OrderBy(e => e.Date)
                .Select(e => new DateTimePoint(e.Date, e.Value))
                .ToList();

            BodyFatChartSeries =
            [
                new LineSeries<DateTimePoint>
                {
                    Values = bodyFatData,
                    Fill = new SolidColorPaint(new SKColor(255, 152, 0, 50)),
                    Stroke = new SolidColorPaint(new SKColor(255, 152, 0)) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(new SKColor(255, 152, 0)),
                    GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                    GeometrySize = 10,
                    LineSmoothness = 0.3,
                    Name = AppStrings.BodyFat
                }
            ];

            YAxesBodyFat =
            [
                new Axis
                {
                    MinLimit = 5,
                    MaxLimit = 45,
                    LabelsPaint = new SolidColorPaint(SKColors.Gray),
                    TextSize = 12,
                    Labeler = value => $"{value:F0}%"
                }
            ];
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
        if (NewValue <= 0)
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

        UndoMessage = string.Format(AppStrings.EntryDeletedOn, entry.Date.ToString("dd.MM.yyyy"));
        ShowUndoBanner = true;

        try
        {
            await Task.Delay(UNDO_TIMEOUT_MS, _undoCancellation.Token);
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

    // IDisposable to clean up chart resources
    public void Dispose()
    {
        if (_disposed) return;

        BmiChartSeries = [];
        BodyFatChartSeries = [];
        XAxes = [];
        YAxesBmi = [];
        YAxesBodyFat = [];

        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();
        _undoCancellation = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

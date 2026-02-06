using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Premium.Ava.Services;
using WorkTimePro.Models;
using WorkTimePro.Helpers;
using WorkTimePro.Resources.Strings;
using WorkTimePro.Services;

namespace WorkTimePro.ViewModels;

/// <summary>
/// ViewModel for calendar view with heatmap
/// </summary>
public partial class CalendarViewModel : ObservableObject
{
    private readonly IDatabaseService _database;
    private readonly ICalculationService _calculation;
    private readonly IPurchaseService _purchaseService;
    private readonly ITrialService _trialService;

    public event Action<string>? NavigationRequested;
    public event Action<string>? MessageRequested;

    public CalendarViewModel(
        IDatabaseService database,
        ICalculationService calculation,
        IPurchaseService purchaseService,
        ITrialService trialService)
    {
        _database = database;
        _calculation = calculation;
        _purchaseService = purchaseService;
        _trialService = trialService;
    }

    // === Properties ===

    [ObservableProperty]
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private string _monthDisplay = "";

    [ObservableProperty]
    private ObservableCollection<CalendarDay> _calendarDays = new();

    [ObservableProperty]
    private ObservableCollection<List<CalendarDay>> _calendarWeeks = new();

    [ObservableProperty]
    private WorkMonth? _monthSummary;

    [ObservableProperty]
    private string _totalWorkDisplay = "0:00";

    [ObservableProperty]
    private string _balanceDisplay = "+0:00";

    [ObservableProperty]
    private string _balanceColor = "#4CAF50";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showAds = true;

    // Localized texts
    public string TodayButtonText => $"{Icons.CalendarToday} {AppStrings.Today}";

    // === Commands ===

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            MonthDisplay = SelectedMonth.ToString("MMMM yyyy");

            // Load month summary
            MonthSummary = await _calculation.CalculateMonthAsync(SelectedMonth.Year, SelectedMonth.Month);

            TotalWorkDisplay = MonthSummary.ActualWorkDisplay;
            BalanceDisplay = MonthSummary.BalanceDisplay;
            BalanceColor = MonthSummary.BalanceColor;

            // Generate calendar days
            await GenerateCalendarDaysAsync();

            // Premium status
            ShowAds = !_purchaseService.IsPremium && !_trialService.IsTrialActive;
        }
        catch (Exception ex)
        {
            MessageRequested?.Invoke(string.Format(AppStrings.ErrorLoading, ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        SelectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SelectDayAsync(CalendarDay? day)
    {
        if (day == null || !day.IsCurrentMonth) return;

        // In Avalonia, show day detail directly
        NavigationRequested?.Invoke($"DayDetailPage?date={day.Date:yyyy-MM-dd}");
    }

    [RelayCommand]
    private async Task QuickStatusAsync(CalendarDay? day)
    {
        // Same function as SelectDay for long-press
        await SelectDayAsync(day);
    }

    /// <summary>
    /// Set status for a specific day
    /// </summary>
    public async Task SetDayStatusAsync(DateTime date, DayStatus status, string? note = null)
    {
        var workDay = await _database.GetOrCreateWorkDayAsync(date);
        workDay.Status = status;
        if (!string.IsNullOrWhiteSpace(note))
            workDay.Note = note;
        await _database.SaveWorkDayAsync(workDay);
        await LoadDataAsync();
    }

    /// <summary>
    /// Enter vacation for multiple consecutive days
    /// </summary>
    public async Task SetVacationRangeAsync(DateTime startDate, int days)
    {
        days = Math.Min(days, 30);

        for (var i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);

            // Skip weekends
            if (IsWeekend(date))
                continue;

            var workDay = await _database.GetOrCreateWorkDayAsync(date);
            workDay.Status = DayStatus.Vacation;
            workDay.Note = $"{AppStrings.Vacation} ({startDate:dd.MM} - {startDate.AddDays(days - 1):dd.MM})";
            await _database.SaveWorkDayAsync(workDay);
        }

        MessageRequested?.Invoke(string.Format(AppStrings.VacationDaysEnteredFormat, days, startDate.ToString("dd.MM.yyyy")));
        await LoadDataAsync();
    }

    // === Helper methods ===

    private async Task GenerateCalendarDaysAsync()
    {
        var days = new List<CalendarDay>();

        var firstDayOfMonth = SelectedMonth;
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Day of week of first day (0=Sun, 1=Mon, ..., 6=Sat)
        // We want Monday as first day
        var firstDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

        // Days from previous month
        var prevMonthStart = firstDayOfMonth.AddDays(-firstDayOfWeek);
        for (var i = 0; i < firstDayOfWeek; i++)
        {
            days.Add(new CalendarDay
            {
                Date = prevMonthStart.AddDays(i),
                IsCurrentMonth = false
            });
        }

        // Load WorkDays for the month
        var workDays = await _database.GetWorkDaysAsync(firstDayOfMonth, lastDayOfMonth);

        // Days of current month
        for (var date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
        {
            var workDay = workDays.FirstOrDefault(w => w.Date.Date == date.Date);

            days.Add(new CalendarDay
            {
                Date = date,
                IsCurrentMonth = true,
                IsToday = date.Date == DateTime.Today,
                WorkMinutes = workDay?.ActualWorkMinutes ?? 0,
                Status = workDay?.Status ?? (IsWeekend(date) ? DayStatus.Weekend : DayStatus.WorkDay),
                HasData = workDay != null && workDay.ActualWorkMinutes > 0
            });
        }

        // Days from next month (fill up to 42 days = 6 weeks)
        var remainingDays = 42 - days.Count;
        var nextMonthStart = lastDayOfMonth.AddDays(1);
        for (var i = 0; i < remainingDays; i++)
        {
            days.Add(new CalendarDay
            {
                Date = nextMonthStart.AddDays(i),
                IsCurrentMonth = false
            });
        }

        CalendarDays = new ObservableCollection<CalendarDay>(days);

        // Group into weeks (7 days each)
        var weeks = new List<List<CalendarDay>>();
        for (int i = 0; i < days.Count; i += 7)
        {
            weeks.Add(days.Skip(i).Take(7).ToList());
        }
        CalendarWeeks = new ObservableCollection<List<CalendarDay>>(weeks);
    }

    private static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }
}

/// <summary>
/// Calendar day for heatmap display
/// </summary>
public class CalendarDay
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public int WorkMinutes { get; set; }
    public DayStatus Status { get; set; }
    public bool HasData { get; set; }

    public string DayNumber => Date.Day.ToString();

    public string HeatmapColor
    {
        get
        {
            if (!IsCurrentMonth) return "#F5F5F5";
            if (!HasData) return "#EEEEEE";

            return WorkMinutes switch
            {
                < 240 => "#C8E6C9",  // < 4h
                < 360 => "#81C784",  // 4-6h
                < 480 => "#4CAF50",  // 6-8h
                < 600 => "#388E3C",  // 8-10h
                _ => "#F44336"       // > 10h (overtime)
            };
        }
    }

    public string TextColor
    {
        get
        {
            if (!IsCurrentMonth) return "#BDBDBD";
            if (IsToday) return "#FFFFFF";
            if (Status == DayStatus.Weekend) return "#9E9E9E";
            return "#212121";
        }
    }

    public string BackgroundColor
    {
        get
        {
            if (IsToday) return "#1565C0";
            return HeatmapColor;
        }
    }

    public string StatusIcon => Status switch
    {
        DayStatus.Vacation => Icons.Beach,
        DayStatus.Holiday => Icons.PartyPopper,
        DayStatus.Sick => Icons.Thermometer,
        DayStatus.HomeOffice => Icons.HomeAccount,
        _ => ""
    };
}

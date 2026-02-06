using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Services;
using WorkTimePro.Models;
using WorkTimePro.Resources.Strings;
using WorkTimePro.Services;

namespace WorkTimePro.ViewModels;

/// <summary>
/// Type for vacation type selection (replaces ValueTuple for bindability)
/// </summary>
public class VacationTypeItem
{
    public DayStatus Status { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// ViewModel for vacation management (Premium feature)
/// </summary>
public partial class VacationViewModel : ObservableObject
{
    private readonly IVacationService _vacationService;
    private readonly IHolidayService _holidayService;
    private readonly IPurchaseService _purchaseService;
    private readonly ITrialService _trialService;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private int _selectedYear;

    [ObservableProperty]
    private VacationQuota? _quota;

    [ObservableProperty]
    private VacationStatistics? _statistics;

    [ObservableProperty]
    private ObservableCollection<VacationEntry> _vacationEntries = new();

    [ObservableProperty]
    private ObservableCollection<HolidayEntry> _holidays = new();

    [ObservableProperty]
    private bool _isPremium;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showAds = true;

    [ObservableProperty]
    private string _quotaDisplay = "";

    [ObservableProperty]
    private string _progressDisplay = "";

    [ObservableProperty]
    private double _progressPercent;

    // For new vacation
    [ObservableProperty]
    private DateTime _newStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _newEndDate = DateTime.Today;

    [ObservableProperty]
    private string _newNote = "";

    [ObservableProperty]
    private DayStatus _newType = DayStatus.Vacation;

    [ObservableProperty]
    private int _calculatedDays;

    // Derived properties
    public string CalculatedDaysDisplay => CalculatedDays.ToString();
    public bool HasNoVacations => VacationEntries.Count == 0;

    partial void OnCalculatedDaysChanged(int value) => OnPropertyChanged(nameof(CalculatedDaysDisplay));

    public event Action<string>? NavigationRequested;
    public event Action<string>? MessageRequested;

    public VacationViewModel(
        IVacationService vacationService,
        IHolidayService holidayService,
        IPurchaseService purchaseService,
        ITrialService trialService,
        ILocalizationService localization)
    {
        _vacationService = vacationService;
        _holidayService = holidayService;
        _purchaseService = purchaseService;
        _trialService = trialService;
        _localization = localization;

        SelectedYear = DateTime.Today.Year;
    }

    partial void OnSelectedYearChanged(int value)
    {
        _ = LoadDataAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
                MessageRequested?.Invoke(string.Format(AppStrings.ErrorLoading, t.Exception?.Message));
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    partial void OnNewStartDateChanged(DateTime value)
    {
        if (value > NewEndDate)
            NewEndDate = value;
        _ = CalculateWorkDaysAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
                MessageRequested?.Invoke(string.Format(AppStrings.ErrorLoading, t.Exception?.Message));
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    partial void OnNewEndDateChanged(DateTime value)
    {
        if (value < NewStartDate)
            NewStartDate = value;
        _ = CalculateWorkDaysAsync().ContinueWith(t =>
        {
            if (t.Exception != null)
                MessageRequested?.Invoke(string.Format(AppStrings.ErrorLoading, t.Exception?.Message));
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            IsPremium = _purchaseService.IsPremium || _trialService.IsTrialActive;
            ShowAds = !IsPremium;

            // Load statistics
            Statistics = await _vacationService.GetStatisticsAsync(SelectedYear);

            // Quota display
            QuotaDisplay = $"{Statistics.TakenDays}/{Statistics.AvailableDays} {AppStrings.Days}";
            ProgressPercent = Statistics.UsedPercent / 100.0;
            ProgressDisplay = $"{Statistics.RemainingDays} {AppStrings.Remaining}";

            if (Statistics.PlannedDays > 0)
            {
                ProgressDisplay += $" ({Statistics.PlannedDays} {AppStrings.Planned})";
            }

            // Load vacation entries
            var entries = await _vacationService.GetVacationEntriesAsync(SelectedYear);
            VacationEntries = new ObservableCollection<VacationEntry>(entries);
            OnPropertyChanged(nameof(HasNoVacations));

            // Load holidays
            var holidays = await _holidayService.GetHolidaysAsync(SelectedYear);
            Holidays = new ObservableCollection<HolidayEntry>(holidays);
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
    private async Task CalculateWorkDaysAsync()
    {
        CalculatedDays = await _vacationService.CalculateWorkDaysAsync(NewStartDate, NewEndDate);
    }

    [RelayCommand]
    private async Task AddVacationAsync()
    {
        if (!IsPremium)
        {
            MessageRequested?.Invoke(AppStrings.PremiumRequired);
            return;
        }

        if (CalculatedDays <= 0)
        {
            MessageRequested?.Invoke(AppStrings.NoWorkDaysInPeriod);
            return;
        }

        var entry = new VacationEntry
        {
            Year = NewStartDate.Year,
            StartDate = NewStartDate,
            EndDate = NewEndDate,
            Days = CalculatedDays,
            Type = NewType,
            Note = string.IsNullOrWhiteSpace(NewNote) ? null : NewNote
        };

        await _vacationService.SaveVacationEntryAsync(entry);

        // Reset
        NewStartDate = DateTime.Today;
        NewEndDate = DateTime.Today;
        NewNote = "";
        NewType = DayStatus.Vacation;

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteVacationAsync(VacationEntry entry)
    {
        await _vacationService.DeleteVacationEntryAsync(entry.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditQuotaAsync()
    {
        if (!IsPremium) return;

        // TODO: implement platform-specific quota edit dialog
    }

    [RelayCommand]
    private async Task CarryOverDaysAsync()
    {
        if (!IsPremium) return;

        var previousYear = SelectedYear - 1;
        var transferred = await _vacationService.CarryOverRemainingDaysAsync(previousYear, SelectedYear);

        if (transferred > 0)
        {
            MessageRequested?.Invoke(string.Format(AppStrings.DaysCarriedOver, transferred, previousYear));
            await LoadDataAsync();
        }
        else
        {
            MessageRequested?.Invoke(string.Format(AppStrings.NoDaysToCarryOver, previousYear));
        }
    }

    [RelayCommand]
    private void PreviousYear()
    {
        SelectedYear--;
    }

    [RelayCommand]
    private void NextYear()
    {
        SelectedYear++;
    }

    [RelayCommand]
    private void GoToCurrentYear()
    {
        SelectedYear = DateTime.Today.Year;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    public List<VacationTypeItem> VacationTypes => new()
    {
        new() { Status = DayStatus.Vacation, Name = AppStrings.Vacation },
        new() { Status = DayStatus.Sick, Name = AppStrings.Illness },
        new() { Status = DayStatus.SpecialLeave, Name = AppStrings.SpecialLeave },
        new() { Status = DayStatus.UnpaidLeave, Name = AppStrings.UnpaidLeave },
        new() { Status = DayStatus.OvertimeCompensation, Name = AppStrings.OvertimeCompensation }
    };
}

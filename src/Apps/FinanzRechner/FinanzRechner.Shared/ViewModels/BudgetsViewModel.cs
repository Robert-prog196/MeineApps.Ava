using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanzRechner.Helpers;
using FinanzRechner.Models;
using FinanzRechner.Services;
using MeineApps.Core.Ava.Localization;

namespace FinanzRechner.ViewModels;

public partial class BudgetsViewModel : ObservableObject, IDisposable
{
    private readonly IExpenseService _expenseService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;

    // Thread-safe collection for tracking notified budgets
    private static readonly ConcurrentDictionary<string, DateTime> _notifiedBudgets = new();
    private static readonly object _cleanupLock = new();

    public event Action<string, string>? MessageRequested;

    public BudgetsViewModel(IExpenseService expenseService, ILocalizationService localizationService, INotificationService notificationService)
    {
        _expenseService = expenseService;
        _localizationService = localizationService;
        _notificationService = notificationService;
    }

    #region Localized Text Properties

    public string BudgetLimitsText => _localizationService.GetString("BudgetLimits") ?? "Budget Limits";
    public string NoBudgetsText => _localizationService.GetString("EmptyBudgetsTitle") ?? "No Budgets";
    public string NoBudgetsHintText => _localizationService.GetString("EmptyBudgetsDesc") ?? "Set monthly spending limits for your categories";
    public string ExceededText => _localizationService.GetString("Exceeded") ?? "Exceeded";
    public string WarningLabelText => _localizationService.GetString("Warning") ?? "Warning";
    public string SpentText => _localizationService.GetString("Spent") ?? "Spent";
    public string RemainingText => _localizationService.GetString("Remaining") ?? "Remaining";
    public string MonthlyLimitText => _localizationService.GetString("MonthlyLimit") ?? "Monthly Limit";
    public string SetBudgetText => _localizationService.GetString("SetBudget") ?? "Set Budget";
    public string CategoryText => _localizationService.GetString("Category") ?? "Category";
    public string MonthlyLimitEuroText => _localizationService.GetString("MonthlyLimitEuro") ?? "Monthly Limit (\u20ac)";
    public string WarningThresholdText => _localizationService.GetString("WarningThreshold") ?? "Warning Threshold (%)";
    public string WarningThresholdHintText => _localizationService.GetString("WarningThresholdHint") ?? "A warning will be displayed at this percentage.";
    public string CancelText => _localizationService.GetString("Cancel") ?? "Cancel";
    public string SaveText => _localizationService.GetString("Save") ?? "Save";
    public string UndoText => _localizationService.GetString("Undo") ?? "Undo";

    public void UpdateLocalizedTexts()
    {
        OnPropertyChanged(nameof(BudgetLimitsText));
        OnPropertyChanged(nameof(NoBudgetsText));
        OnPropertyChanged(nameof(NoBudgetsHintText));
        OnPropertyChanged(nameof(ExceededText));
        OnPropertyChanged(nameof(WarningLabelText));
        OnPropertyChanged(nameof(SpentText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(MonthlyLimitText));
        OnPropertyChanged(nameof(SetBudgetText));
        OnPropertyChanged(nameof(CategoryText));
        OnPropertyChanged(nameof(MonthlyLimitEuroText));
        OnPropertyChanged(nameof(WarningThresholdText));
        OnPropertyChanged(nameof(WarningThresholdHintText));
        OnPropertyChanged(nameof(CancelText));
        OnPropertyChanged(nameof(SaveText));
        OnPropertyChanged(nameof(UndoText));
    }

    #endregion

    #region Navigation Events

    public event Action<string>? NavigationRequested;
    private void NavigateTo(string route) => NavigationRequested?.Invoke(route);

    #endregion

    [ObservableProperty] private ObservableCollection<BudgetStatus> _budgetStatuses = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasBudgets;

    // Undo Delete
    [ObservableProperty] private bool _showUndoDelete;
    [ObservableProperty] private string _undoMessage = string.Empty;
    private Budget? _deletedBudget;
    private CancellationTokenSource? _undoCancellation;

    // For new budget
    [ObservableProperty] private bool _showAddBudget;
    [ObservableProperty] private ExpenseCategory _selectedCategory;
    [ObservableProperty] private double _monthlyLimit = 500;
    [ObservableProperty] private double _warningThreshold = 80;

    public List<ExpenseCategory> AvailableCategories { get; } = new()
    {
        ExpenseCategory.Food,
        ExpenseCategory.Transport,
        ExpenseCategory.Housing,
        ExpenseCategory.Entertainment,
        ExpenseCategory.Shopping,
        ExpenseCategory.Health,
        ExpenseCategory.Education,
        ExpenseCategory.Bills,
        ExpenseCategory.Other
    };

    [RelayCommand]
    private async Task LoadBudgetsAsync()
    {
        IsLoading = true;

        // Ensure ExpenseService is initialized
        await _expenseService.InitializeAsync();

        var statuses = await _expenseService.GetAllBudgetStatusAsync();
        BudgetStatuses.Clear();
        foreach (var status in statuses.OrderByDescending(s => s.PercentageUsed))
        {
            BudgetStatuses.Add(status);
        }

        HasBudgets = BudgetStatuses.Count > 0;
        IsLoading = false;

        // Check for budget alerts (80% or 100%)
        await CheckBudgetAlertsAsync(statuses);
    }

    /// <summary>
    /// Checks budgets and sends notifications for 80% or 100% thresholds
    /// </summary>
    private async Task CheckBudgetAlertsAsync(IEnumerable<BudgetStatus> statuses)
    {
        // Check if notifications are allowed
        if (!await _notificationService.AreNotificationsAllowedAsync())
            return;

        // Clean up old notifications (once per session)
        CleanupOldNotifications();

        var currentMonthKey = $"{DateTime.Now.Year}-{DateTime.Now.Month}";

        foreach (var status in statuses)
        {
            var percentage = status.PercentageUsed;
            if (percentage < 80) continue;

            // Create unique key for this budget alert (category + month + threshold)
            var threshold = percentage >= 100 ? "100" : "80";
            var alertKey = $"{status.Category}_{currentMonthKey}_{threshold}";

            // Skip if already notified this month (thread-safe check)
            if (_notifiedBudgets.ContainsKey(alertKey))
                continue;

            // Send notification
            var categoryName = GetLocalizedCategoryName(status.Category);
            await _notificationService.SendBudgetAlertAsync(
                categoryName,
                percentage,
                status.Spent,
                status.Limit);

            // Mark as notified (thread-safe add)
            _notifiedBudgets.TryAdd(alertKey, DateTime.Now);
        }
    }

    /// <summary>
    /// Removes old notification entries (older than current month)
    /// </summary>
    private static void CleanupOldNotifications()
    {
        lock (_cleanupLock)
        {
            var currentMonthKey = $"{DateTime.Now.Year}-{DateTime.Now.Month}";
            var keysToRemove = _notifiedBudgets.Keys
                .Where(k => !k.Contains(currentMonthKey))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _notifiedBudgets.TryRemove(key, out _);
            }
        }
    }

    [RelayCommand]
    private void ShowAddBudgetDialog()
    {
        SelectedCategory = ExpenseCategory.Food;
        MonthlyLimit = 500;
        WarningThreshold = 80;
        ShowAddBudget = true;
    }

    [RelayCommand]
    private void CancelAddBudget()
    {
        ShowAddBudget = false;
    }

    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        if (MonthlyLimit <= 0)
        {
            var title = _localizationService.GetString("Error") ?? "Error";
            var message = _localizationService.GetString("ErrorInvalidBudget") ?? "Please enter a valid budget amount.";
            MessageRequested?.Invoke(title, message);
            return;
        }

        try
        {
            var budget = new Budget
            {
                Category = SelectedCategory,
                MonthlyLimit = MonthlyLimit,
                WarningThreshold = WarningThreshold,
                IsEnabled = true
            };

            await _expenseService.SetBudgetAsync(budget);
            ShowAddBudget = false;
            await LoadBudgetsAsync();
        }
        catch (Exception)
        {
            var title = _localizationService.GetString("Error") ?? "Error";
            var message = _localizationService.GetString("SaveError") ?? "Failed to save budget. Please try again.";
            MessageRequested?.Invoke(title, message);
        }
    }

    [RelayCommand]
    private async Task EditBudgetAsync(BudgetStatus status)
    {
        var budget = await _expenseService.GetBudgetAsync(status.Category);
        if (budget == null) return;

        SelectedCategory = budget.Category;
        MonthlyLimit = budget.MonthlyLimit;
        WarningThreshold = budget.WarningThreshold;
        ShowAddBudget = true;
    }

    [RelayCommand]
    private async Task DeleteBudgetAsync(BudgetStatus status)
    {
        CancellationTokenSource? cts = null;
        try
        {
            // Get the budget for undo
            _deletedBudget = await _expenseService.GetBudgetAsync(status.Category);
            if (_deletedBudget == null) return;

            // Remove from UI
            var itemToRemove = BudgetStatuses.FirstOrDefault(b => b.Category == status.Category);
            if (itemToRemove != null)
            {
                BudgetStatuses.Remove(itemToRemove);
                HasBudgets = BudgetStatuses.Count > 0;
            }

            // Show undo notification
            var categoryName = GetLocalizedCategoryName(status.Category);
            UndoMessage = $"{_localizationService.GetString("BudgetDeleted") ?? "Budget deleted"} - {categoryName}";
            ShowUndoDelete = true;

            // Start timer for permanent deletion (5 seconds)
            _undoCancellation?.Cancel();
            _undoCancellation?.Dispose();
            cts = _undoCancellation = new CancellationTokenSource();

            await Task.Delay(5000, cts.Token);

            // Permanent deletion after 5 seconds
            if (_deletedBudget != null)
            {
                await _expenseService.DeleteBudgetAsync(_deletedBudget.Category);
                _deletedBudget = null;
                ShowUndoDelete = false;
            }
        }
        catch (TaskCanceledException)
        {
            // Undo was triggered - do nothing
        }
        catch (OperationCanceledException)
        {
            // Undo was triggered - do nothing
        }
        catch (Exception)
        {
            var title = _localizationService.GetString("Error") ?? "Error";
            var message = _localizationService.GetString("DeleteError") ?? "Failed to delete budget. Please try again.";
            MessageRequested?.Invoke(title, message);
        }
    }

    [RelayCommand]
    private async Task UndoDeleteAsync()
    {
        // Stop timer
        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();

        // Restore deleted budget
        if (_deletedBudget != null)
        {
            await _expenseService.SetBudgetAsync(_deletedBudget);
            await LoadBudgetsAsync();
            _deletedBudget = null;
        }

        ShowUndoDelete = false;
    }

    [RelayCommand]
    private void DismissUndo()
    {
        ShowUndoDelete = false;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateTo("..");
    }

    private string GetLocalizedCategoryName(ExpenseCategory category)
        => CategoryLocalizationHelper.GetLocalizedName(category, _localizationService);

    #region IDisposable

    public void Dispose()
    {
        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();
        _undoCancellation = null;
    }

    #endregion
}

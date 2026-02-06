using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeineApps.Core.Ava.Localization;
using MeineApps.Core.Premium.Ava.Services;
using FinanzRechner.Models;
using FinanzRechner.Services;
using FinanzRechner.ViewModels.Calculators;

namespace FinanzRechner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly IAdService _adService;
    private readonly ILocalizationService _localizationService;
    private readonly IExpenseService _expenseService;

    [ObservableProperty]
    private bool _isAdBannerVisible;

    public event Action<string, string>? MessageRequested;

    public ExpenseTrackerViewModel ExpenseTrackerViewModel { get; }
    public StatisticsViewModel StatisticsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public BudgetsViewModel BudgetsViewModel { get; }
    public RecurringTransactionsViewModel RecurringTransactionsViewModel { get; }
    public CompoundInterestViewModel CompoundInterestViewModel { get; }
    public SavingsPlanViewModel SavingsPlanViewModel { get; }
    public LoanViewModel LoanViewModel { get; }
    public AmortizationViewModel AmortizationViewModel { get; }
    public YieldViewModel YieldViewModel { get; }

    public MainViewModel(
        IPurchaseService purchaseService,
        IAdService adService,
        ILocalizationService localizationService,
        IExpenseService expenseService,
        ExpenseTrackerViewModel expenseTrackerViewModel,
        StatisticsViewModel statisticsViewModel,
        SettingsViewModel settingsViewModel,
        BudgetsViewModel budgetsViewModel,
        RecurringTransactionsViewModel recurringTransactionsViewModel,
        CompoundInterestViewModel compoundInterestViewModel,
        SavingsPlanViewModel savingsPlanViewModel,
        LoanViewModel loanViewModel,
        AmortizationViewModel amortizationViewModel,
        YieldViewModel yieldViewModel)
    {
        _purchaseService = purchaseService;
        _adService = adService;
        _localizationService = localizationService;
        _expenseService = expenseService;

        IsAdBannerVisible = _adService.BannerVisible;
        _adService.AdsStateChanged += (_, _) => IsAdBannerVisible = _adService.BannerVisible;

        ExpenseTrackerViewModel = expenseTrackerViewModel;
        StatisticsViewModel = statisticsViewModel;
        SettingsViewModel = settingsViewModel;
        BudgetsViewModel = budgetsViewModel;
        RecurringTransactionsViewModel = recurringTransactionsViewModel;
        CompoundInterestViewModel = compoundInterestViewModel;
        SavingsPlanViewModel = savingsPlanViewModel;
        LoanViewModel = loanViewModel;
        AmortizationViewModel = amortizationViewModel;
        YieldViewModel = yieldViewModel;

        // Wire up GoBack actions
        CompoundInterestViewModel.GoBackAction = CloseCalculator;
        SavingsPlanViewModel.GoBackAction = CloseCalculator;
        LoanViewModel.GoBackAction = CloseCalculator;
        AmortizationViewModel.GoBackAction = CloseCalculator;
        YieldViewModel.GoBackAction = CloseCalculator;

        // Wire up sub-page navigation from ExpenseTracker
        ExpenseTrackerViewModel.NavigationRequested += OnExpenseTrackerNavigation;
        BudgetsViewModel.NavigationRequested += OnSubPageGoBack;
        RecurringTransactionsViewModel.NavigationRequested += OnSubPageGoBack;

        // Set default tab
        _selectedTab = 0;
        UpdateNavTexts();

        // Subscribe to language changes
        SettingsViewModel.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        UpdateNavTexts();
        UpdateHomeTexts();
        ExpenseTrackerViewModel.UpdateLocalizedTexts();
        StatisticsViewModel.UpdateLocalizedTexts();
        BudgetsViewModel.UpdateLocalizedTexts();
        RecurringTransactionsViewModel.UpdateLocalizedTexts();
    }

    [ObservableProperty]
    private bool _isPremium;

    #region Navigation

    [ObservableProperty]
    private int _selectedTab;

    public bool IsHomeActive => SelectedTab == 0;
    public bool IsTrackerActive => SelectedTab == 1;
    public bool IsStatsActive => SelectedTab == 2;
    public bool IsSettingsActive => SelectedTab == 3;

    [ObservableProperty]
    private string _navHomeText = "Home";

    [ObservableProperty]
    private string _navTrackerText = "Tracker";

    [ObservableProperty]
    private string _navStatsText = "Statistics";

    [ObservableProperty]
    private string _navSettingsText = "Settings";

    private void UpdateNavTexts()
    {
        NavHomeText = _localizationService.GetString("TabHome") ?? "Home";
        NavTrackerText = _localizationService.GetString("TabTracker") ?? "Tracker";
        NavStatsText = _localizationService.GetString("TabStatistics") ?? "Statistics";
        NavSettingsText = _localizationService.GetString("TabSettings") ?? "Settings";
    }

    [RelayCommand]
    private void NavigateToHome() => SelectedTab = 0;

    [RelayCommand]
    private void NavigateToTracker() => SelectedTab = 1;

    [RelayCommand]
    private void NavigateToStats() => SelectedTab = 2;

    [RelayCommand]
    private void NavigateToSettings() => SelectedTab = 3;

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsHomeActive));
        OnPropertyChanged(nameof(IsTrackerActive));
        OnPropertyChanged(nameof(IsStatsActive));
        OnPropertyChanged(nameof(IsSettingsActive));

        // Close any open overlays on tab switch
        if (IsCalculatorOpen)
            CloseCalculator();
        if (IsSubPageOpen)
            CurrentSubPage = null;
    }

    #endregion

    #region Calculator Navigation

    [ObservableProperty]
    private bool _isCalculatorOpen;

    [ObservableProperty]
    private int _activeCalculatorIndex = -1;

    public bool IsCompoundInterestActive => ActiveCalculatorIndex == 0;
    public bool IsSavingsPlanActive => ActiveCalculatorIndex == 1;
    public bool IsLoanActive => ActiveCalculatorIndex == 2;
    public bool IsAmortizationActive => ActiveCalculatorIndex == 3;
    public bool IsYieldActive => ActiveCalculatorIndex == 4;

    partial void OnActiveCalculatorIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsCompoundInterestActive));
        OnPropertyChanged(nameof(IsSavingsPlanActive));
        OnPropertyChanged(nameof(IsLoanActive));
        OnPropertyChanged(nameof(IsAmortizationActive));
        OnPropertyChanged(nameof(IsYieldActive));
    }

    [RelayCommand]
    private void OpenCompoundInterest()
    {
        ActiveCalculatorIndex = 0;
        IsCalculatorOpen = true;
    }

    [RelayCommand]
    private void OpenSavingsPlan()
    {
        ActiveCalculatorIndex = 1;
        IsCalculatorOpen = true;
    }

    [RelayCommand]
    private void OpenLoan()
    {
        ActiveCalculatorIndex = 2;
        IsCalculatorOpen = true;
    }

    [RelayCommand]
    private void OpenAmortization()
    {
        ActiveCalculatorIndex = 3;
        IsCalculatorOpen = true;
    }

    [RelayCommand]
    private void OpenYield()
    {
        ActiveCalculatorIndex = 4;
        IsCalculatorOpen = true;
    }

    private void CloseCalculator()
    {
        IsCalculatorOpen = false;
        ActiveCalculatorIndex = -1;
    }

    #endregion

    #region Sub-Page Navigation (Budgets, Recurring Transactions)

    [ObservableProperty]
    private string? _currentSubPage;

    public bool IsSubPageOpen => CurrentSubPage != null;
    public bool IsBudgetsPageActive => CurrentSubPage == "BudgetsPage";
    public bool IsRecurringPageActive => CurrentSubPage == "RecurringTransactionsPage";

    partial void OnCurrentSubPageChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSubPageOpen));
        OnPropertyChanged(nameof(IsBudgetsPageActive));
        OnPropertyChanged(nameof(IsRecurringPageActive));
    }

    private void OnExpenseTrackerNavigation(string route)
    {
        if (route is "BudgetsPage" or "RecurringTransactionsPage")
        {
            CurrentSubPage = route;
        }
    }

    private void OnSubPageGoBack(string route)
    {
        if (route == "..")
        {
            CurrentSubPage = null;
        }
    }

    #endregion

    #region HomeView Localized Text

    public string HomeTitleText => _localizationService.GetString("AppName") ?? "FinanceCalc";
    public string HomeSubtitleText => _localizationService.GetString("AppDescription") ?? "Financial calculator for savings, loans and investments";
    public string SectionCalculatorsText => _localizationService.GetString("SectionCalculators") ?? "CALCULATORS";
    public string CalculatorsTitleText => _localizationService.GetString("CalculatorsTitle") ?? "Financial Calculations";
    public string CalcCompoundInterestText => _localizationService.GetString("CalcCompoundInterest") ?? "Compound Interest";
    public string CalcSavingsPlanText => _localizationService.GetString("CalcSavingsPlan") ?? "Savings Plan";
    public string CalcLoanText => _localizationService.GetString("CalcLoan") ?? "Loan";
    public string CalcAmortizationText => _localizationService.GetString("CalcAmortization") ?? "Amortization";
    public string CalcYieldText => _localizationService.GetString("CalcYield") ?? "Yield";
    public string IncomeLabelText => _localizationService.GetString("IncomeTotalLabel") ?? "Income:";
    public string ExpensesLabelText => _localizationService.GetString("ExpensesTotalLabel") ?? "Expenses:";
    public string BalanceLabelText => _localizationService.GetString("BalanceTotalLabel") ?? "Balance:";
    public string RemoveAdsText => _localizationService.GetString("RemoveAds") ?? "Remove Ads";
    public string RemoveAdsDescText => _localizationService.GetString("RemoveAdsDesc") ?? "Enjoy ad-free experience with Premium";

    private void UpdateHomeTexts()
    {
        OnPropertyChanged(nameof(HomeTitleText));
        OnPropertyChanged(nameof(HomeSubtitleText));
        OnPropertyChanged(nameof(SectionCalculatorsText));
        OnPropertyChanged(nameof(CalculatorsTitleText));
        OnPropertyChanged(nameof(CalcCompoundInterestText));
        OnPropertyChanged(nameof(CalcSavingsPlanText));
        OnPropertyChanged(nameof(CalcLoanText));
        OnPropertyChanged(nameof(CalcAmortizationText));
        OnPropertyChanged(nameof(CalcYieldText));
        OnPropertyChanged(nameof(IncomeLabelText));
        OnPropertyChanged(nameof(ExpensesLabelText));
        OnPropertyChanged(nameof(BalanceLabelText));
        OnPropertyChanged(nameof(RemoveAdsText));
        OnPropertyChanged(nameof(RemoveAdsDescText));
    }

    #endregion

    #region Dashboard

    [ObservableProperty]
    private double _monthlyIncome;

    [ObservableProperty]
    private double _monthlyExpenses;

    [ObservableProperty]
    private double _monthlyBalance;

    [ObservableProperty]
    private bool _hasTransactions;

    public string MonthlyIncomeDisplay => $"+{MonthlyIncome:N2} \u20ac";
    public string MonthlyExpensesDisplay => $"-{MonthlyExpenses:N2} \u20ac";
    public string MonthlyBalanceDisplay => MonthlyBalance >= 0 ? $"+{MonthlyBalance:N2} \u20ac" : $"{MonthlyBalance:N2} \u20ac";
    public string CurrentMonthDisplay => DateTime.Today.ToString("MMMM yyyy");

    #endregion

    #region Quick Add

    [ObservableProperty]
    private bool _showQuickAdd;

    [ObservableProperty]
    private string _quickAddAmount = string.Empty;

    [ObservableProperty]
    private string _quickAddDescription = string.Empty;

    [ObservableProperty]
    private ExpenseCategory _quickAddCategory = ExpenseCategory.Other;

    public List<ExpenseCategory> QuickAddCategories =>
    [
        ExpenseCategory.Food,
        ExpenseCategory.Transport,
        ExpenseCategory.Shopping,
        ExpenseCategory.Entertainment,
        ExpenseCategory.Bills,
        ExpenseCategory.Health,
        ExpenseCategory.Other
    ];

    #endregion

    public async Task OnAppearingAsync()
    {
        IsPremium = _purchaseService.IsPremium;
        UpdateNavTexts();
        await LoadMonthlyDataAsync();
    }

    private async Task LoadMonthlyDataAsync()
    {
        try
        {
            var today = DateTime.Today;
            var summary = await _expenseService.GetMonthSummaryAsync(today.Year, today.Month);

            MonthlyIncome = summary.TotalIncome;
            MonthlyExpenses = summary.TotalExpenses;
            MonthlyBalance = summary.Balance;
            HasTransactions = summary.TotalExpenses > 0 || summary.TotalIncome > 0;

            OnPropertyChanged(nameof(MonthlyIncomeDisplay));
            OnPropertyChanged(nameof(MonthlyExpensesDisplay));
            OnPropertyChanged(nameof(MonthlyBalanceDisplay));
        }
        catch (Exception ex)
        {
            MessageRequested?.Invoke(
                _localizationService.GetString("Error") ?? "Error",
                ex.Message);
            MonthlyIncome = 0;
            MonthlyExpenses = 0;
            MonthlyBalance = 0;
            HasTransactions = false;
        }
    }

    [RelayCommand]
    private void ToggleQuickAdd()
    {
        ShowQuickAdd = !ShowQuickAdd;
        if (ShowQuickAdd)
        {
            QuickAddAmount = string.Empty;
            QuickAddDescription = string.Empty;
            QuickAddCategory = ExpenseCategory.Other;
        }
    }

    [RelayCommand]
    private void CancelQuickAdd()
    {
        ShowQuickAdd = false;
    }

    private const int MaxDescriptionLength = 200;

    [RelayCommand]
    private async Task SaveQuickExpenseAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddAmount) || string.IsNullOrWhiteSpace(QuickAddDescription))
            return;

        if (!double.TryParse(QuickAddAmount.Replace(",", "."), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
            return;

        var description = QuickAddDescription.Trim();
        if (description.Length > MaxDescriptionLength)
            description = description[..MaxDescriptionLength];

        try
        {
            var expense = new Expense
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Today,
                Description = description,
                Amount = amount,
                Category = QuickAddCategory,
                Type = TransactionType.Expense
            };

            await _expenseService.AddExpenseAsync(expense);
            ShowQuickAdd = false;
            await LoadMonthlyDataAsync();
        }
        catch (Exception ex)
        {
            MessageRequested?.Invoke(
                _localizationService.GetString("Error") ?? "Error",
                ex.Message);
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;
using FitnessRechner.Services;
using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Services;

namespace FitnessRechner.ViewModels;

public partial class FoodSearchViewModel : ObservableObject, IDisposable
{
    private bool _disposed;
    private readonly IFoodSearchService _foodSearchService;
    private readonly IPurchaseService _purchaseService;
    private readonly IPreferencesService _preferences;
    private CancellationTokenSource? _searchCancellationTokenSource;

    private const string CALORIE_GOAL_KEY = "daily_calorie_goal";
    private const string MACRO_PROTEIN_KEY = "macro_goal_protein";
    private const string MACRO_CARBS_KEY = "macro_goal_carbs";
    private const string MACRO_FAT_KEY = "macro_goal_fat";

    /// <summary>
    /// Raised when the VM wants to navigate
    /// </summary>
    public event Action<string>? NavigationRequested;

    /// <summary>
    /// Raised when the VM wants to show a message (title, message).
    /// </summary>
    public event Action<string, string>? MessageRequested;

    public FoodSearchViewModel(
        IFoodSearchService foodSearchService,
        IPurchaseService purchaseService,
        IPreferencesService preferences)
    {
        _foodSearchService = foodSearchService;
        _purchaseService = purchaseService;
        _preferences = preferences;
        LoadCalorieGoal();
        LoadMacroGoals();
        UpdateShowAds();
    }

    /// <summary>
    /// Apply parameters passed from navigation (e.g. scanned food from barcode scanner)
    /// </summary>
    public void ApplyParameters(Dictionary<string, object> parameters)
    {
        if (parameters == null) return;

        if (parameters.TryGetValue("SelectedFood", out var foodObj) && foodObj is FoodItem scannedFood)
        {
            SelectedFood = scannedFood;
        }
    }

    // Search
    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private ObservableCollection<FoodSearchResult> _searchResults = [];
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _hasMoreResults;

    // Pagination
    private List<FoodSearchResult> _allSearchResults = [];
    private const int RESULTS_PAGE_SIZE = 15;

    // Ads
    [ObservableProperty] private bool _showAds;

    // Undo functionality
    [ObservableProperty] private bool _showUndoBanner;
    [ObservableProperty] private string _undoMessage = string.Empty;
    private FoodLogEntry? _recentlyDeletedLogEntry;
    private CancellationTokenSource? _undoCancellation;

    // Selected Food
    [ObservableProperty] private FoodItem? _selectedFood;
    [ObservableProperty] private double _portionGrams = 100;
    [ObservableProperty] private int _selectedMeal;
    [ObservableProperty] private bool _showAddPanel;

    private const double MIN_PORTION_GRAMS = 1;
    private const double MAX_PORTION_GRAMS = 10000;
    private const double HIGH_CALORIE_WARNING_THRESHOLD = 2000;
    private const int SEARCH_DEBOUNCE_MS = 300;
    private const int UNDO_TIMEOUT_MS = 5000;

    // Calculated values for selected portion
    [ObservableProperty] private double _calculatedCalories;
    [ObservableProperty] private double _calculatedProtein;
    [ObservableProperty] private double _calculatedCarbs;
    [ObservableProperty] private double _calculatedFat;

    // Today's Log
    [ObservableProperty] private ObservableCollection<FoodLogEntry> _todayLog = [];
    [ObservableProperty] private DailyNutritionSummary? _todaySummary;
    [ObservableProperty] private bool _hasLogEntries;

    // Calorie Goal & Remaining Calories
    [ObservableProperty] private double _dailyCalorieGoal;
    [ObservableProperty] private double _remainingCalories;
    [ObservableProperty] private bool _hasCalorieDeficit;
    [ObservableProperty] private string _calorieStatusText = "";
    [ObservableProperty] private bool _hasCalorieGoal;

    // Macro Goals
    [ObservableProperty] private MacroGoals? _macroGoals;
    [ObservableProperty] private DailyMacroSummary? _dailyMacroSummary;
    [ObservableProperty] private bool _hasMacroGoals;

    // Favorites
    [ObservableProperty] private ObservableCollection<FavoriteFoodEntry> _favorites = [];
    [ObservableProperty] private bool _hasFavorites;
    [ObservableProperty] private bool _isSelectedFoodFavorite;

    public List<string> Meals { get; } =
    [
        AppStrings.Breakfast,
        AppStrings.Lunch,
        AppStrings.Dinner,
        AppStrings.Snack
    ];

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            HasResults = false;
            HasMoreResults = false;
            _allSearchResults.Clear();
            return;
        }

        // Cancel previous search
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new CancellationTokenSource();
        var token = _searchCancellationTokenSource.Token;

        // Debounce: Wait before searching
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SEARCH_DEBOUNCE_MS, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            PerformSearch(SearchQuery);
                        }
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when search is cancelled
            }
        }, token);
    }

    partial void OnPortionGramsChanged(double value)
    {
        if (value < MIN_PORTION_GRAMS)
        {
            PortionGrams = MIN_PORTION_GRAMS;
            return;
        }

        if (value > MAX_PORTION_GRAMS)
        {
            PortionGrams = MAX_PORTION_GRAMS;
            return;
        }

        UpdateCalculatedValues();
    }

    partial void OnSelectedFoodChanged(FoodItem? value)
    {
        if (value != null)
        {
            PortionGrams = value.DefaultPortionGrams;
            UpdateCalculatedValues();
            ShowAddPanel = true;
            _ = SafeUpdateSelectedFoodFavoriteStatusAsync();
        }
        else
        {
            ShowAddPanel = false;
            IsSelectedFoodFavorite = false;
        }
    }

    private async Task SafeUpdateSelectedFoodFavoriteStatusAsync()
    {
        try
        {
            await UpdateSelectedFoodFavoriteStatus();
        }
        catch (Exception)
        {
            // Silently handle - favorite status will default to false
            IsSelectedFoodFavorite = false;
        }
    }

    private void PerformSearch(string query)
    {
        IsSearching = true;

        _allSearchResults = _foodSearchService.Search(query, 200).ToList();

        SearchResults.Clear();
        var firstPage = _allSearchResults.Take(RESULTS_PAGE_SIZE);
        foreach (var result in firstPage)
        {
            SearchResults.Add(result);
        }

        HasResults = SearchResults.Count > 0;
        HasMoreResults = _allSearchResults.Count > RESULTS_PAGE_SIZE;
        IsSearching = false;
    }

    [RelayCommand]
    private void LoadMoreResults()
    {
        if (!HasMoreResults || _allSearchResults.Count == 0) return;

        var currentCount = SearchResults.Count;
        var nextPage = _allSearchResults.Skip(currentCount).Take(RESULTS_PAGE_SIZE);

        foreach (var result in nextPage)
        {
            SearchResults.Add(result);
        }

        HasMoreResults = SearchResults.Count < _allSearchResults.Count;
    }

    private void UpdateCalculatedValues()
    {
        if (SelectedFood == null)
        {
            CalculatedCalories = 0;
            CalculatedProtein = 0;
            CalculatedCarbs = 0;
            CalculatedFat = 0;
            return;
        }

        var factor = PortionGrams / 100.0;
        CalculatedCalories = Math.Round(SelectedFood.CaloriesPer100g * factor, 1);
        CalculatedProtein = Math.Round(SelectedFood.ProteinPer100g * factor, 1);
        CalculatedCarbs = Math.Round(SelectedFood.CarbsPer100g * factor, 1);
        CalculatedFat = Math.Round(SelectedFood.FatPer100g * factor, 1);
    }

    [RelayCommand]
    private void SelectFood(FoodSearchResult result)
    {
        SelectedFood = result.Food;
    }

    [RelayCommand]
    private async Task AddToLog()
    {
        if (SelectedFood == null) return;

        // High calorie warning
        if (CalculatedCalories > HIGH_CALORIE_WARNING_THRESHOLD)
        {
            MessageRequested?.Invoke(AppStrings.Warning, string.Format(AppStrings.HighCalorieWarning, $"{CalculatedCalories:F0}"));
        }

        var entry = new FoodLogEntry
        {
            Date = DateTime.Today,
            FoodName = SelectedFood.Name,
            Grams = PortionGrams,
            Calories = CalculatedCalories,
            Protein = CalculatedProtein,
            Carbs = CalculatedCarbs,
            Fat = CalculatedFat,
            Meal = (MealType)SelectedMeal
        };

        try
        {
            await _foodSearchService.SaveFoodLogAsync(entry);

            // Increment favorite usage if applicable
            if (IsSelectedFoodFavorite)
            {
                await _foodSearchService.IncrementFavoriteUsageAsync(SelectedFood.Name);
            }

            // Reset
            SelectedFood = null;
            SearchQuery = "";
            ShowAddPanel = false;

            // Refresh today's log
            await LoadTodaySummaryAsync();
        }
        catch (Exception)
        {
            MessageRequested?.Invoke(AppStrings.Error, AppStrings.ErrorSavingData);
        }
    }

    [RelayCommand]
    private void CancelAdd()
    {
        SelectedFood = null;
        ShowAddPanel = false;
    }

    [RelayCommand]
    private async Task DeleteLogEntry(FoodLogEntry entry)
    {
        _undoCancellation?.Cancel();
        _undoCancellation = new CancellationTokenSource();

        _recentlyDeletedLogEntry = entry;

        TodayLog.Remove(entry);

        await LoadTodaySummaryAsync();

        UndoMessage = string.Format(AppStrings.ItemDeleted, entry.FoodName);
        ShowUndoBanner = true;

        try
        {
            await Task.Delay(UNDO_TIMEOUT_MS, _undoCancellation.Token);
            await _foodSearchService.DeleteFoodLogAsync(entry.Id);
            _recentlyDeletedLogEntry = null;
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
    private async Task UndoDeleteLog()
    {
        if (_recentlyDeletedLogEntry != null)
        {
            _undoCancellation?.Cancel();

            var logs = TodayLog.ToList();
            logs.Add(_recentlyDeletedLogEntry);
            TodayLog = new ObservableCollection<FoodLogEntry>(logs.OrderByDescending(e => e.Date));

            await LoadTodaySummaryAsync();

            _recentlyDeletedLogEntry = null;
            ShowUndoBanner = false;
        }
    }

    [RelayCommand]
    private async Task RefreshLog()
    {
        await LoadTodaySummaryAsync();
    }

    private async Task LoadTodaySummaryAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            var entries = await _foodSearchService.GetFoodLogAsync(DateTime.Today);
            TodayLog = new ObservableCollection<FoodLogEntry>(entries);
            HasLogEntries = TodayLog.Count > 0;

            TodaySummary = await _foodSearchService.GetDailySummaryAsync(DateTime.Today);

            UpdateRemainingCalories();
            UpdateDailyMacroSummary();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadCalorieGoal()
    {
        DailyCalorieGoal = _preferences.Get(CALORIE_GOAL_KEY, 0.0);
        HasCalorieGoal = DailyCalorieGoal > 0;
        UpdateRemainingCalories();
    }

    private void UpdateRemainingCalories()
    {
        if (TodaySummary == null || !HasCalorieGoal)
        {
            RemainingCalories = 0;
            CalorieStatusText = "";
            return;
        }

        var consumed = TodaySummary.TotalCalories;
        var remaining = DailyCalorieGoal - consumed;
        RemainingCalories = Math.Abs(remaining);
        HasCalorieDeficit = remaining >= 0;

        if (HasCalorieDeficit)
        {
            CalorieStatusText = string.Format(AppStrings.CaloriesRemaining, $"{RemainingCalories:F0}");
        }
        else
        {
            CalorieStatusText = string.Format(AppStrings.CaloriesOver, $"{RemainingCalories:F0}");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    [RelayCommand]
    private void UseDefaultPortion()
    {
        if (SelectedFood != null)
        {
            PortionGrams = SelectedFood.DefaultPortionGrams;
        }
    }

    /// <summary>
    /// Saves the calorie goal from a user-entered value
    /// </summary>
    public void SaveCalorieGoal(double goal)
    {
        if (goal > 0)
        {
            DailyCalorieGoal = goal;
            HasCalorieGoal = true;
            _preferences.Set(CALORIE_GOAL_KEY, goal);
            UpdateRemainingCalories();
        }
    }

    [RelayCommand]
    private void OpenBarcodeScanner()
    {
        NavigationRequested?.Invoke("BarcodeScannerPage");
    }

    private void LoadMacroGoals()
    {
        var protein = _preferences.Get(MACRO_PROTEIN_KEY, 0.0);
        var carbs = _preferences.Get(MACRO_CARBS_KEY, 0.0);
        var fat = _preferences.Get(MACRO_FAT_KEY, 0.0);

        if (protein > 0 || carbs > 0 || fat > 0)
        {
            MacroGoals = new MacroGoals
            {
                ProteinGrams = protein,
                CarbsGrams = carbs,
                FatGrams = fat
            };
            HasMacroGoals = true;
        }
        else
        {
            HasMacroGoals = false;
        }

        UpdateDailyMacroSummary();
    }

    private void UpdateDailyMacroSummary()
    {
        if (TodaySummary == null || MacroGoals == null)
        {
            DailyMacroSummary = null;
            return;
        }

        DailyMacroSummary = new DailyMacroSummary(
            TodaySummary.TotalProtein,
            TodaySummary.TotalCarbs,
            TodaySummary.TotalFat,
            TodaySummary.TotalCalories,
            MacroGoals
        );
    }

    /// <summary>
    /// Saves macro goals from user-entered values
    /// </summary>
    public void SaveMacroGoals(double protein, double carbs, double fat)
    {
        MacroGoals = new MacroGoals
        {
            ProteinGrams = protein,
            CarbsGrams = carbs,
            FatGrams = fat
        };

        _preferences.Set(MACRO_PROTEIN_KEY, protein);
        _preferences.Set(MACRO_CARBS_KEY, carbs);
        _preferences.Set(MACRO_FAT_KEY, fat);

        HasMacroGoals = true;

        // Auto-update calorie goal based on macros
        DailyCalorieGoal = MacroGoals.TotalCalories;
        HasCalorieGoal = true;
        _preferences.Set(CALORIE_GOAL_KEY, DailyCalorieGoal);

        UpdateDailyMacroSummary();
        UpdateRemainingCalories();
    }

    /// <summary>
    /// Calculates macro grams from calorie goal and preset distribution.
    /// Protein: 4 kcal/g, Carbs: 4 kcal/g, Fat: 9 kcal/g
    /// </summary>
    public static (double protein, double carbs, double fat) CalculateMacrosFromPreset(double calories, string preset)
    {
        double proteinPercent, carbsPercent, fatPercent;

        if (preset.Contains("High-Protein"))
        {
            proteinPercent = 0.40;
            carbsPercent = 0.30;
            fatPercent = 0.30;
        }
        else if (preset.Contains("Low-Carb"))
        {
            proteinPercent = 0.30;
            carbsPercent = 0.20;
            fatPercent = 0.50;
        }
        else if (preset.Contains("Muscle"))
        {
            proteinPercent = 0.35;
            carbsPercent = 0.45;
            fatPercent = 0.20;
        }
        else // Balanced (default)
        {
            proteinPercent = 0.30;
            carbsPercent = 0.40;
            fatPercent = 0.30;
        }

        double proteinGrams = Math.Round(calories * proteinPercent / 4);
        double carbsGrams = Math.Round(calories * carbsPercent / 4);
        double fatGrams = Math.Round(calories * fatPercent / 9);

        return (proteinGrams, carbsGrams, fatGrams);
    }

    private void UpdateShowAds()
    {
        ShowAds = !_purchaseService.IsPremium;
    }

    public void OnAppearing()
    {
        UpdateShowAds();
        _ = SafeLoadOnAppearingAsync();
    }

    private async Task SafeLoadOnAppearingAsync()
    {
        try
        {
            await LoadFavoritesAsync();
            await LoadTodaySummaryAsync();
        }
        catch (Exception)
        {
            // Silently handle - data will be loaded on next appearance
        }
    }

    private async Task LoadFavoritesAsync()
    {
        var favorites = await _foodSearchService.GetFavoritesAsync();
        Favorites = new ObservableCollection<FavoriteFoodEntry>(favorites);
        HasFavorites = Favorites.Count > 0;
    }

    [RelayCommand]
    private async Task ToggleFavorite()
    {
        if (SelectedFood == null) return;

        if (IsSelectedFoodFavorite)
        {
            var favorite = Favorites.FirstOrDefault(f => f.Food.Name == SelectedFood.Name);
            if (favorite != null)
            {
                await _foodSearchService.RemoveFavoriteAsync(favorite.Id);
            }
        }
        else
        {
            await _foodSearchService.SaveFavoriteAsync(SelectedFood);
        }

        await LoadFavoritesAsync();
        await UpdateSelectedFoodFavoriteStatus();
    }

    [RelayCommand]
    private async Task RemoveFavorite(FavoriteFoodEntry favorite)
    {
        await _foodSearchService.RemoveFavoriteAsync(favorite.Id);
        await LoadFavoritesAsync();
    }

    [RelayCommand]
    private void SelectFavorite(FavoriteFoodEntry favorite)
    {
        SelectedFood = favorite.Food;
    }

    private async Task UpdateSelectedFoodFavoriteStatus()
    {
        if (SelectedFood == null)
        {
            IsSelectedFoodFavorite = false;
            return;
        }
        IsSelectedFoodFavorite = await _foodSearchService.IsFavoriteAsync(SelectedFood.Name);
    }

    // IDisposable instead of Finalizer to avoid GC-Thread issues
    public void Dispose()
    {
        if (_disposed) return;

        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();
        _searchCancellationTokenSource = null;

        _undoCancellation?.Cancel();
        _undoCancellation?.Dispose();
        _undoCancellation = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

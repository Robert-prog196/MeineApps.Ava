using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.Models;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.Services.Interfaces;
using MeineApps.Core.Ava.Localization;

namespace HandwerkerImperium.ViewModels;

/// <summary>
/// ViewModel für das Crafting-System (Produktionsketten mit Rezepten).
/// Zeigt verfügbare Rezepte, aktive Aufträge und Inventar.
/// </summary>
public partial class CraftingViewModel : ObservableObject
{
    private readonly IGameStateService _gameStateService;
    private readonly ICraftingService _craftingService;
    private readonly ILocalizationService _localizationService;

    // ═══════════════════════════════════════════════════════════════════════
    // EVENTS
    // ═══════════════════════════════════════════════════════════════════════

    public event Action<string>? NavigationRequested;

    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private ObservableCollection<CraftingRecipeDisplay> _recipes = [];

    [ObservableProperty]
    private ObservableCollection<CraftingJobDisplay> _activeJobs = [];

    [ObservableProperty]
    private ObservableCollection<InventoryItemDisplay> _inventoryItems = [];

    /// <summary>
    /// Aktuelle Geld-Balance-Anzeige im Header.
    /// </summary>
    [ObservableProperty]
    private string _currentBalance = "";

    /// <summary>
    /// Verfügbare Workshop-Typen für die ComboBox.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkshopOption> _availableWorkshops = [];

    [ObservableProperty]
    private WorkshopOption? _selectedWorkshop;

    /// <summary>
    /// Ob aktive Crafting-Aufträge vorhanden sind.
    /// </summary>
    public bool HasActiveJobs => ActiveJobs.Count > 0;

    partial void OnActiveJobsChanged(ObservableCollection<CraftingJobDisplay> value) =>
        OnPropertyChanged(nameof(HasActiveJobs));

    /// <summary>
    /// Ob Inventar-Produkte vorhanden sind.
    /// </summary>
    public bool HasInventoryItems => InventoryItems.Count > 0;

    partial void OnInventoryItemsChanged(ObservableCollection<InventoryItemDisplay> value) =>
        OnPropertyChanged(nameof(HasInventoryItems));

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public CraftingViewModel(
        IGameStateService gameStateService,
        ICraftingService craftingService,
        ILocalizationService localizationService)
    {
        _gameStateService = gameStateService;
        _craftingService = craftingService;
        _localizationService = localizationService;

        BuildWorkshopOptions();
        UpdateLocalizedTexts();
        RefreshCrafting();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void StartCrafting(CraftingRecipeDisplay? recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.Id)) return;

        _craftingService.StartCrafting(recipe.Id);
        RefreshCrafting();
    }

    [RelayCommand]
    private void CollectProduct(string? jobId)
    {
        if (string.IsNullOrEmpty(jobId)) return;

        _craftingService.CollectProduct(jobId);
        RefreshCrafting();
    }

    [RelayCommand]
    private void SellItem(InventoryItemDisplay? item)
    {
        if (item == null || string.IsNullOrEmpty(item.ProductId)) return;

        _craftingService.SellProduct(item.ProductId);
        RefreshCrafting();
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationRequested?.Invoke("..");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Aktualisiert alle Crafting-Daten aus dem State.
    /// </summary>
    public void RefreshCrafting()
    {
        var state = _gameStateService.State;
        var allProducts = CraftingProduct.GetAllProducts();

        // Geld-Balance aktualisieren
        CurrentBalance = MoneyFormatter.Format(state.Money, 0);

        // Workshop-Typ aus Auswahl ermitteln
        var workshopType = SelectedWorkshop?.Type ?? WorkshopType.Carpenter;

        // Workshop-Level für ausgewählten Typ ermitteln
        var workshop = state.Workshops.FirstOrDefault(w => w.Type == workshopType);
        int workshopLevel = workshop?.Level ?? 0;

        // Verfügbare Rezepte
        var availableRecipes = _craftingService.GetAvailableRecipes(workshopType, workshopLevel);
        var recipeItems = new ObservableCollection<CraftingRecipeDisplay>();

        foreach (var recipe in availableRecipes)
        {
            // Input-Anzeige aufbauen
            string inputsDisplay = BuildInputsDisplay(recipe, allProducts, state.CraftingInventory);

            // Prüfen ob genug Materialien vorhanden
            bool canCraft = CanCraftRecipe(recipe, state.CraftingInventory);

            // Output-Name + Icon
            string outputName = allProducts.TryGetValue(recipe.OutputProductId, out var product)
                ? _localizationService.GetString(product.NameKey) ?? product.NameKey
                : recipe.OutputProductId;
            string outputIcon = GetProductIcon(recipe.OutputProductId);

            // Dauer formatieren
            string durationDisplay = FormatDuration(recipe.DurationSeconds);

            recipeItems.Add(new CraftingRecipeDisplay
            {
                Id = recipe.Id,
                Name = _localizationService.GetString(recipe.NameKey) ?? recipe.NameKey,
                InputDisplay = inputsDisplay,
                OutputName = outputName,
                OutputIcon = outputIcon,
                DurationDisplay = durationDisplay,
                CanCraft = canCraft
            });
        }

        Recipes = recipeItems;

        // Aktive Crafting-Aufträge
        var jobItems = new ObservableCollection<CraftingJobDisplay>();
        foreach (var job in state.ActiveCraftingJobs)
        {
            // Rezept-Info für Produkt-Name
            var recipeForJob = CraftingRecipe.GetAllRecipes().FirstOrDefault(r => r.Id == job.RecipeId);
            string productName = "";
            string outputIcon = "";
            if (recipeForJob != null && allProducts.TryGetValue(recipeForJob.OutputProductId, out var outputProduct))
            {
                productName = _localizationService.GetString(outputProduct.NameKey) ?? outputProduct.NameKey;
                outputIcon = GetProductIcon(recipeForJob.OutputProductId);
            }

            var remaining = job.TimeRemaining;
            string timeDisplay = remaining > TimeSpan.Zero
                ? FormatDuration((int)remaining.TotalSeconds)
                : _localizationService.GetString("Ready") ?? "Fertig";

            jobItems.Add(new CraftingJobDisplay
            {
                JobId = job.RecipeId,
                OutputName = productName,
                OutputIcon = outputIcon,
                Progress = job.Progress,
                ProgressPercentDisplay = $"{job.Progress * 100:F0}%",
                ProgressBarWidth = job.Progress * 200.0,
                TimeRemainingDisplay = timeDisplay,
                IsComplete = job.IsComplete
            });
        }

        ActiveJobs = jobItems;

        // Inventar
        var inventoryItemList = new ObservableCollection<InventoryItemDisplay>();
        foreach (var (productId, count) in state.CraftingInventory)
        {
            if (count <= 0) continue;

            string name = allProducts.TryGetValue(productId, out var invProduct)
                ? _localizationService.GetString(invProduct.NameKey) ?? invProduct.NameKey
                : productId;

            decimal value = invProduct?.BaseValue ?? 0m;
            string icon = GetProductIcon(productId);

            inventoryItemList.Add(new InventoryItemDisplay
            {
                ProductId = productId,
                Name = name,
                Icon = icon,
                QuantityDisplay = $"x{count}",
                ValueDisplay = MoneyFormatter.Format(value, 0)
            });
        }

        InventoryItems = inventoryItemList;
    }

    /// <summary>
    /// Lokalisierte Texte aktualisieren.
    /// </summary>
    public void UpdateLocalizedTexts()
    {
        Title = _localizationService.GetString("Crafting") ?? "Handwerk";
        BuildWorkshopOptions();
        RefreshCrafting();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildWorkshopOptions()
    {
        var options = new ObservableCollection<WorkshopOption>();
        var types = new[] { WorkshopType.Carpenter, WorkshopType.Plumber, WorkshopType.Electrician,
                           WorkshopType.Painter, WorkshopType.Roofer };

        foreach (var type in types)
        {
            options.Add(new WorkshopOption
            {
                Type = type,
                Name = _localizationService.GetString(type.GetLocalizationKey()) ?? type.ToString(),
                IconKind = GetWorkshopIconKind(type)
            });
        }

        AvailableWorkshops = options;
        SelectedWorkshop ??= options.FirstOrDefault();
    }

    partial void OnSelectedWorkshopChanged(WorkshopOption? value)
    {
        if (value != null) RefreshCrafting();
    }

    private string BuildInputsDisplay(CraftingRecipe recipe, Dictionary<string, CraftingProduct> allProducts,
        Dictionary<string, int> craftingInventory)
    {
        if (recipe.InputProducts.Count == 0)
            return _localizationService.GetString("NoInputRequired") ?? "Keine Materialien";

        var parts = new List<string>();
        foreach (var (productId, needed) in recipe.InputProducts)
        {
            string name = allProducts.TryGetValue(productId, out var p)
                ? _localizationService.GetString(p.NameKey) ?? p.NameKey
                : productId;
            int have = craftingInventory.GetValueOrDefault(productId, 0);
            parts.Add($"{name} ({have}/{needed})");
        }

        return string.Join(", ", parts);
    }

    private static bool CanCraftRecipe(CraftingRecipe recipe, Dictionary<string, int> craftingInventory)
    {
        foreach (var (productId, needed) in recipe.InputProducts)
        {
            int have = craftingInventory.GetValueOrDefault(productId, 0);
            if (have < needed) return false;
        }

        return true;
    }

    private static string FormatDuration(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{totalSeconds}s";
    }

    private static string GetProductIcon(string productId) => productId switch
    {
        "plank" => "🪵",
        "beam" => "🏗",
        "pipe_section" => "🔧",
        "wire_bundle" => "⚡",
        "paint_can" => "🎨",
        "tile_set" => "🧱",
        "furniture" => "🪑",
        "cabinet" => "🗄",
        "pipe_system" => "🚿",
        "wiring_harness" => "🔌",
        "painted_surface" => "🖼",
        "tiled_floor" => "🏠",
        "luxury_kitchen" => "👑",
        _ => "📦"
    };

    private static string GetWorkshopIconKind(WorkshopType type) => type switch
    {
        WorkshopType.Carpenter => "Hammer",
        WorkshopType.Plumber => "Pipe",
        WorkshopType.Electrician => "LightningBolt",
        WorkshopType.Painter => "FormatPaint",
        WorkshopType.Roofer => "HomeRoof",
        _ => "Cog"
    };
}

// ═══════════════════════════════════════════════════════════════════════════════
// DISPLAY MODELS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Workshop-Option für die ComboBox-Auswahl.
/// </summary>
public class WorkshopOption
{
    public WorkshopType Type { get; set; }
    public string Name { get; set; } = "";
    public string IconKind { get; set; } = "Cog";
}

/// <summary>
/// Anzeige-Modell für ein Crafting-Rezept im UI.
/// </summary>
public class CraftingRecipeDisplay
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string InputDisplay { get; set; } = "";
    public string OutputName { get; set; } = "";
    public string OutputIcon { get; set; } = "";
    public string DurationDisplay { get; set; } = "";
    public bool CanCraft { get; set; }
}

/// <summary>
/// Anzeige-Modell für einen aktiven Crafting-Auftrag im UI.
/// </summary>
public class CraftingJobDisplay
{
    public string JobId { get; set; } = "";
    public string OutputName { get; set; } = "";
    public string OutputIcon { get; set; } = "";
    public double Progress { get; set; }
    public string ProgressPercentDisplay { get; set; } = "";
    public double ProgressBarWidth { get; set; }
    public string TimeRemainingDisplay { get; set; } = "";
    public bool IsComplete { get; set; }
}

/// <summary>
/// Anzeige-Modell für ein Crafting-Inventar-Produkt im UI.
/// </summary>
public class InventoryItemDisplay
{
    public string ProductId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string QuantityDisplay { get; set; } = "";
    public string ValueDisplay { get; set; } = "";
}

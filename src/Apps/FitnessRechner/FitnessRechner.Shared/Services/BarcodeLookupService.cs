using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitnessRechner.Models;
using FitnessRechner.Resources.Strings;

namespace FitnessRechner.Services;

public class BarcodeLookupService : IBarcodeLookupService
{
    // Static HttpClient for efficient socket usage (Microsoft Best Practice)
    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://world.openfoodfacts.org/api/v2/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly IFoodSearchService _foodSearchService;
    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private Dictionary<string, CachedBarcodeEntry> _barcodeCache = new();

    static BarcodeLookupService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FitnessRechner - Avalonia - Version 1.0");
    }

    public BarcodeLookupService(IFoodSearchService foodSearchService)
    {
        _foodSearchService = foodSearchService;

        // Cache file in AppData directory
        _cacheFilePath = Path.Combine(GetDataDirectory(), "barcode_cache.json");
        _ = LoadCacheAsync();
    }

    public async Task<FoodItem?> LookupByBarcodeAsync(string barcode)
    {
        await _cacheLock.WaitAsync();
        try
        {
            // 1. Check cache (with invalidation after 30 days)
            if (_barcodeCache.TryGetValue(barcode, out var cachedEntry))
            {
                if ((DateTime.UtcNow - cachedEntry.CachedAt).TotalDays <= 30)
                {
                    // Update scan statistics
                    cachedEntry.ScannedCount++;
                    cachedEntry.LastScannedAt = DateTime.UtcNow;
                    await SaveCacheInternalAsync();
                    return cachedEntry.Food;
                }
                else
                {
                    // Cache expired - remove
                    _barcodeCache.Remove(barcode);
                    await SaveCacheInternalAsync();
                }
            }

            // 2. First search in local database (if already available)
            var localResult = _foodSearchService.Search(barcode, 1).FirstOrDefault();
            if (localResult != null)
            {
                return localResult.Food;
            }

            // 3. Query Open Food Facts API (release lock during network call)
            _cacheLock.Release();
            OpenFoodFactsResponse? apiResponse;
            try
            {
                var response = await _httpClient.GetAsync($"product/{barcode}");
                if (!response.IsSuccessStatusCode)
                    return null;

                apiResponse = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>();
            }
            finally
            {
                await _cacheLock.WaitAsync();
            }

            if (apiResponse?.Status != 1 || apiResponse.Product == null)
                return null;

            var product = apiResponse.Product;

            // Extract nutritional values (per 100g)
            var calories = product.Nutriments?.EnergyKcal100g ?? 0;
            var protein = product.Nutriments?.Proteins100g ?? 0;
            var carbs = product.Nutriments?.Carbohydrates100g ?? 0;
            var fat = product.Nutriments?.Fat100g ?? 0;

            // Extract product name
            var name = product.ProductName ?? product.ProductNameDe ?? product.ProductNameEn ?? AppStrings.UnknownProduct;

            // Create FoodItem
            var foodItem = new FoodItem
            {
                Name = name,
                Category = DetermineCategoryFromProduct(product),
                CaloriesPer100g = calories,
                ProteinPer100g = protein,
                CarbsPer100g = carbs,
                FatPer100g = fat,
                DefaultPortionGrams = 100
            };

            // 4. Save to cache for future offline use
            _barcodeCache[barcode] = new CachedBarcodeEntry
            {
                Barcode = barcode,
                Food = foodItem,
                CachedAt = DateTime.UtcNow,
                ScannedCount = 1,
                LastScannedAt = DateTime.UtcNow
            };
            await SaveCacheInternalAsync();

            return foodItem;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<IReadOnlyList<CachedBarcodeEntry>> GetScanHistoryAsync(int limit = 10)
    {
        await _cacheLock.WaitAsync();
        try
        {
            return _barcodeCache.Values
                .OrderByDescending(e => e.LastScannedAt)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task ClearScanHistoryAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            _barcodeCache.Clear();
            await SaveCacheInternalAsync();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task LoadCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            if (File.Exists(_cacheFilePath))
            {
                var json = await File.ReadAllTextAsync(_cacheFilePath);
                var entries = JsonSerializer.Deserialize<List<CachedBarcodeEntry>>(json);
                if (entries != null)
                {
                    _barcodeCache = entries.ToDictionary(e => e.Barcode, e => e);
                }
            }
        }
        catch (Exception)
        {
            _barcodeCache = new();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Saves cache to disk. Must be called within a _cacheLock context.
    /// </summary>
    private async Task SaveCacheInternalAsync()
    {
        try
        {
            var entries = _barcodeCache.Values.ToList();
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception)
        {
            // Ignore file write errors - cache is non-critical
        }
    }

    private FoodCategory DetermineCategoryFromProduct(OpenFoodFactsProduct product)
    {
        var categories = product.CategoriesTags?.ToList() ?? new List<string>();

        // Determine category based on tags
        if (categories.Any(c => c.Contains("fruit") || c.Contains("obst")))
            return FoodCategory.Fruit;

        if (categories.Any(c => c.Contains("vegetable") || c.Contains("gemuse")))
            return FoodCategory.Vegetable;

        if (categories.Any(c => c.Contains("meat") || c.Contains("fleisch")))
            return FoodCategory.Meat;

        if (categories.Any(c => c.Contains("fish") || c.Contains("fisch")))
            return FoodCategory.Fish;

        if (categories.Any(c => c.Contains("dairy") || c.Contains("milk") || c.Contains("milch")))
            return FoodCategory.Dairy;

        if (categories.Any(c => c.Contains("bread") || c.Contains("cereal") || c.Contains("grain") || c.Contains("getreide")))
            return FoodCategory.Grain;

        if (categories.Any(c => c.Contains("beverage") || c.Contains("drink") || c.Contains("getrank")))
            return FoodCategory.Beverage;

        if (categories.Any(c => c.Contains("snack") || c.Contains("candy") || c.Contains("chocolate") || c.Contains("sweet")))
            return FoodCategory.Snack;

        // Default
        return FoodCategory.Other;
    }

    private static string GetDataDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitnessRechner");
        Directory.CreateDirectory(dir);
        return dir;
    }
}

#region Open Food Facts API Models

public class OpenFoodFactsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

public class OpenFoodFactsProduct
{
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_de")]
    public string? ProductNameDe { get; set; }

    [JsonPropertyName("product_name_en")]
    public string? ProductNameEn { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("categories_tags")]
    public string[]? CategoriesTags { get; set; }

    [JsonPropertyName("nutriments")]
    public OpenFoodFactsNutriments? Nutriments { get; set; }
}

public class OpenFoodFactsNutriments
{
    [JsonPropertyName("energy-kcal_100g")]
    public double EnergyKcal100g { get; set; }

    [JsonPropertyName("proteins_100g")]
    public double Proteins100g { get; set; }

    [JsonPropertyName("carbohydrates_100g")]
    public double Carbohydrates100g { get; set; }

    [JsonPropertyName("fat_100g")]
    public double Fat100g { get; set; }
}

#endregion

#region Barcode Cache Models

public class CachedBarcodeEntry
{
    public string Barcode { get; set; } = "";
    public FoodItem Food { get; set; } = new();
    public DateTime CachedAt { get; set; }
    public int ScannedCount { get; set; } = 1;
    public DateTime LastScannedAt { get; set; } = DateTime.UtcNow;
}

#endregion

using FitnessRechner.Models;

namespace FitnessRechner.Services;

/// <summary>
/// Service for intelligent food search
/// </summary>
public interface IFoodSearchService
{
    /// <summary>
    /// Wird ausgelöst wenn ein neuer Food-Log-Eintrag hinzugefügt wurde.
    /// </summary>
    event Action? FoodLogAdded;

    /// <summary>
    /// Searches for foods with fuzzy matching
    /// </summary>
    /// <param name="query">Search term</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <returns>Sorted list by relevance</returns>
    IReadOnlyList<FoodSearchResult> Search(string query, int maxResults = 10);

    /// <summary>
    /// Returns all foods of a category
    /// </summary>
    IReadOnlyList<FoodItem> GetByCategory(FoodCategory category);

    /// <summary>
    /// Returns all available categories
    /// </summary>
    IReadOnlyList<FoodCategory> GetCategories();

    /// <summary>
    /// Saves a food log entry
    /// </summary>
    Task SaveFoodLogAsync(FoodLogEntry entry);

    /// <summary>
    /// Loads all log entries for a date
    /// </summary>
    Task<IReadOnlyList<FoodLogEntry>> GetFoodLogAsync(DateTime date);

    /// <summary>
    /// Loads the daily summary
    /// </summary>
    Task<DailyNutritionSummary> GetDailySummaryAsync(DateTime date);

    /// <summary>
    /// Deletes a log entry
    /// </summary>
    Task DeleteFoodLogAsync(string entryId);

    /// <summary>
    /// Saves a food item as favorite
    /// </summary>
    Task SaveFavoriteAsync(FoodItem food);

    /// <summary>
    /// Gets all favorite foods sorted by usage count
    /// </summary>
    Task<IReadOnlyList<FavoriteFoodEntry>> GetFavoritesAsync();

    /// <summary>
    /// Removes a food from favorites
    /// </summary>
    Task RemoveFavoriteAsync(string id);

    /// <summary>
    /// Checks if a food is already a favorite
    /// </summary>
    Task<bool> IsFavoriteAsync(string foodName);

    /// <summary>
    /// Increments the usage count for a favorite
    /// </summary>
    Task IncrementFavoriteUsageAsync(string foodName);

    /// <summary>
    /// Archives food log entries older than specified months
    /// </summary>
    /// <param name="monthsOld">Archive entries older than this (default: 6 months)</param>
    /// <returns>Number of archived entries</returns>
    Task<int> ArchiveOldEntriesAsync(int monthsOld = 6);

    /// <summary>
    /// Gets the count of entries that would be archived
    /// </summary>
    Task<int> GetArchivableEntriesCountAsync(int monthsOld = 6);

    /// <summary>
    /// Clears the archive file (permanent deletion)
    /// </summary>
    Task ClearArchiveAsync();

    #region Recipes

    /// <summary>
    /// Saves a recipe
    /// </summary>
    Task SaveRecipeAsync(Recipe recipe);

    /// <summary>
    /// Gets all saved recipes sorted by usage
    /// </summary>
    Task<IReadOnlyList<Recipe>> GetRecipesAsync();

    /// <summary>
    /// Deletes a recipe by ID
    /// </summary>
    Task DeleteRecipeAsync(string recipeId);

    /// <summary>
    /// Increments the usage count for a recipe
    /// </summary>
    Task IncrementRecipeUsageAsync(string recipeId);

    /// <summary>
    /// Updates an existing recipe
    /// </summary>
    Task UpdateRecipeAsync(Recipe recipe);

    #endregion
}

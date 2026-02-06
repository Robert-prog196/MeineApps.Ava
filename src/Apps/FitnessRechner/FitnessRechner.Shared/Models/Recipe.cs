namespace FitnessRechner.Models;

/// <summary>
/// A recipe consisting of multiple food items with portions.
/// Allows users to save frequently used meal combinations.
/// </summary>
public class Recipe
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Recipe name (e.g., "Morning Smoothie", "Chicken Salad")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the recipe was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// How many times this recipe has been used
    /// </summary>
    public int TimesUsed { get; set; } = 0;

    /// <summary>
    /// The ingredients with their portions
    /// </summary>
    public List<RecipeIngredient> Ingredients { get; set; } = [];

    /// <summary>
    /// Total number of servings this recipe makes
    /// </summary>
    public int Servings { get; set; } = 1;

    #region Calculated Properties

    /// <summary>
    /// Total calories for the entire recipe
    /// </summary>
    public double TotalCalories => Ingredients.Sum(i => i.Calories);

    /// <summary>
    /// Total protein for the entire recipe
    /// </summary>
    public double TotalProtein => Ingredients.Sum(i => i.Protein);

    /// <summary>
    /// Total carbs for the entire recipe
    /// </summary>
    public double TotalCarbs => Ingredients.Sum(i => i.Carbs);

    /// <summary>
    /// Total fat for the entire recipe
    /// </summary>
    public double TotalFat => Ingredients.Sum(i => i.Fat);

    /// <summary>
    /// Calories per serving
    /// </summary>
    public double CaloriesPerServing => Servings > 0 ? TotalCalories / Servings : TotalCalories;

    /// <summary>
    /// Protein per serving
    /// </summary>
    public double ProteinPerServing => Servings > 0 ? TotalProtein / Servings : TotalProtein;

    /// <summary>
    /// Carbs per serving
    /// </summary>
    public double CarbsPerServing => Servings > 0 ? TotalCarbs / Servings : TotalCarbs;

    /// <summary>
    /// Fat per serving
    /// </summary>
    public double FatPerServing => Servings > 0 ? TotalFat / Servings : TotalFat;

    #endregion
}

/// <summary>
/// An ingredient in a recipe with its portion size
/// </summary>
public class RecipeIngredient
{
    /// <summary>
    /// The food item
    /// </summary>
    public FoodItem Food { get; set; } = new();

    /// <summary>
    /// Amount in grams
    /// </summary>
    public double Grams { get; set; } = 100;

    #region Calculated Properties

    /// <summary>
    /// Calories for this portion
    /// </summary>
    public double Calories => Food.CaloriesPer100g * Grams / 100;

    /// <summary>
    /// Protein for this portion
    /// </summary>
    public double Protein => Food.ProteinPer100g * Grams / 100;

    /// <summary>
    /// Carbs for this portion
    /// </summary>
    public double Carbs => Food.CarbsPer100g * Grams / 100;

    /// <summary>
    /// Fat for this portion
    /// </summary>
    public double Fat => Food.FatPer100g * Grams / 100;

    #endregion
}

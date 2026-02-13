namespace FitnessRechner.Models;

/// <summary>
/// Represents a favorite food entry saved by the user
/// </summary>
public class FavoriteFoodEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public FoodItem Food { get; set; } = new();
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int TimesUsed { get; set; } = 0;
}

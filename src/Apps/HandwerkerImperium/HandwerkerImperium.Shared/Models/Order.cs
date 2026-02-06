using System.Text.Json.Serialization;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Models;

/// <summary>
/// Represents a contract/order that players can complete for rewards.
/// Orders consist of one or more mini-game tasks.
/// </summary>
public class Order
{
    /// <summary>
    /// Unique identifier for this order.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Localization key for the order title.
    /// </summary>
    [JsonPropertyName("titleKey")]
    public string TitleKey { get; set; } = string.Empty;

    /// <summary>
    /// Fallback title if localization key is not found.
    /// </summary>
    [JsonPropertyName("titleFallback")]
    public string TitleFallback { get; set; } = string.Empty;

    /// <summary>
    /// The workshop type required for this order.
    /// </summary>
    [JsonPropertyName("workshopType")]
    public WorkshopType WorkshopType { get; set; }

    /// <summary>
    /// Difficulty level of the order.
    /// </summary>
    [JsonPropertyName("difficulty")]
    public OrderDifficulty Difficulty { get; set; } = OrderDifficulty.Medium;

    /// <summary>
    /// Mini-games that must be completed for this order.
    /// </summary>
    [JsonPropertyName("tasks")]
    public List<OrderTask> Tasks { get; set; } = [];

    /// <summary>
    /// Base money reward for completing this order.
    /// </summary>
    [JsonPropertyName("baseReward")]
    public decimal BaseReward { get; set; }

    /// <summary>
    /// Base XP reward for completing this order.
    /// </summary>
    [JsonPropertyName("baseXp")]
    public int BaseXp { get; set; }

    /// <summary>
    /// Minimum workshop level required.
    /// </summary>
    [JsonPropertyName("requiredLevel")]
    public int RequiredLevel { get; set; } = 1;

    /// <summary>
    /// When this order was generated.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this order expires (optional, for timed orders).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Current progress through the tasks (0 to Tasks.Count).
    /// </summary>
    [JsonPropertyName("currentTaskIndex")]
    public int CurrentTaskIndex { get; set; }

    /// <summary>
    /// Results of completed tasks.
    /// </summary>
    [JsonPropertyName("taskResults")]
    public List<MiniGameRating> TaskResults { get; set; } = [];

    /// <summary>
    /// Localized display title (populated at runtime by ViewModel).
    /// </summary>
    [JsonIgnore]
    public string DisplayTitle { get; set; } = string.Empty;

    /// <summary>
    /// Localized workshop type name (populated at runtime by ViewModel).
    /// </summary>
    [JsonIgnore]
    public string DisplayWorkshopName { get; set; } = string.Empty;

    /// <summary>
    /// Whether all tasks have been completed.
    /// </summary>
    [JsonIgnore]
    public bool IsCompleted => CurrentTaskIndex >= Tasks.Count;

    /// <summary>
    /// Gets the current task to complete.
    /// </summary>
    [JsonIgnore]
    public OrderTask? CurrentTask => CurrentTaskIndex < Tasks.Count ? Tasks[CurrentTaskIndex] : null;

    /// <summary>
    /// Calculates the final reward based on task performance.
    /// </summary>
    [JsonIgnore]
    public decimal FinalReward
    {
        get
        {
            if (TaskResults.Count == 0) return 0;

            // Average rating percentage
            decimal avgPercentage = TaskResults.Average(r => r.GetRewardPercentage());

            // Apply difficulty multiplier
            return BaseReward * avgPercentage * Difficulty.GetRewardMultiplier();
        }
    }

    /// <summary>
    /// Calculates the final XP based on task performance.
    /// </summary>
    [JsonIgnore]
    public int FinalXp
    {
        get
        {
            if (TaskResults.Count == 0) return 0;

            // Average rating percentage
            decimal avgPercentage = TaskResults.Average(r => r.GetXpPercentage());

            // Apply difficulty multiplier
            return (int)(BaseXp * avgPercentage * Difficulty.GetXpMultiplier());
        }
    }

    /// <summary>
    /// Records a task result and advances to next task.
    /// </summary>
    public void RecordTaskResult(MiniGameRating rating)
    {
        TaskResults.Add(rating);
        CurrentTaskIndex++;
    }
}

/// <summary>
/// A single task within an order.
/// </summary>
public class OrderTask
{
    /// <summary>
    /// The mini-game type for this task.
    /// </summary>
    [JsonPropertyName("gameType")]
    public MiniGameType GameType { get; set; }

    /// <summary>
    /// Localization key for task description.
    /// </summary>
    [JsonPropertyName("descriptionKey")]
    public string DescriptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Fallback description if localization key is not found.
    /// </summary>
    [JsonPropertyName("descriptionFallback")]
    public string DescriptionFallback { get; set; } = string.Empty;
}

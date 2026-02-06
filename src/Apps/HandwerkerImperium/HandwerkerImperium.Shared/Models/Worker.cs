using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Represents a worker in a workshop.
/// Workers generate passive income over time.
/// </summary>
public class Worker
{
    /// <summary>
    /// Unique identifier for this worker.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the worker.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Worker efficiency multiplier (1.0 = normal, higher = faster).
    /// Increases with upgrades.
    /// </summary>
    [JsonPropertyName("efficiency")]
    public decimal Efficiency { get; set; } = 1.0m;

    /// <summary>
    /// Worker's skill level (affects mini-game bonuses).
    /// </summary>
    [JsonPropertyName("skillLevel")]
    public int SkillLevel { get; set; } = 1;

    /// <summary>
    /// When this worker was hired.
    /// </summary>
    [JsonPropertyName("hiredAt")]
    public DateTime HiredAt { get; set; }

    /// <summary>
    /// Creates a new worker with a random international name.
    /// </summary>
    public static Worker CreateRandom()
    {
        var firstNames = new[]
        {
            "Hans", "Klaus", "Peter", "Michael", "Thomas",
            "Stefan", "Andreas", "Markus", "Frank", "Erik",
            "Carlos", "Marco", "Pierre", "James", "Oliver",
            "Lucas", "Matteo", "Hugo", "Leo", "Noah"
        };

        var surnames = new[]
        {
            "M\u00fcller", "Schmidt", "Schneider", "Fischer", "Weber",
            "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann",
            "Martin", "Garcia", "Santos", "Silva", "Rossi"
        };

        var random = new Random();
        return new Worker
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{firstNames[random.Next(firstNames.Length)]} {surnames[random.Next(surnames.Length)]}",
            HiredAt = DateTime.UtcNow
        };
    }
}

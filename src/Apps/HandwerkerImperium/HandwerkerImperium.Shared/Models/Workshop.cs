using System.Text.Json.Serialization;
using HandwerkerImperium.Models.Enums;

namespace HandwerkerImperium.Models;

/// <summary>
/// Represents a workshop/trade in the game.
/// Each workshop can be upgraded and staffed with workers.
/// </summary>
public class Workshop
{
    /// <summary>
    /// The type of this workshop.
    /// </summary>
    [JsonPropertyName("type")]
    public WorkshopType Type { get; set; }

    /// <summary>
    /// Current level of the workshop (1-10).
    /// Higher levels = higher base income and more worker slots.
    /// </summary>
    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    /// <summary>
    /// Workers assigned to this workshop.
    /// </summary>
    [JsonPropertyName("workers")]
    public List<Worker> Workers { get; set; } = [];

    /// <summary>
    /// Total money earned by this workshop (lifetime).
    /// </summary>
    [JsonPropertyName("totalEarned")]
    public decimal TotalEarned { get; set; }

    /// <summary>
    /// Total orders completed by this workshop.
    /// </summary>
    [JsonPropertyName("ordersCompleted")]
    public int OrdersCompleted { get; set; }

    /// <summary>
    /// Maximum workers allowed at current level.
    /// </summary>
    [JsonIgnore]
    public int MaxWorkers => Level switch
    {
        1 => 1,
        2 => 1,
        3 => 2,
        4 => 2,
        5 => 3,
        6 => 3,
        7 => 4,
        8 => 4,
        9 => 5,
        10 => 5,
        _ => 1
    };

    /// <summary>
    /// Base income per second per worker at current level.
    /// </summary>
    [JsonIgnore]
    public decimal BaseIncomePerWorker
    {
        get
        {
            // Base: 5€/s, doubles every 2 levels
            decimal baseIncome = 5m * (decimal)Math.Pow(2, (Level - 1) / 2.0);
            // Apply workshop type multiplier
            return baseIncome * Type.GetBaseIncomeMultiplier();
        }
    }

    /// <summary>
    /// Total income per second from all workers.
    /// </summary>
    [JsonIgnore]
    public decimal IncomePerSecond
    {
        get
        {
            if (Workers.Count == 0) return 0;

            decimal total = 0;
            foreach (var worker in Workers)
            {
                total += BaseIncomePerWorker * worker.Efficiency;
            }
            return total;
        }
    }

    /// <summary>
    /// Cost to upgrade to next level.
    /// </summary>
    [JsonIgnore]
    public decimal UpgradeCost
    {
        get
        {
            if (Level >= 10) return 0; // Max level

            // Exponential cost curve (reduced from 2.5 to 1.8 for better balance)
            // Level 1→2: 100€, Level 9→10: ~1,100€
            return 100m * (decimal)Math.Pow(1.8, Level - 1);
        }
    }

    /// <summary>
    /// Cost to hire a new worker.
    /// </summary>
    [JsonIgnore]
    public decimal HireWorkerCost
    {
        get
        {
            // Base 50€, increases with number of workers
            return 50m * (decimal)Math.Pow(2, Workers.Count);
        }
    }

    /// <summary>
    /// Whether this workshop can be upgraded.
    /// </summary>
    [JsonIgnore]
    public bool CanUpgrade => Level < 10;

    /// <summary>
    /// Whether a new worker can be hired.
    /// </summary>
    [JsonIgnore]
    public bool CanHireWorker => Workers.Count < MaxWorkers;

    /// <summary>
    /// Icon for this workshop type.
    /// </summary>
    [JsonIgnore]
    public string Icon => Type.GetIcon();

    /// <summary>
    /// Creates a new workshop of the specified type.
    /// </summary>
    public static Workshop Create(WorkshopType type)
    {
        return new Workshop { Type = type };
    }
}

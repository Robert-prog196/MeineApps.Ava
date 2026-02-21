using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Ein tägliches vergünstigtes Angebot im Shop.
/// </summary>
public class ShopOffer
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("nameKey")]
    public string NameKey { get; set; } = "";

    [JsonPropertyName("originalPrice")]
    public int OriginalPrice { get; set; }

    [JsonPropertyName("discountedPrice")]
    public int DiscountedPrice { get; set; }

    [JsonPropertyName("discount")]
    public int Discount { get; set; } = 50; // Prozent

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("goldenScrewReward")]
    public int GoldenScrewReward { get; set; }

    [JsonPropertyName("moneyReward")]
    public decimal MoneyReward { get; set; }

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    [JsonIgnore]
    public TimeSpan TimeRemaining => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;

    /// <summary>
    /// Generiert ein zufälliges tägliches Angebot.
    /// </summary>
    public static ShopOffer GenerateDaily(decimal incomePerSecond)
    {
        var offers = new[]
        {
            ("daily_screws_10", "DailyOfferScrews10", 20, 10, 0m),
            ("daily_screws_25", "DailyOfferScrews25", 50, 25, 0m),
            ("daily_money_boost", "DailyOfferMoneyBoost", 15, 0, Math.Max(5000m, incomePerSecond * 600m)),
            ("daily_speed_boost", "DailyOfferSpeedBoost", 10, 0, 0m),
        };

        var selected = offers[Random.Shared.Next(offers.Length)];
        return new ShopOffer
        {
            ItemId = selected.Item1,
            NameKey = selected.Item2,
            OriginalPrice = selected.Item3,
            DiscountedPrice = selected.Item3 / 2,
            Discount = 50,
            GoldenScrewReward = selected.Item4,
            MoneyReward = selected.Item5,
            ExpiresAt = DateTime.UtcNow.Date.AddDays(1) // Bis Mitternacht
        };
    }
}

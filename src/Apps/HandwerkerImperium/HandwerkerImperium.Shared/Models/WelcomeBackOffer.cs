using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Typ des Welcome-Back-Angebots.
/// </summary>
public enum WelcomeBackOfferType
{
    /// <summary>Standard-Angebot bei 24h+ Abwesenheit.</summary>
    Standard,
    /// <summary>Premium-Angebot bei 72h+ Abwesenheit (50% mehr).</summary>
    Premium,
    /// <summary>Einmaliges Starter-Paket für neue Spieler (Level 5-15).</summary>
    StarterPack
}

/// <summary>
/// Ein Welcome-Back-Angebot das nach längerer Abwesenheit angezeigt wird.
/// </summary>
public class WelcomeBackOffer
{
    [JsonPropertyName("type")]
    public WelcomeBackOfferType Type { get; set; }

    [JsonPropertyName("goldenScrewReward")]
    public int GoldenScrewReward { get; set; }

    [JsonPropertyName("moneyReward")]
    public decimal MoneyReward { get; set; }

    [JsonPropertyName("xpReward")]
    public int XpReward { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    [JsonIgnore]
    public TimeSpan TimeRemaining => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTime.UtcNow;
}

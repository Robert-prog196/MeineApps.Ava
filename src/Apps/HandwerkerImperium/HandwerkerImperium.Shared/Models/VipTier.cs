namespace HandwerkerImperium.Models;

/// <summary>
/// VIP-Stufe basierend auf Gesamtausgaben.
/// </summary>
public enum VipTier
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
    Platinum = 4
}

public static class VipTierExtensions
{
    /// <summary>
    /// Mindestausgaben für die VIP-Stufe (in EUR).
    /// </summary>
    public static decimal GetMinSpend(this VipTier tier) => tier switch
    {
        VipTier.Bronze => 4.99m,
        VipTier.Silver => 9.99m,
        VipTier.Gold => 19.99m,
        VipTier.Platinum => 49.99m,
        _ => decimal.MaxValue
    };

    /// <summary>
    /// Einkommens-Bonus der VIP-Stufe.
    /// </summary>
    public static decimal GetIncomeBonus(this VipTier tier) => tier switch
    {
        VipTier.Bronze => 0.05m,
        VipTier.Silver => 0.10m,
        VipTier.Gold => 0.15m,
        VipTier.Platinum => 0.25m,
        _ => 0m
    };

    /// <summary>
    /// XP-Bonus der VIP-Stufe.
    /// </summary>
    public static decimal GetXpBonus(this VipTier tier) => tier switch
    {
        VipTier.Silver => 0.05m,
        VipTier.Gold => 0.10m,
        VipTier.Platinum => 0.15m,
        _ => 0m
    };

    /// <summary>
    /// Kosten-Reduktion der VIP-Stufe.
    /// </summary>
    public static decimal GetCostReduction(this VipTier tier) => tier switch
    {
        VipTier.Gold => 0.05m,
        VipTier.Platinum => 0.10m,
        _ => 0m
    };

    /// <summary>
    /// Farbe der VIP-Stufe.
    /// </summary>
    public static string GetColor(this VipTier tier) => tier switch
    {
        VipTier.Bronze => "#CD7F32",
        VipTier.Silver => "#C0C0C0",
        VipTier.Gold => "#FFD700",
        VipTier.Platinum => "#E5E4E2",
        _ => "#808080"
    };

    /// <summary>
    /// Bestimmt VIP-Stufe aus Gesamtausgaben.
    /// </summary>
    public static VipTier DetermineVipTier(decimal totalSpent)
    {
        if (totalSpent >= VipTier.Platinum.GetMinSpend()) return VipTier.Platinum;
        if (totalSpent >= VipTier.Gold.GetMinSpend()) return VipTier.Gold;
        if (totalSpent >= VipTier.Silver.GetMinSpend()) return VipTier.Silver;
        if (totalSpent >= VipTier.Bronze.GetMinSpend()) return VipTier.Bronze;
        return VipTier.None;
    }
}

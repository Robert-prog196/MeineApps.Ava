namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// Service for In-App Purchases
/// </summary>
public interface IPurchaseService
{
    /// <summary>
    /// Whether the user has premium status (ads removed)
    /// </summary>
    bool IsPremium { get; }

    /// <summary>
    /// Whether the user has an active subscription
    /// </summary>
    bool HasActiveSubscription { get; }

    /// <summary>
    /// Whether the user has purchased lifetime
    /// </summary>
    bool HasLifetime { get; }

    /// <summary>
    /// Initialize the purchase service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Purchase the "remove ads" product (legacy, for other apps)
    /// </summary>
    Task<bool> PurchaseRemoveAdsAsync();

    /// <summary>
    /// Purchase monthly subscription
    /// </summary>
    Task<bool> PurchaseMonthlyAsync();

    /// <summary>
    /// Purchase lifetime package
    /// </summary>
    Task<bool> PurchaseLifetimeAsync();

    /// <summary>
    /// Restore previous purchases
    /// </summary>
    Task<bool> RestorePurchasesAsync();

    /// <summary>
    /// Purchase a consumable product (e.g. golden screws, instant cash).
    /// Platform-specific: Override in Android for real Google Play Billing.
    /// </summary>
    Task<bool> PurchaseConsumableAsync(string productId);

    /// <summary>
    /// Event fired when premium status changes
    /// </summary>
    event EventHandler? PremiumStatusChanged;
}

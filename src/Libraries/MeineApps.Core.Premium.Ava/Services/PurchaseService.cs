using MeineApps.Core.Ava.Services;

namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// Implementation of the purchase service.
/// Manages purchase state via IPreferencesService.
/// Actual billing operations (Google Play / Desktop) must be implemented
/// in platform-specific code by overriding the virtual purchase methods.
/// </summary>
public class PurchaseService : IPurchaseService
{
    // Product IDs
    public const string RemoveAdsProductId = "remove_ads";
    public const string MonthlyProductId = "premium_monthly";
    public const string LifetimeProductId = "premium_lifetime";

    // Preferences Keys
    private const string PremiumKey = "is_premium";
    private const string SubscriptionKey = "has_subscription";
    private const string LifetimeKey = "has_lifetime";

    private readonly IPreferencesService _preferences;
    private readonly IAdService _adService;
    private bool _isPremium;
    private bool _hasSubscription;
    private bool _hasLifetime;

    public bool IsPremium => _isPremium || _hasSubscription || _hasLifetime;
    public bool HasActiveSubscription => _hasSubscription;
    public bool HasLifetime => _hasLifetime;

    public event EventHandler? PremiumStatusChanged;

    public PurchaseService(IPreferencesService preferences, IAdService adService)
    {
        _preferences = preferences;
        _adService = adService;
        _isPremium = _preferences.Get(PremiumKey, false);
        _hasSubscription = _preferences.Get(SubscriptionKey, false);
        _hasLifetime = _preferences.Get(LifetimeKey, false);
    }

    public virtual async Task InitializeAsync()
    {
        if (IsPremium)
        {
            _adService.DisableAds();
            return;
        }

        try
        {
            await RestorePurchasesAsync();
        }
        catch
        {
            // Ignore errors during initialization
        }
    }

    public virtual Task<bool> PurchaseRemoveAdsAsync()
    {
        // Override in platform-specific implementation
        System.Diagnostics.Debug.WriteLine("PurchaseRemoveAdsAsync: Not implemented on this platform");
        return Task.FromResult(false);
    }

    public virtual Task<bool> PurchaseMonthlyAsync()
    {
        // Override in platform-specific implementation
        System.Diagnostics.Debug.WriteLine("PurchaseMonthlyAsync: Not implemented on this platform");
        return Task.FromResult(false);
    }

    public virtual Task<bool> PurchaseLifetimeAsync()
    {
        // Override in platform-specific implementation
        System.Diagnostics.Debug.WriteLine("PurchaseLifetimeAsync: Not implemented on this platform");
        return Task.FromResult(false);
    }

    public virtual Task<bool> RestorePurchasesAsync()
    {
        // Override in platform-specific implementation
        System.Diagnostics.Debug.WriteLine("RestorePurchasesAsync: Not implemented on this platform");
        return Task.FromResult(false);
    }

    public virtual Task<bool> PurchaseConsumableAsync(string productId)
    {
        // Override in platform-specific implementation (Google Play Billing)
        System.Diagnostics.Debug.WriteLine($"PurchaseConsumableAsync({productId}): Not implemented on this platform");
        return Task.FromResult(false);
    }

    /// <summary>
    /// Set premium status (called by platform-specific code after successful purchase)
    /// </summary>
    public void SetPremiumStatus(bool isPremium)
    {
        _isPremium = isPremium;
        _preferences.Set(PremiumKey, isPremium);
        UpdateAdsAndNotify();
    }

    /// <summary>
    /// Set subscription status (called by platform-specific code)
    /// </summary>
    public void SetSubscriptionStatus(bool hasSubscription)
    {
        _hasSubscription = hasSubscription;
        _preferences.Set(SubscriptionKey, hasSubscription);
        UpdateAdsAndNotify();
    }

    /// <summary>
    /// Set lifetime status (called by platform-specific code)
    /// </summary>
    public void SetLifetimeStatus(bool hasLifetime)
    {
        _hasLifetime = hasLifetime;
        _preferences.Set(LifetimeKey, hasLifetime);
        UpdateAdsAndNotify();
    }

    private void UpdateAdsAndNotify()
    {
        if (IsPremium)
        {
            _adService.DisableAds();
        }
        PremiumStatusChanged?.Invoke(this, EventArgs.Empty);
    }
}

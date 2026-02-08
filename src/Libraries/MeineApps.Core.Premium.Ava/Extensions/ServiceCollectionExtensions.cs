using MeineApps.Core.Premium.Ava.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MeineApps.Core.Premium.Ava.Extensions;

/// <summary>
/// Extension methods for IServiceCollection - Premium Services (Ads, Billing, Trial)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add MeineApps Premium services (AdMob, Purchase, Trial)
    /// </summary>
    public static IServiceCollection AddMeineAppsPremium(this IServiceCollection services)
    {
        services.AddSingleton<IAdService, AdMobService>();
        services.AddSingleton<IPurchaseService, PurchaseService>();
        services.AddSingleton<ITrialService, TrialService>();
        services.AddSingleton<IRewardedAdService, RewardedAdService>();

        return services;
    }

    /// <summary>
    /// Add MeineApps Premium services with custom purchase service (for platform-specific billing)
    /// </summary>
    public static IServiceCollection AddMeineAppsPremium<TPurchaseService>(this IServiceCollection services)
        where TPurchaseService : class, IPurchaseService
    {
        services.AddSingleton<IAdService, AdMobService>();
        services.AddSingleton<IPurchaseService, TPurchaseService>();
        services.AddSingleton<ITrialService, TrialService>();
        services.AddSingleton<IRewardedAdService, RewardedAdService>();

        return services;
    }
}

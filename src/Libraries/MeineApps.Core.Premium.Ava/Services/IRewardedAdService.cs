namespace MeineApps.Core.Premium.Ava.Services;

/// <summary>
/// Service fuer belohnende Werbevideos (Rewarded Ads).
/// Desktop: Simuliert. Android: Echte Google AdMob Rewarded Ads.
/// Unterstuetzt mehrere Placements pro App (z.B. "continue", "export_pdf").
/// </summary>
public interface IRewardedAdService
{
    /// <summary>Ob Rewarded Ads verfuegbar sind (false wenn Premium oder disabled)</summary>
    bool IsAvailable { get; }

    /// <summary>Zeigt Rewarded Ad (Default-Placement). Gibt true zurueck wenn User komplett geschaut hat.</summary>
    Task<bool> ShowAdAsync();

    /// <summary>
    /// Zeigt Rewarded Ad fuer ein bestimmtes Placement.
    /// Verschiedene Placements nutzen verschiedene Ad-Unit-IDs fuer AdMob-Tracking.
    /// </summary>
    /// <param name="placement">Placement-Name (z.B. "continue", "export_pdf", "barcode_scan")</param>
    Task<bool> ShowAdAsync(string placement);

    /// <summary>Deaktiviert Rewarded Ads (z.B. nach Premium-Kauf)</summary>
    void Disable();

    /// <summary>Wird gefeuert wenn kein Werbevideo verfuegbar ist (z.B. kein Fill, Timeout)</summary>
    event Action? AdUnavailable;
}

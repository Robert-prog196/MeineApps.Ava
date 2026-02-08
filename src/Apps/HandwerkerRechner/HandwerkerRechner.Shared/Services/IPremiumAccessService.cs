namespace HandwerkerRechner.Services;

/// <summary>
/// Service fuer temporaeren Premium-Zugang nach Rewarded Ad (30 Min).
/// Premium-Nutzer haben immer Zugang.
/// </summary>
public interface IPremiumAccessService
{
    /// <summary>true wenn Premium ODER temporaerer Zugang aktiv</summary>
    bool HasAccess { get; }

    /// <summary>Wann der temporaere Zugang ablaeuft (null wenn kein Zugang)</summary>
    DateTime? AccessExpiresAt { get; }

    /// <summary>Verbleibende Minuten (0 wenn kein Zugang)</summary>
    int RemainingMinutes { get; }

    /// <summary>Gewaehrt temporaeren Zugang</summary>
    void GrantTemporaryAccess(TimeSpan duration);

    /// <summary>Event wenn temporaerer Zugang ablaeuft</summary>
    event EventHandler? AccessExpired;

    /// <summary>true wenn erweiterte History aktiv (24h nach Rewarded Ad)</summary>
    bool HasExtendedHistory { get; }

    /// <summary>Gewaehrt 24h Zugang zur erweiterten History (30 statt 5 Eintraege)</summary>
    void GrantExtendedHistory();

    /// <summary>Gibt das aktuelle History-Limit zurueck (5 oder 30)</summary>
    int GetHistoryLimit();
}

namespace FitnessRechner.Services;

/// <summary>
/// Begrenzt kostenlose Barcode-Scans pro Tag (3 free, +5 via Ad).
/// Premium-Nutzer haben unbegrenzte Scans.
/// </summary>
public interface IScanLimitService
{
    /// <summary>Verbleibende Scans fuer heute</summary>
    int RemainingScans { get; }

    /// <summary>Ob ein Scan moeglich ist (Premium = immer true)</summary>
    bool CanScan { get; }

    /// <summary>Einen Scan verbrauchen</summary>
    void UseOneScan();

    /// <summary>Scans hinzufuegen (z.B. nach Rewarded Ad)</summary>
    void AddScans(int count);
}

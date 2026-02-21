using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Verwaltet das Glücksrad mit täglichem Gratis-Spin und kostenpflichtigen Spins.
/// </summary>
public interface ILuckySpinService
{
    /// <summary>
    /// Ob ein kostenloser Spin verfügbar ist (einmal täglich).
    /// </summary>
    bool HasFreeSpin { get; }

    /// <summary>
    /// Kosten pro Spin in Goldschrauben (wenn kein Gratis-Spin verfügbar).
    /// </summary>
    int SpinCost { get; }

    /// <summary>
    /// Führt einen Spin durch (Gratis oder kostenpflichtig).
    /// Gibt den Gewinntyp zurück.
    /// </summary>
    LuckySpinPrizeType Spin();

    /// <summary>
    /// Wendet den Gewinn auf den GameState an.
    /// </summary>
    void ApplyPrize(LuckySpinPrizeType prizeType);
}

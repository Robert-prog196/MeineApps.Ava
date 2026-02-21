using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Service für saisonale Events (4x pro Jahr, jeweils 2 Wochen).
/// </summary>
public interface ISeasonalEventService
{
    /// <summary>Feuert wenn sich das saisonale Event ändert (Start/Ende).</summary>
    event Action? SeasonalEventChanged;

    /// <summary>Prüft ob ein saisonales Event starten/enden sollte.</summary>
    void CheckSeasonalEvent();

    /// <summary>Ob gerade ein saisonales Event aktiv ist.</summary>
    bool IsEventActive { get; }

    /// <summary>Fügt Saison-Währung hinzu.</summary>
    void AddSeasonalCurrency(int amount);

    /// <summary>Kauft ein Item im saisonalen Shop. Gibt true bei Erfolg zurück.</summary>
    bool BuySeasonalItem(string itemId);
}

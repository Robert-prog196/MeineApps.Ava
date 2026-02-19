namespace BomberBlast.Services;

/// <summary>
/// Verwaltet die Erstentdeckung von PowerUps und Welt-Mechaniken.
/// Zeigt beim ersten Kontakt einen Erklärungs-Hint an.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>Ob ein Item bereits entdeckt wurde</summary>
    bool IsDiscovered(string id);

    /// <summary>Item als entdeckt markieren</summary>
    void MarkDiscovered(string id);

    /// <summary>
    /// Gibt den RESX-Key für den Hint-Text zurück, falls das Item noch nicht entdeckt wurde.
    /// Markiert das Item gleichzeitig als entdeckt.
    /// Gibt null zurück wenn bereits bekannt.
    /// </summary>
    string? GetDiscoveryTitleKey(string id);

    /// <summary>
    /// Gibt den RESX-Key für die Hint-Beschreibung zurück.
    /// </summary>
    string? GetDiscoveryDescKey(string id);
}

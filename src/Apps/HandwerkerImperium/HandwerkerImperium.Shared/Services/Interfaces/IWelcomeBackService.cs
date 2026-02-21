namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Generiert und verwaltet Welcome-Back-Angebote nach längerer Abwesenheit.
/// </summary>
public interface IWelcomeBackService
{
    /// <summary>
    /// Wird ausgelöst wenn ein neues Angebot generiert wurde.
    /// </summary>
    event Action? OfferGenerated;

    /// <summary>
    /// Prüft die Abwesenheitsdauer und generiert ggf. ein passendes Angebot.
    /// Wird nach App-Start und Offline-Einnahmen aufgerufen.
    /// </summary>
    void CheckAndGenerateOffer();

    /// <summary>
    /// Beansprucht das aktive Angebot und schreibt Belohnungen gut.
    /// </summary>
    void ClaimOffer();

    /// <summary>
    /// Verwirft das aktive Angebot ohne Belohnung.
    /// </summary>
    void DismissOffer();
}

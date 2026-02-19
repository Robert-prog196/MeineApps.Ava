namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// In-App Review Timing-Service.
/// Prüft ob der Benutzer um eine Bewertung gebeten werden soll.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Ob eine Review-Anfrage angezeigt werden soll.
    /// </summary>
    bool ShouldPromptReview();

    /// <summary>
    /// Markiert, dass eine Review-Anfrage angezeigt wurde.
    /// </summary>
    void MarkReviewPrompted();

    /// <summary>
    /// Wird bei Spieler-Meilensteinen aufgerufen.
    /// </summary>
    /// <param name="type">Typ: "level", "prestige", "orders"</param>
    /// <param name="value">Wert des Meilensteins</param>
    void OnMilestone(string type, int value);
}

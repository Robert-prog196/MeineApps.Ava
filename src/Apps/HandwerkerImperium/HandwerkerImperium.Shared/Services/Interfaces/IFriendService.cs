namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Service für simulierte Freunde (tägliche Geschenke, Freundschafts-Level).
/// </summary>
public interface IFriendService
{
    /// <summary>Feuert wenn sich der Freunde-Zustand ändert.</summary>
    event Action? FriendsUpdated;

    /// <summary>Initialisiert Freundes-Liste falls leer.</summary>
    void Initialize();

    /// <summary>Generiert tägliche Geschenke von Freunden.</summary>
    void GenerateDailyGifts();

    /// <summary>Nimmt ein Geschenk von einem Freund an.</summary>
    void ClaimGift(string friendId);

    /// <summary>Sendet ein Geschenk an einen Freund (kostet 1 Goldschraube).</summary>
    void SendGift(string friendId);
}

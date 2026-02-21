using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Verwaltet simulierte Freunde mit täglichen Geschenken.
/// 5 simulierte Freunde senden täglich Goldschrauben.
/// Zurückschenken erhöht das Freundschafts-Level (max 5) und damit den Geschenk-Betrag.
/// </summary>
public class FriendService : IFriendService
{
    private readonly IGameStateService _gameState;

    public event Action? FriendsUpdated;

    public FriendService(IGameStateService gameState)
    {
        _gameState = gameState;
    }

    public void Initialize()
    {
        var state = _gameState.State;

        // Freundes-Liste erstellen falls leer
        if (state.Friends.Count == 0)
        {
            state.Friends = Friend.CreateSimulatedFriends();
            _gameState.MarkDirty();
        }
    }

    public void GenerateDailyGifts()
    {
        var state = _gameState.State;
        if (state.Friends.Count == 0) return;

        bool changed = false;

        foreach (var friend in state.Friends)
        {
            // Simuliert: Freund sendet täglich ein Geschenk
            // HasGiftAvailable prüft ob LastGiftSent.Date < heute
            if (friend.HasGiftAvailable)
            {
                // LastGiftSent auf gestern setzen, damit HasGiftAvailable = true bleibt
                // (der Spieler muss das Geschenk erst abholen via ClaimGift)
                // Geschenk ist verfügbar wenn LastGiftSent älter als heute ist
                changed = true;
            }
        }

        if (changed)
        {
            _gameState.MarkDirty();
            FriendsUpdated?.Invoke();
        }
    }

    public void ClaimGift(string friendId)
    {
        var state = _gameState.State;
        var friend = state.Friends.FirstOrDefault(f => f.Id == friendId);
        if (friend == null) return;

        // Prüfen ob Geschenk verfügbar
        if (!friend.HasGiftAvailable) return;

        // Goldschrauben gutschreiben (basierend auf Freundschafts-Level)
        _gameState.AddGoldenScrews(friend.GiftAmount);

        // Geschenk als abgeholt markieren
        friend.LastGiftSent = DateTime.UtcNow;

        _gameState.MarkDirty();
        FriendsUpdated?.Invoke();
    }

    public void SendGift(string friendId)
    {
        var state = _gameState.State;
        var friend = state.Friends.FirstOrDefault(f => f.Id == friendId);
        if (friend == null) return;

        // Bereits heute zurückgeschenkt?
        if (friend.HasSentGiftToday) return;

        // 1 Goldschraube kostet das Zurückschenken
        if (!_gameState.TrySpendGoldenScrews(1)) return;

        friend.LastGiftReceived = DateTime.UtcNow;

        // Freundschafts-Level erhöhen (max 5)
        if (friend.FriendshipLevel < 5)
            friend.FriendshipLevel++;

        _gameState.MarkDirty();
        FriendsUpdated?.Invoke();
    }
}

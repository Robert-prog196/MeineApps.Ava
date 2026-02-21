using System.Text.Json.Serialization;

namespace HandwerkerImperium.Models;

/// <summary>
/// Ein simulierter Freund der täglich kleine Geschenke sendet.
/// </summary>
public class Friend
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("lastGiftSent")]
    public DateTime LastGiftSent { get; set; } = DateTime.MinValue;

    [JsonPropertyName("lastGiftReceived")]
    public DateTime LastGiftReceived { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Freundschafts-Level (1-5, steigt durch Geschenke).
    /// </summary>
    [JsonPropertyName("friendshipLevel")]
    public int FriendshipLevel { get; set; } = 1;

    /// <summary>
    /// Ob heute ein Geschenk verfügbar ist.
    /// </summary>
    [JsonIgnore]
    public bool HasGiftAvailable => LastGiftSent.Date < DateTime.UtcNow.Date;

    /// <summary>
    /// Ob heute bereits zurückgeschenkt wurde.
    /// </summary>
    [JsonIgnore]
    public bool HasSentGiftToday => LastGiftReceived.Date >= DateTime.UtcNow.Date;

    /// <summary>
    /// Goldschrauben-Geschenk basierend auf Freundschafts-Level.
    /// </summary>
    [JsonIgnore]
    public int GiftAmount => FriendshipLevel switch
    {
        1 => 1,
        2 => 1,
        3 => 2,
        4 => 2,
        _ => 3
    };

    /// <summary>
    /// Erstellt 5 simulierte Freunde.
    /// </summary>
    public static List<Friend> CreateSimulatedFriends()
    {
        var names = new[] { "MaxBuilder", "LisaCraft", "TomHammer", "SarahPro", "OttoMeister" };
        var friends = new List<Friend>();

        for (int i = 0; i < 5; i++)
        {
            friends.Add(new Friend
            {
                Id = $"friend_{i}",
                Name = names[i],
                Level = Random.Shared.Next(5, 50),
                FriendshipLevel = 1
            });
        }

        return friends;
    }
}

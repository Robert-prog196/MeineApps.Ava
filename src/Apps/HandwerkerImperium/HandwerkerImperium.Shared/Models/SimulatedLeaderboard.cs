namespace HandwerkerImperium.Models;

/// <summary>
/// Kategorie einer simulierten Bestenliste.
/// </summary>
public enum LeaderboardCategory
{
    HighestLevel,
    MostGoldenScrews,
    MiniGameScore,
    TournamentsWon
}

/// <summary>
/// Ein Eintrag in der simulierten Bestenliste.
/// </summary>
public class LeaderboardEntry
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public int Rank { get; set; }
    public bool IsPlayer { get; set; }

    /// <summary>
    /// Generiert eine simulierte Bestenliste mit 20 Einträgen.
    /// Spieler-Score wird eingeordnet.
    /// </summary>
    public static List<LeaderboardEntry> Generate(LeaderboardCategory category, int playerScore, string playerName = "Du")
    {
        var names = new[]
        {
            "ProBauer42", "MeisterMax", "HandwerkHeld", "SchrauberKing",
            "WerkstattWolf", "HammerHanna", "BaustelleBob", "CraftQueen",
            "ToolMaster99", "ElektroErik", "RohrRebell", "PinselPaula",
            "DachdeckerDan", "MalerMartin", "KabelKurt", "FliesenFritz",
            "BetonBernd", "SägenSusi", "NagelNina", "ZollstockZoe"
        };

        var entries = new List<LeaderboardEntry>();
        int baseScore = Math.Max(10, playerScore);

        // 20 simulierte Spieler mit Scores um den Spieler herum verteilt
        for (int i = 0; i < 20; i++)
        {
            // Einige besser, einige schlechter als der Spieler
            double factor = 0.3 + (i / 20.0) * 1.8;
            int score = (int)(baseScore * factor);
            entries.Add(new LeaderboardEntry
            {
                Name = names[i],
                Score = score,
                IsPlayer = false
            });
        }

        // Spieler hinzufügen
        entries.Add(new LeaderboardEntry
        {
            Name = playerName,
            Score = playerScore,
            IsPlayer = true
        });

        // Sortieren (höchster Score zuerst) und Ränge vergeben
        entries = entries.OrderByDescending(e => e.Score).ToList();
        for (int i = 0; i < entries.Count; i++)
            entries[i].Rank = i + 1;

        return entries;
    }
}

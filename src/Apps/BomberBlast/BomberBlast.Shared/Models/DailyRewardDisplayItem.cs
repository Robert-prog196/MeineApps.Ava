namespace BomberBlast.Models;

/// <summary>
/// Darstellungs-Modell fuer einen Tag im Daily-Reward-Popup.
/// </summary>
public class DailyRewardDisplayItem
{
    public int Day { get; init; }
    public string DayText { get; init; } = "";
    public string CoinsText { get; init; } = "";
    public bool HasExtraLife { get; init; }
    public bool IsClaimed { get; init; }
    public bool IsCurrentDay { get; init; }
    public bool IsFuture { get; init; }

    /// <summary>Border-Farbe: Gold fuer aktuellen Tag, Gruen fuer claimed, Grau fuer Zukunft</summary>
    public string BorderColor => IsCurrentDay ? "#FFD700" : IsClaimed ? "#22C55E" : "#444444";

    /// <summary>Hintergrund: dunkler bei Zukunft</summary>
    public string BackgroundColor => IsCurrentDay ? "#3A2A0A" : IsClaimed ? "#0A2A0A" : "#1A1A2A";

    /// <summary>Opacity: gedimmt fuer Zukunft</summary>
    public double CardOpacity => IsFuture ? 0.5 : 1.0;
}

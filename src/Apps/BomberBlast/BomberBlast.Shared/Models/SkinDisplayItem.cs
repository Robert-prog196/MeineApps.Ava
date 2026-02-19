using Avalonia.Media;

namespace BomberBlast.Models;

/// <summary>
/// Darstellungs-Modell für einen Skin in der Shop-Ansicht.
/// </summary>
public class SkinDisplayItem
{
    /// <summary>Skin-ID (z.B. "gold", "neon")</summary>
    public string Id { get; init; } = "";

    /// <summary>Lokalisierter Anzeigename</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Primärfarbe des Skins</summary>
    public Color PrimaryColor { get; init; }

    /// <summary>Sekundärfarbe (Akzent)</summary>
    public Color SecondaryColor { get; init; }

    /// <summary>Ob nur für Premium-Nutzer</summary>
    public bool IsPremiumOnly { get; init; }

    /// <summary>Ob dieser Skin gerade ausgewählt ist</summary>
    public bool IsEquipped { get; set; }

    /// <summary>Ob gesperrt (Premium-Only + kein Premium)</summary>
    public bool IsLocked { get; set; }

    /// <summary>Ob der Skin einen Glow-Effekt hat</summary>
    public bool HasGlow { get; init; }

    /// <summary>Status-Text ("Ausgewählt", "Nur Premium" etc.)</summary>
    public string StatusText { get; set; } = "";

    /// <summary>Karten-Opacity (gedimmt wenn gesperrt)</summary>
    public double CardOpacity => IsLocked ? 0.5 : 1.0;

    /// <summary>Ob der Auswählen-Button sichtbar ist</summary>
    public bool CanSelect => !IsLocked && !IsEquipped;
}

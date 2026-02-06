namespace HandwerkerImperium.Models;

/// <summary>
/// Represents a single step in the tutorial.
/// </summary>
public class TutorialStep
{
    /// <summary>
    /// Step number (0-based).
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Localization key for the title.
    /// </summary>
    public string TitleKey { get; set; } = "";

    /// <summary>
    /// Fallback title in English.
    /// </summary>
    public string TitleFallback { get; set; } = "";

    /// <summary>
    /// Localization key for the description.
    /// </summary>
    public string DescriptionKey { get; set; } = "";

    /// <summary>
    /// Fallback description in English.
    /// </summary>
    public string DescriptionFallback { get; set; } = "";

    /// <summary>
    /// Name of the UI element to highlight (x:Name in XAML).
    /// Null means no specific element (full-screen message).
    /// </summary>
    public string? TargetElement { get; set; }

    /// <summary>
    /// Position of the tutorial text relative to the highlighted element.
    /// </summary>
    public TutorialTextPosition TextPosition { get; set; } = TutorialTextPosition.Bottom;

    /// <summary>
    /// Whether this step requires user interaction with the highlighted element.
    /// </summary>
    public bool RequiresInteraction { get; set; }

    /// <summary>
    /// Icon/emoji for the step.
    /// </summary>
    public string Icon { get; set; } = "\ud83d\udca1";
}

/// <summary>
/// Position of tutorial text relative to highlighted element.
/// </summary>
public enum TutorialTextPosition
{
    Top,
    Bottom,
    Left,
    Right,
    Center
}

/// <summary>
/// Predefined tutorial steps.
/// </summary>
public static class TutorialSteps
{
    public static List<TutorialStep> GetAll()
    {
        return
        [
            new()
            {
                StepNumber = 0,
                TitleKey = "TutorialWelcomeTitle",
                TitleFallback = "Welcome!",
                DescriptionKey = "TutorialWelcomeDesc",
                DescriptionFallback = "Build your craftsman empire from scratch! Let's show you how it works.",
                TargetElement = null,
                TextPosition = TutorialTextPosition.Center,
                Icon = "\ud83d\udc4b"
            },
            new()
            {
                StepNumber = 1,
                TitleKey = "TutorialWorkshopsTitle",
                TitleFallback = "Your Workshops",
                DescriptionKey = "TutorialWorkshopsDesc",
                DescriptionFallback = "These are your workshops. Tap on one to see details and upgrades.",
                TargetElement = "WorkshopsSection",
                TextPosition = TutorialTextPosition.Bottom,
                Icon = "\ud83c\udfed"
            },
            new()
            {
                StepNumber = 2,
                TitleKey = "TutorialOrdersTitle",
                TitleFallback = "Orders",
                DescriptionKey = "TutorialOrdersDesc",
                DescriptionFallback = "Accept orders to earn money and XP. Each order has mini-games to complete.",
                TargetElement = "OrdersSection",
                TextPosition = TutorialTextPosition.Top,
                Icon = "\ud83d\udce6"
            },
            new()
            {
                StepNumber = 3,
                TitleKey = "TutorialMiniGameTitle",
                TitleFallback = "Mini-Games",
                DescriptionKey = "TutorialMiniGameDesc",
                DescriptionFallback = "Complete mini-games to finish orders. Aim for Perfect ratings for bonus rewards!",
                TargetElement = null,
                TextPosition = TutorialTextPosition.Center,
                Icon = "\ud83c\udfae"
            },
            new()
            {
                StepNumber = 4,
                TitleKey = "TutorialUpgradeTitle",
                TitleFallback = "Upgrades",
                DescriptionKey = "TutorialUpgradeDesc",
                DescriptionFallback = "Upgrade your workshops to increase income. The \u2191 button shows the upgrade cost.",
                TargetElement = "WorkshopsSection",
                TextPosition = TutorialTextPosition.Bottom,
                Icon = "\u2b06\ufe0f"
            },
            new()
            {
                StepNumber = 5,
                TitleKey = "TutorialWorkersTitle",
                TitleFallback = "Workers",
                DescriptionKey = "TutorialWorkersDesc",
                DescriptionFallback = "Hire workers to earn money automatically - even while you're away!",
                TargetElement = null,
                TextPosition = TutorialTextPosition.Center,
                Icon = "\ud83d\udc77"
            },
            new()
            {
                StepNumber = 6,
                TitleKey = "TutorialShopTitle",
                TitleFallback = "Shop",
                DescriptionKey = "TutorialShopDesc",
                DescriptionFallback = "Visit the shop for boosts and bonuses. You can also go ad-free with Premium!",
                TargetElement = "ShopButton",
                TextPosition = TutorialTextPosition.Top,
                Icon = "\ud83d\uded2"
            },
            new()
            {
                StepNumber = 7,
                TitleKey = "TutorialCompleteTitle",
                TitleFallback = "You're Ready!",
                DescriptionKey = "TutorialCompleteDesc",
                DescriptionFallback = "That's all! Start accepting orders and build your empire. Good luck!",
                TargetElement = null,
                TextPosition = TutorialTextPosition.Center,
                Icon = "\ud83c\udf89"
            }
        ];
    }
}

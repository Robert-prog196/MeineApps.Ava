namespace SocialPostGenerator;

/// <summary>
/// Post-Vorlagen für X/Twitter (max 280 Zeichen, 1-2 Hashtags) und Reddit (authentischer Ton, keine Hashtags)
/// </summary>
static class PostTemplates
{
    // === X/Twitter Posts (max 280 Zeichen) ===

    public static string GenerateXPost(AppInfo app, string version, PostCategory category)
    {
        var templates = GetXTemplates(category);
        var template = templates[Random.Shared.Next(templates.Length)];
        return FillTemplate(template, app, version);
    }

    static string[][] GetXTemplates(PostCategory category) => category switch
    {
        PostCategory.LaunchUpdate =>
        [
            [
                "Just pushed v{version} of {app} -- {feature1} and {feature2}.",
                "",
                "Built with .NET + Avalonia for Android, Windows & Linux.",
                "",
                "#indiedev"
            ],
            [
                "{app} v{version} is out! {shortDesc}",
                "",
                "{price}. Android + Desktop.",
                "",
                "#indiedev #{tag1}"
            ],
            [
                "New update for {app} (v{version}):",
                "",
                "{feature1}, {feature2}, and more.",
                "",
                "Free on Android + Desktop. #indiedev"
            ],
            [
                "v{version} of {app} just dropped.",
                "",
                "{feature1}. {feature2}. {platforms}.",
                "",
                "#indiedev #{tag1}"
            ],
        ],

        PostCategory.FeatureSpotlight =>
        [
            [
                "Did you know {app} has {feature1}?",
                "",
                "Plus {feature2} and {feature3}.",
                "",
                "Free on Android + Desktop. #indiedev"
            ],
            [
                "Feature spotlight: {feature1} in {app}.",
                "",
                "Also: {feature2}. Works on Android, Windows & Linux.",
                "",
                "#indiedev #{tag1}"
            ],
            [
                "One of my favorite features in {app}: {feature1}.",
                "",
                "Building a {type} that actually works cross-platform.",
                "",
                "#indiedev"
            ],
        ],

        PostCategory.FreeNoAds =>
        [
            [
                "Built {app} -- a full {type}.",
                "",
                "Completely free. No ads. No tracking. No subscriptions.",
                "",
                "Android, Windows & Linux. #indiedev #freeapp"
            ],
            [
                "Not everything needs a subscription.",
                "",
                "{app}: {shortDesc}",
                "",
                "100% free, zero ads. #indiedev #freeapp"
            ],
            [
                "I built a {type} and made it completely free.",
                "",
                "{feature1}. {feature2}. No ads, no catch.",
                "",
                "#indiedev #freeapp"
            ],
        ],

        PostCategory.IndieDevStory =>
        [
            [
                "Solo dev from Germany building 8 cross-platform apps.",
                "",
                "Today working on {app} -- {feature1}.",
                "",
                ".NET + Avalonia = Android, Windows & Linux from one codebase.",
                "",
                "#indiedev"
            ],
            [
                "I'm a solo dev and I build apps for Android + Desktop.",
                "",
                "{app}: {shortDesc}",
                "",
                "All built with Avalonia. #indiedev #{tag1}"
            ],
            [
                "8 apps. 1 developer. 3 platforms.",
                "",
                "Currently polishing {app} (v{version}) -- {feature1}.",
                "",
                "#indiedev #solodev"
            ],
        ],

        PostCategory.Comparison =>
        [
            [
                "Looking for a {type} that works on Android AND Desktop?",
                "",
                "{app}: {feature1}, {feature2}. {price}.",
                "",
                "#indiedev"
            ],
            [
                "Most {type} apps are mobile-only.",
                "",
                "{app} runs on Android, Windows & Linux. Same app, same data.",
                "",
                "{feature1}. #indiedev"
            ],
            [
                "Why I built {app}: I wanted a {type} that works everywhere.",
                "",
                "{feature1}. {feature2}. {platforms}.",
                "",
                "#indiedev #{tag1}"
            ],
        ],

        PostCategory.CallToAction =>
        [
            [
                "Building {app} in public. Would love your feedback!",
                "",
                "{shortDesc}",
                "",
                "Free on Android + Desktop. #indiedev #buildinpublic"
            ],
            [
                "Beta testing {app} (v{version}) -- a {type} for Android + Desktop.",
                "",
                "Looking for testers! Join:",
                "{testerLink}",
                "",
                "#indiedev #betatest"
            ],
            [
                "Shipped {app} v{version}. Now I need real-world feedback.",
                "",
                "{feature1}. {feature2}.",
                "",
                "Testers welcome: {testerLink}",
                "",
                "#indiedev"
            ],
        ],

        _ => [["Check out {app}! #indiedev"]]
    };

    // === Reddit Posts (keine Hashtags, authentischer Ton) ===

    public static (string Title, string Body) GenerateRedditPost(AppInfo app, string version, PostCategory category)
    {
        var titles = GetRedditTitles(category);
        var bodies = GetRedditBodies(category);

        var title = FillTemplate(titles[Random.Shared.Next(titles.Length)], app, version);
        var body = FillTemplate(bodies[Random.Shared.Next(bodies.Length)], app, version);

        return (title, body);
    }

    static string[][] GetRedditTitles(PostCategory category) => category switch
    {
        PostCategory.LaunchUpdate =>
        [
            ["I just released v{version} of {app} -- {shortDesc}"],
            ["{app} v{version} is out! A {type} for Android + Desktop"],
            ["After months of work, {app} v{version} is ready for testing"],
        ],
        PostCategory.FeatureSpotlight =>
        [
            ["I built a {type} with {feature1} -- here's how it works"],
            ["Feature deep-dive: {feature1} in {app}"],
        ],
        PostCategory.FreeNoAds =>
        [
            ["I built {app} -- a completely free {type} with no ads"],
            ["Made a free {type} with zero ads. Here's why."],
        ],
        PostCategory.IndieDevStory =>
        [
            ["I'm a solo dev from Germany building 8 apps -- here's my latest: {app}"],
            ["Solo dev here: I build cross-platform apps with .NET + Avalonia"],
        ],
        PostCategory.Comparison =>
        [
            ["I wanted a {type} that works on Android AND Desktop, so I built one"],
            ["Most {type} apps are mobile-only. I built {app} for 3 platforms."],
        ],
        PostCategory.CallToAction =>
        [
            ["Looking for beta testers for {app} -- a {type} for Android + Desktop"],
            ["Built a {type}, now I need feedback. Beta testers welcome!"],
        ],
        _ => [["Check out {app}"]]
    };

    static string[][] GetRedditBodies(PostCategory category) => category switch
    {
        PostCategory.LaunchUpdate or PostCategory.FeatureSpotlight or PostCategory.Comparison =>
        [
            [
                "Hey everyone!",
                "",
                "I'm a solo developer from Germany, building cross-platform apps with .NET and Avalonia.",
                "",
                "**{app}** is a {type} that runs on Android, Windows, and Linux from the same codebase.",
                "",
                "Key features:",
                "- {feature1}",
                "- {feature2}",
                "- {feature3}",
                "- Available in 6 languages (DE, EN, ES, FR, IT, PT)",
                "- {price}",
                "",
                "Currently in closed beta on Google Play. Would love any feedback!",
                "",
                "Join testers: {testerLink}",
                "",
                "Happy to answer any questions about the tech stack or design decisions."
            ],
        ],
        PostCategory.FreeNoAds =>
        [
            [
                "Hey!",
                "",
                "I built **{app}** -- a {type}. It's completely free with zero ads, no tracking, no subscriptions.",
                "",
                "Why free? Because I wanted to build something useful without the typical monetization BS.",
                "",
                "Features:",
                "- {feature1}",
                "- {feature2}",
                "- {feature3}",
                "- Runs on Android, Windows, and Linux",
                "- 6 languages",
                "",
                "Currently in closed beta. Feedback welcome!",
                "",
                "Join: {testerLink}"
            ],
        ],
        PostCategory.IndieDevStory =>
        [
            [
                "Hey everyone!",
                "",
                "I'm a solo developer from Germany. Over the past year, I've been building 8 cross-platform apps with .NET and Avalonia (a WPF-like framework that targets Android, Windows, and Linux).",
                "",
                "My latest project is **{app}** -- a {type}.",
                "",
                "What it does:",
                "- {feature1}",
                "- {feature2}",
                "- {feature3}",
                "",
                "The cool part: all 8 apps share the same core libraries and design system. One codebase, three platforms.",
                "",
                "Currently all apps are in closed beta. I'd love feedback from this community!",
                "",
                "Join testers: {testerLink}",
                "",
                "AMA about the tech stack, architecture, or anything else!"
            ],
        ],
        PostCategory.CallToAction =>
        [
            [
                "Hey!",
                "",
                "I've been working on **{app}** (v{version}) -- a {type} for Android, Windows, and Linux.",
                "",
                "Features:",
                "- {feature1}",
                "- {feature2}",
                "- {feature3}",
                "- {price}",
                "",
                "It's currently in closed beta on Google Play and I'm looking for testers who can give honest feedback.",
                "",
                "**How to join:**",
                "1. Join our Google Group: {testerLink}",
                "2. You'll get access to the Play Store listing",
                "3. Install and test!",
                "",
                "All feedback is welcome -- bugs, UX suggestions, feature requests.",
                "",
                "Thanks!"
            ],
        ],
        _ =>
        [
            [
                "Check out **{app}** -- a {type}.",
                "",
                "Features: {feature1}, {feature2}, {feature3}.",
                "",
                "{price}. Available on Android, Windows & Linux."
            ],
        ]
    };

    // === Template-Ersetzung ===

    static string FillTemplate(string[] lines, AppInfo app, string version)
    {
        var text = string.Join("\n", lines);
        return FillTemplate(text, app, version);
    }

    static string FillTemplate(string text, AppInfo app, string version)
    {
        // Zufällige Features auswählen (shuffled)
        var shuffled = app.KeyFeatures.OrderBy(_ => Random.Shared.Next()).ToArray();
        var f1 = shuffled.Length > 0 ? shuffled[0] : "";
        var f2 = shuffled.Length > 1 ? shuffled[1] : "";
        var f3 = shuffled.Length > 2 ? shuffled[2] : "";

        // Zufälliger Tag
        var tag1 = app.Tags.Length > 0 ? app.Tags[Random.Shared.Next(app.Tags.Length)] : "app";

        return text
            .Replace("{app}", app.Name)
            .Replace("{version}", version)
            .Replace("{type}", app.Type)
            .Replace("{price}", app.Price)
            .Replace("{shortDesc}", app.ShortDescription)
            .Replace("{feature1}", f1)
            .Replace("{feature2}", f2)
            .Replace("{feature3}", f3)
            .Replace("{tag1}", tag1)
            .Replace("{platforms}", "Android | Windows | Linux")
            .Replace("{testerLink}", app.TesterLink);
    }
}

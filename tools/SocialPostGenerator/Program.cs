using TextCopy;

namespace SocialPostGenerator;

class Program
{
    static string _baseDir = null!;

    static void Main(string[] args)
    {
        // Solution-Root ermitteln (2 Ebenen hoch von tools/SocialPostGenerator/)
        _baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        var apps = AppRegistry.GetAll();
        var versions = VersionDetector.GetAllVersions(_baseDir, apps.Select(a => a.Name).ToArray());

        if (args.Length > 0)
        {
            HandleCli(args, apps, versions);
            return;
        }

        RunInteractive(apps, versions);
    }

    // === CLI-Modus ===
    static void HandleCli(string[] args, AppInfo[] apps, Dictionary<string, string> versions)
    {
        var cmd = args[0].ToLowerInvariant();
        switch (cmd)
        {
            case "post":
                if (args.Length < 3)
                {
                    PrintColor("Verwendung: post <AppName> <x|reddit>", ConsoleColor.Red);
                    return;
                }
                var app = apps.FirstOrDefault(a => a.Name.Contains(args[1], StringComparison.OrdinalIgnoreCase));
                if (app == null) { PrintColor($"App '{args[1]}' nicht gefunden.", ConsoleColor.Red); return; }
                var platform = args[2].ToLowerInvariant() == "reddit" ? Platform.Reddit : Platform.X;
                var version = versions.GetValueOrDefault(app.Name, "?.?.?");
                GenerateAndCopyPost(app, version, platform, null);
                break;

            case "image":
                if (args.Length < 2)
                {
                    PrintColor("Verwendung: image <AppName|portfolio>", ConsoleColor.Red);
                    return;
                }
                if (args[1].Equals("portfolio", StringComparison.OrdinalIgnoreCase))
                {
                    GeneratePortfolioImage(apps, versions);
                }
                else
                {
                    var imgApp = apps.FirstOrDefault(a => a.Name.Contains(args[1], StringComparison.OrdinalIgnoreCase));
                    if (imgApp == null) { PrintColor($"App '{args[1]}' nicht gefunden.", ConsoleColor.Red); return; }
                    GenerateAppImage(imgApp, versions.GetValueOrDefault(imgApp.Name, "?.?.?"));
                }
                break;

            case "all":
                PrintAllPosts(apps, versions);
                break;

            default:
                PrintColor($"Unbekannter Befehl: {cmd}", ConsoleColor.Red);
                PrintUsage();
                break;
        }
    }

    // === Interaktiver Modus ===
    static void RunInteractive(AppInfo[] apps, Dictionary<string, string> versions)
    {
        while (true)
        {
            Console.Clear();
            PrintColor("=== SocialPostGenerator ===\n", ConsoleColor.Cyan);

            Console.WriteLine("[1] X/Twitter Post generieren");
            Console.WriteLine("[2] Reddit Post generieren");
            Console.WriteLine("[3] Promo-Bild generieren");
            Console.WriteLine("[4] Portfolio-Bild (alle 8 Apps)");
            Console.WriteLine("[5] Alle Posts anzeigen");
            Console.WriteLine("[0] Beenden\n");

            var choice = ReadChoice("Auswahl", 0, 5);
            if (choice == 0) break;

            if (choice == 4)
            {
                GeneratePortfolioImage(apps, versions);
                WaitForKey();
                continue;
            }

            if (choice == 5)
            {
                PrintAllPosts(apps, versions);
                WaitForKey();
                continue;
            }

            // App auswählen
            Console.WriteLine();
            for (int i = 0; i < apps.Length; i++)
            {
                var v = versions.GetValueOrDefault(apps[i].Name, "?.?.?");
                var priceTag = apps[i].HasAds ? "" : " [KOSTENLOS]";
                PrintColor($"  [{i + 1}] {apps[i].Name} v{v}{priceTag}", ConsoleColor.White);
            }
            Console.WriteLine();

            var appIdx = ReadChoice("App", 1, apps.Length) - 1;
            var selectedApp = apps[appIdx];
            var selectedVersion = versions.GetValueOrDefault(selectedApp.Name, "?.?.?");

            if (choice == 3)
            {
                GenerateAppImage(selectedApp, selectedVersion);
                WaitForKey();
                continue;
            }

            var selectedPlatform = choice == 1 ? Platform.X : Platform.Reddit;

            // Post-Kategorie wählen
            Console.WriteLine();
            var categories = GetAvailableCategories(selectedApp);
            for (int i = 0; i < categories.Length; i++)
            {
                Console.WriteLine($"  [{i + 1}] {GetCategoryName(categories[i])}");
            }
            Console.WriteLine();

            var catIdx = ReadChoice("Kategorie", 1, categories.Length) - 1;

            Console.WriteLine();
            GenerateAndCopyPost(selectedApp, selectedVersion, selectedPlatform, categories[catIdx]);
            WaitForKey();
        }
    }

    // === Post generieren und in Zwischenablage ===
    static void GenerateAndCopyPost(AppInfo app, string version, Platform platform, PostCategory? category)
    {
        // Wenn keine Kategorie, zufällig wählen
        var cat = category ?? GetAvailableCategories(app)[Random.Shared.Next(GetAvailableCategories(app).Length)];

        if (platform == Platform.X)
        {
            var post = PostTemplates.GenerateXPost(app, version, cat);
            var charCount = post.Length;
            var charColor = charCount <= 280 ? ConsoleColor.Green : ConsoleColor.Red;

            PrintColor("--- X/Twitter Post ---", ConsoleColor.Cyan);
            Console.WriteLine();
            Console.WriteLine(post);
            Console.WriteLine();
            PrintColor($"[{charCount}/280 Zeichen]", charColor);

            try { ClipboardService.SetText(post); PrintColor("\n[In Zwischenablage kopiert]", ConsoleColor.Green); }
            catch { PrintColor("\n[Zwischenablage nicht verfügbar]", ConsoleColor.Yellow); }
        }
        else
        {
            var (title, body) = PostTemplates.GenerateRedditPost(app, version, cat);

            PrintColor("--- Reddit Post ---", ConsoleColor.Cyan);
            PrintColor($"\nTitel: {title}", ConsoleColor.White);
            Console.WriteLine();
            Console.WriteLine(body);

            try
            {
                ClipboardService.SetText($"{title}\n\n{body}");
                PrintColor("\n[In Zwischenablage kopiert]", ConsoleColor.Green);
            }
            catch { PrintColor("\n[Zwischenablage nicht verfügbar]", ConsoleColor.Yellow); }
        }

        // Screenshot-Vorschläge
        Console.WriteLine();
        SuggestScreenshots(app, cat);
    }

    // === Screenshot-Vorschläge ===
    static void SuggestScreenshots(AppInfo app, PostCategory category)
    {
        var releasesDir = Path.Combine(_baseDir, "Releases", app.Name);
        if (!Directory.Exists(releasesDir)) return;

        PrintColor("Passende Screenshots:", ConsoleColor.Yellow);

        // phone_1 ist immer der Hero-Shot
        var hero = Path.Combine(releasesDir, "phone_1.png");
        if (File.Exists(hero))
            Console.WriteLine($"  {hero}");

        // Feature-Spotlight: mehrere Screenshots
        if (category == PostCategory.FeatureSpotlight)
        {
            for (int i = 2; i <= 4; i++)
            {
                var f = Path.Combine(releasesDir, $"phone_{i}.png");
                if (File.Exists(f)) Console.WriteLine($"  {f}");
            }
        }

        // Feature-Graphic für Launch-Posts
        if (category == PostCategory.LaunchUpdate)
        {
            var fg = Path.Combine(releasesDir, "feature_graphic.png");
            if (File.Exists(fg)) Console.WriteLine($"  {fg}");
        }
    }

    // === Bild-Generierung ===
    static void GenerateAppImage(AppInfo app, string version)
    {
        var outputDir = Path.Combine(_baseDir, "Releases", "SocialPosts");
        Directory.CreateDirectory(outputDir);
        var iconPath = Path.Combine(_baseDir, "Releases", app.Name, "icon_512.png");

        ImageGenerator.GeneratePromoCard(outputDir, app, version, iconPath);
        PrintColor($"\nPromo-Bild: {Path.Combine(outputDir, $"{app.Name}_promo.png")}", ConsoleColor.Green);
    }

    static void GeneratePortfolioImage(AppInfo[] apps, Dictionary<string, string> versions)
    {
        var outputDir = Path.Combine(_baseDir, "Releases", "SocialPosts");
        Directory.CreateDirectory(outputDir);

        var iconPaths = new Dictionary<string, string>();
        foreach (var app in apps)
        {
            var p = Path.Combine(_baseDir, "Releases", app.Name, "icon_512.png");
            if (File.Exists(p)) iconPaths[app.Name] = p;
        }

        ImageGenerator.GeneratePortfolio(outputDir, apps, iconPaths);
        PrintColor($"\nPortfolio-Bild: {Path.Combine(outputDir, "portfolio.png")}", ConsoleColor.Green);
    }

    // === Alle Posts anzeigen ===
    static void PrintAllPosts(AppInfo[] apps, Dictionary<string, string> versions)
    {
        PrintColor("\n=== Alle Posts (Übersicht) ===\n", ConsoleColor.Cyan);

        foreach (var app in apps)
        {
            var version = versions.GetValueOrDefault(app.Name, "?.?.?");
            PrintColor($"\n--- {app.Name} v{version} ---", ConsoleColor.Yellow);

            // Ein X-Post pro Kategorie
            foreach (var cat in GetAvailableCategories(app))
            {
                var post = PostTemplates.GenerateXPost(app, version, cat);
                Console.WriteLine($"\n  [{GetCategoryName(cat)}] ({post.Length} Zeichen)");
                Console.WriteLine($"  {post}");
            }
        }
    }

    // === Hilfsmethoden ===

    static PostCategory[] GetAvailableCategories(AppInfo app)
    {
        var cats = new List<PostCategory>
        {
            PostCategory.LaunchUpdate,
            PostCategory.FeatureSpotlight,
            PostCategory.IndieDevStory,
            PostCategory.Comparison,
            PostCategory.CallToAction
        };

        // Free/No-Ads nur für werbefreie Apps
        if (!app.HasAds)
            cats.Insert(2, PostCategory.FreeNoAds);

        return cats.ToArray();
    }

    static string GetCategoryName(PostCategory cat) => cat switch
    {
        PostCategory.LaunchUpdate => "Launch / Update",
        PostCategory.FeatureSpotlight => "Feature Spotlight",
        PostCategory.FreeNoAds => "Free & No Ads",
        PostCategory.IndieDevStory => "Indie Dev Story",
        PostCategory.Comparison => "Comparison / Alternative",
        PostCategory.CallToAction => "Call to Action (Feedback)",
        _ => cat.ToString()
    };

    static int ReadChoice(string label, int min, int max)
    {
        while (true)
        {
            Console.Write($"{label} [{min}-{max}]: ");
            if (int.TryParse(Console.ReadLine(), out var val) && val >= min && val <= max)
                return val;
            PrintColor("Ungültige Eingabe.", ConsoleColor.Red);
        }
    }

    static void WaitForKey()
    {
        Console.WriteLine();
        PrintColor("[Enter drücken]", ConsoleColor.DarkGray);
        Console.ReadLine();
    }

    static void PrintColor(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    static void PrintUsage()
    {
        Console.WriteLine("\nVerwendung:");
        Console.WriteLine("  (kein Argument)              Interaktiver Modus");
        Console.WriteLine("  post <App> <x|reddit>        Post generieren");
        Console.WriteLine("  image <App|portfolio>         Promo-Bild generieren");
        Console.WriteLine("  all                          Alle Posts anzeigen");
    }
}

enum Platform { X, Reddit }
enum PostCategory { LaunchUpdate, FeatureSpotlight, FreeNoAds, IndieDevStory, Comparison, CallToAction }

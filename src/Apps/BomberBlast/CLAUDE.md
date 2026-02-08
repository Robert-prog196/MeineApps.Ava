# BomberBlast - Avalonia Port

## Overview
Bomberman-clone game with SkiaSharp rendering, AI pathfinding, and multiple input methods.
Landscape-only on Android. Ported from .NET MAUI to Avalonia UI.
Grid: 15x10 (previously 11x9). Two visual styles: Classic HD + Neon/Cyberpunk. HUD on right side.

## Project Structure

```
BomberBlast/
├── BomberBlast.Shared/          # Shared code (net10.0)
│   ├── App.axaml(.cs)           # Application entry, DI setup
│   ├── AI/                      # Enemy AI + A* pathfinding
│   │   ├── EnemyAI.cs
│   │   └── PathFinding/AStar.cs
│   ├── Core/                    # Game engine
│   │   ├── GameEngine.cs        # Main game logic
│   │   ├── GameState.cs         # State enum
│   │   ├── GameTimer.cs         # Timer management
│   │   └── SoundManager.cs      # Audio (ISoundService abstraction)
│   ├── Graphics/                # SkiaSharp rendering
│   │   ├── GameRenderer.cs      # Full game renderer + HUD (Classic/Neon palettes, side HUD)
│   │   └── SpriteSheet.cs       # Sprite management
│   ├── Input/                   # Input system
│   │   ├── IInputHandler.cs     # Interface
│   │   ├── InputType.cs         # Enum (FloatingJoystick, Swipe, DPad, Keyboard)
│   │   ├── FloatingJoystick.cs
│   │   ├── SwipeGestureHandler.cs
│   │   ├── DPadHandler.cs
│   │   ├── KeyboardHandler.cs   # Arrow/WASD + Space/E for desktop
│   │   └── InputManager.cs      # Manages active input handler
│   ├── Models/
│   │   ├── Entities/            # Player, Enemy, Bomb, Explosion, PowerUp, Direction
│   │   ├── Grid/                # Cell, CellType, GameGrid (15x10)
│   │   ├── Levels/              # Level, LevelGenerator (50 levels + arcade)
│   │   ├── UpgradeType.cs       # Enum: 6 Shop-Upgrade-Typen
│   │   ├── PlayerUpgrades.cs    # Persistenter Upgrade-Stand (Preise, MaxLevel)
│   │   └── ShopDisplayItem.cs   # ObservableObject fuer Shop-UI Binding
│   ├── Services/
│   │   ├── ISoundService.cs     # Sound abstraction
│   │   ├── IProgressService.cs  # Game progress persistence
│   │   ├── ProgressService.cs
│   │   ├── IHighScoreService.cs
│   │   ├── HighScoreService.cs
│   │   ├── IGameStyleService.cs # Visual style (Classic/Neon)
│   │   ├── GameStyleService.cs  # Persists style via IPreferencesService
│   │   ├── ICoinService.cs      # Persistente Coin-Waehrung
│   │   ├── CoinService.cs       # Balance, AddCoins, TrySpendCoins
│   │   ├── IShopService.cs      # 6 Upgrades, Preise, Kauf-Logik
│   │   ├── ShopService.cs       # PlayerUpgrades Persistenz
│   │   ├── IRewardedAdService.cs # Rewarded Ad Abstraktion
│   │   └── RewardedAdService.cs  # Desktop-Simulator (Task.Delay)
│   ├── ViewModels/              # 10 ViewModels (MVVM)
│   │   ├── MainViewModel.cs     # Navigation/view switching (10 child VMs)
│   │   ├── GameViewModel.cs     # 60fps game loop + Coin-Events
│   │   ├── MainMenuViewModel.cs # Coins-Badge, Shop-Button
│   │   ├── LevelSelectViewModel.cs # World-Gating, Stern-Header
│   │   ├── SettingsViewModel.cs
│   │   ├── HighScoresViewModel.cs
│   │   ├── GameOverViewModel.cs # Coins, Verdoppeln, Weitermachen
│   │   ├── PauseViewModel.cs
│   │   ├── HelpViewModel.cs
│   │   └── ShopViewModel.cs     # 6 Upgrades kaufen
│   ├── Views/                   # 10 Avalonia views
│   │   ├── MainWindow.axaml(.cs)
│   │   ├── MainView.axaml(.cs)
│   │   ├── MainMenuView.axaml(.cs) # Coin-Badge + Shop-Button
│   │   ├── GameView.axaml(.cs)  # SKCanvasView for SkiaSharp
│   │   ├── LevelSelectView.axaml(.cs) # Welt-Header + Coin-Badge
│   │   ├── SettingsView.axaml(.cs)
│   │   ├── HighScoresView.axaml(.cs)
│   │   ├── GameOverView.axaml(.cs) # Coins + Verdoppeln + Weitermachen
│   │   ├── ShopView.axaml(.cs)  # 6 Upgrade-Karten, Coin-Stand
│   │   └── HelpView.axaml(.cs)
│   └── Resources/Strings/       # 6 languages (DE, EN, ES, FR, IT, PT)
├── BomberBlast.Desktop/         # Desktop entry (net10.0)
│   ├── Program.cs
│   └── BomberBlast.Desktop.csproj
└── BomberBlast.Android/         # Android entry (net10.0-android)
    ├── MainActivity.cs          # Landscape-only
    ├── AndroidManifest.xml      # com.meineapps.bomberblast
    └── BomberBlast.Android.csproj
```

## Build Commands

```bash
# Shared only
dotnet build src/Apps/BomberBlast/BomberBlast.Shared/BomberBlast.Shared.csproj

# Desktop
dotnet build src/Apps/BomberBlast/BomberBlast.Desktop/BomberBlast.Desktop.csproj

# Android
dotnet build src/Apps/BomberBlast/BomberBlast.Android/BomberBlast.Android.csproj
```

## Architecture Notes

- **Navigation**: MainViewModel acts as view-switcher (IsXxxActive booleans), no Shell/Router
- **Game Loop**: DispatcherTimer at ~16ms intervals in GameViewModel
- **Rendering**: Full SkiaSharp via SKCanvasView (Avalonia.Skia package)
- **DI**: Microsoft.Extensions.DependencyInjection in App.axaml.cs
- **Sound**: ISoundService abstraction (NullSoundService registered as default)
- **Input**: 4 handlers (FloatingJoystick, SwipeGesture, ClassicDPad, Keyboard) managed by InputManager
- **Keyboard**: Arrow/WASD movement, Space=bomb, E=detonate, Escape=pause. Auto-switches on desktop
- **DPI Handling**: GameView uses canvas.LocalClipBounds (not e.Info) for DPI-correct rendering

## Key Dependencies

| Package | Purpose |
|---------|---------|
| Avalonia | UI framework |
| Avalonia.Skia | SkiaSharp integration |
| Material.Icons.Avalonia | Material icons |
| CommunityToolkit.Mvvm | MVVM source generators |
| SkiaSharp | 2D graphics rendering |
| Avalonia.Labs.Controls | Additional controls |

## Version
- v2.0.0 (Avalonia port, ApplicationDisplayVersion in Android csproj)

## Code Review (06.02.2026)
- Debug.WriteLine entfernt: GameViewModel (1), SettingsViewModel (11)
- GameEngine Overlay-Strings lokalisiert: WaveOverlay, StageOverlay, Paused, TapToResume, LevelComplete, ScoreFormat, TimeBonusFormat, GameOver, FinalScore, WaveReached, LevelFormat (via ILocalizationService)
- GameOverViewModel: "Wave/Level" Strings lokalisiert (ILocalizationService injiziert)
- GameEngine + SpriteSheet in DI registriert (fehlten in App.axaml.cs)
- AlertRequested Event-Pattern (statt Debug.WriteLine Fallback) in SettingsViewModel
- App-Version: Dynamisch via Assembly (MainMenuViewModel + SettingsViewModel)
- 151 Lokalisierungs-Keys in 6 Sprachen (alle konsistent, keine fehlenden)
- Alle Views: Compiled Bindings korrekt, Touch-Targets >= 48px
- Keine DateTime.Parse-Probleme, kein MAUI Preferences.Default

## Deep Code Review Fixes (06.02.2026)

### MainViewModel Navigation-Bugs (KRITISCH)
- **GameOver-Parameter nicht geparst**: NavigateTo("GameOver?score=...") hat SetParameters() nie aufgerufen → Score/Level immer 0
- **Game-Loop nie gestartet**: OnAppearingAsync() wurde nie aufgerufen → Spiel startete nicht
- **Compound-Routes**: "//MainMenu/Game?mode=arcade" (TryAgain) fiel in default → zeigte MainMenu statt neues Spiel
- **OnDisappearing fehlte**: Game-Loop lief weiter im Hintergrund nach Navigation weg vom Game
- **Settings-Ruecknavigation**: ".." aus Settings waehrend Spiel ging zu MainMenu statt zurueck zum Spiel (_returnToGameFromSettings)
- **IsHelpActive fehlte**: Kein Help-View in Navigation, kein Property, kein Panel-Eintrag

### GameViewModel
- SetParameters() setzt _isInitialized=false → neues Spiel wird korrekt initialisiert (TryAgain funktioniert)

### GameView.axaml.cs (KRITISCH)
- InvalidateCanvasRequested Event war NICHT verdrahtet → Canvas hat nie neu gerendert!
- DataContextChanged Event hinzugefuegt: subscribed/unsubscribes InvalidateCanvasRequested

### HelpView.axaml erstellt
- Fehlte komplett (HelpViewModel existierte, aber keine View)
- Lokalisierte Hilfe-Seite: HowToPlay, Controls, 8 PowerUps, 8 Enemies, Tips
- In MainView.axaml Panel eingebunden

### GameRenderer GC-Optimierung
- 6 per-frame Allokationen bei 60fps eliminiert:
  - bombGlowPaint → pooled _bombGlowPaint (nur Color-Update)
  - outerGlowPaint → pooled _outerGlowPaint (nur Color-Update)
  - fusePath → pooled _fusePath mit Reset() (war auch Memory Leak - fehlte using!)
  - gradientShader → gecacht _hudGradientShader (nur bei ScreenWidth-Aenderung neu)
  - linePaint → pooled _hudLinePaint
  - glowFilter → pooled _hudGlowFilter
- Alle pooled Objekte in Dispose() aufgeraeumt

### Desktop Gameplay Fixes (06.02.2026)
- **DPI Rendering Fix**: GameView.OnPaintSurface nutzt canvas.LocalClipBounds statt e.Info.Width/Height
  - e.Info liefert physische Pixel (DPI-skaliert), Canvas zeichnet in logischen Pixeln
  - Bei 150% DPI wurde das Spielfeld fuer 1920x1080 berechnet, aber nur 1280x720 war sichtbar
- **Touch-Koordinaten Fix**: Proportionale Skalierung basierend auf Render- vs Control-Bounds
  - Funktioniert korrekt unabhaengig davon ob Canvas DPI-Transform hat oder nicht
- **Keyboard Input**: KeyboardHandler erstellt (Arrow/WASD, Space=Bomb, E=Detonate)
  - InputType.Keyboard hinzugefuegt, in InputManager registriert
  - Auto-Detect Desktop: Keyboard als Default wenn nicht Android
  - GameView.axaml: Focusable="True", KeyDown/KeyUp Events
  - GameViewModel: OnKeyDown/OnKeyUp forwarding, Escape=Pause Toggle
  - GameEngine: OnKeyDown/OnKeyUp forwarding an InputManager
- **NullSoundService**: ISoundService-Implementierung erstellt und in DI registriert (fixte InvalidOperationException)
- **CalculateViewport Guard**: Schutz gegen ungueltige Dimensionen (0 oder negativ)
- **Render-Loop Fix**: DispatcherTimer direkt in GameView.axaml.cs (InvalidateSurface alle 16ms), Start/Stop via IsVisible
  - InvalidateVisual() feuert NICHT PaintSurface bei SKCanvasView → InvalidateSurface() verwenden
  - GameViewModel: Render-driven Update (OnPaintSurface macht Update + Render, kein eigener Timer mehr)
- **Level-Rushing Fix**: _levelCompleteHandled Guard in UpdateLevelComplete
  - OnLevelComplete feuerte jeden Frame nach LEVEL_COMPLETE_DELAY → dutzende parallele NextLevelAsync-Aufrufe
  - Flag wird in CompleteLevel() zurueckgesetzt, in UpdateLevelComplete() nach erstem Invoke gesetzt
- **Pause-Button entfernt**: Pause nur noch via Escape-Taste (PauseButton aus GameView.axaml entfernt)
- **Window-Level Keyboard**: MainWindow.axaml.cs leitet KeyDown/KeyUp an GameViewModel weiter (zuverlaessiger Focus)

## AppChecker Fixes (07.02.2026)
- **HighScoreService.cs + ProgressService.cs**: 2x Debug.WriteLine entfernt, catch (Exception ex)→catch (Exception)
- **AndroidManifest.xml**: `android:roundIcon="@mipmap/appicon_round"` ergaenzt
- **MainViewModel**: `ILocalizationService localization` als Constructor-Parameter, `LanguageChanged += (_, _) => MenuVm.OnAppearing()` abonniert

## Performance-Fixes (07.02.2026)

### AStar.cs - Object Pooling
- PriorityQueue, HashSet, Dictionaries als Klassenfelder mit .Clear() statt neue Instanzen pro FindPath()-Aufruf
- BFS visited/queue als Klassenfelder mit .Clear() statt neue Instanzen pro FindSafeCell()-Aufruf
- Directions als static readonly Array statt new[] pro GetNeighbors()-Aufruf
- Node Klasse durch (int X, int Y) value tuple ersetzt (kein Heap)
- GetNeighbors() fuellt gepoolte _neighbors Liste statt IEnumerable<Node> mit yield return

### EnemyAI.cs - DangerZone + LINQ Fixes
- _dangerZone HashSet als Klassenfeld mit .Clear() statt neue Instanz pro CalculateDangerZone()
- _validDirections Liste als Klassenfeld fuer GetRandomValidDirection() und GetRandomSafeDirection()
- LINQ .Where().ToList() durch manuelle Loops mit gepoolter Liste ersetzt

### GameEngine.cs - Overlay SKPaint/SKFont Caching
- 5 gecachte Felder: _overlayBgPaint, _overlayTextPaint, _overlayFont, _overlayGlowFilter, _overlayGlowFilterLarge
- Alle 4 Overlay-Methoden nutzen gecachte Objekte (nur Color/Size aendern statt neue Instanzen)
- Dispose() gibt alle gecachten Objekte frei
- _powerUps.ToList() durch Rueckwaerts-Iteration ersetzt (for i = Count-1)

### GameRenderer.cs - HUD String-Allokationen
- _activePowers Liste als Klassenfeld mit .Clear() statt neue Liste pro Frame
- INV-Timer-String wird gecacht (_lastInvTimerValue/_lastInvString), nur bei Wertaenderung neu erstellt

### GameViewModel.cs - DateTime.Now -> Stopwatch
- System.Diagnostics.Stopwatch statt DateTime.Now fuer Frame-Timing (praeziser, weniger Allokationen)
- _frameStopwatch.Restart() in StartGameLoop/Restart
- deltaTime = _frameStopwatch.Elapsed.TotalSeconds + Restart() in OnPaintSurface

## Neon Visual Fixes (07.02.2026)

### Neon Palette Brightened
- FloorBase (20,22,30)→(30,34,48), FloorAlt (18,20,28)→(26,30,42), FloorLine alpha 30→50
- WallBase (35,40,55)→(50,58,80), WallEdge alpha 180→200
- BlockBase (50,45,40)→(70,60,50), BlockMortar alpha 100→170, BlockHighlight alpha 60→100, BlockShadow brightened
- Neon blocks: 3D edge effect (highlight top/left, shadow bottom/right), thicker glow-cracks (1→1.5), diagonal crack

### HUD Text Glow Fix
- _hudTextGlow: `SKBlurStyle.Outer` (glow only outside text, text stays crisp)
- Previously used `_smallGlow` with `SKBlurStyle.Normal` which blurred the text itself (TIME, SCORE unreadable)
- Applied to 4 HUD locations: TIME value, SCORE value, BOMBS/FIRE values, PowerUp labels

### B/F Labels → Mini Icons
- RenderMiniBomb(): Circle body + gloss highlight + fuse line + spark dot
- RenderMiniFlame(): Outer orange flame (QuadTo curves) + inner yellow flame
- Both used in HUD instead of "B"/"F" text labels

## Coins-Economy + Shop (07.02.2026)

### Neue Features
- **Coin-Waehrung**: Score→Coins (1:1 bei Level-Complete, 0.5x bei Game Over), persistent via IPreferencesService
- **Shop mit 6 Upgrades**: StartBombs (3x), StartFire (3x), StartSpeed (1x), ExtraLives (2x), ScoreMultiplier (3x), TimeBonus (1x)
- **Rewarded Ads**: Coins verdoppeln (GameOver), Weitermachen (Story, 1x pro Versuch) - Desktop: Simulator
- **World-Gating**: 5 Welten a 10 Level, Stern-Anforderungen: 0/10/25/45/70 Sterne
- **UI**: Shop-Button im Hauptmenue, Coin-Badges ueberall, Welt-Header in LevelSelect

### Upgrade-Preise
| Upgrade | Max | Preise |
|---------|-----|--------|
| StartBombs | 3 | 3.000 / 8.000 / 20.000 |
| StartFire | 3 | 3.000 / 8.000 / 20.000 |
| StartSpeed | 1 | 5.000 |
| ExtraLives | 2 | 15.000 / 40.000 |
| ScoreMultiplier | 3 | 10.000 / 30.000 / 75.000 |
| TimeBonus | 1 | 12.000 |

### Neue Dateien (11)
- Models: UpgradeType.cs, PlayerUpgrades.cs, ShopDisplayItem.cs
- Services: ICoinService+CoinService, IShopService+ShopService, IRewardedAdService+RewardedAdService
- ViewModels: ShopViewModel.cs
- Views: ShopView.axaml(.cs)

### Geaenderte Dateien (10)
- GameEngine.cs: IShopService, ApplyUpgrades, ContinueAfterGameOver, Score-Multiplikator, OnCoinsEarned
- Player.cs: MaxBombs/FireRange public set
- IProgressService.cs + ProgressService.cs: World-Gating (GetWorldStarsRequired, GetWorldForLevel)
- GameOverViewModel.cs: Coins, Verdoppeln, Weitermachen
- GameViewModel.cs: Continue-Mode, Coin-Events
- LevelSelectViewModel.cs: World-Header, Coin-Badge
- MainMenuViewModel.cs: Coin-Badge, Shop-Button
- MainViewModel.cs: ShopVm, IsShopActive, erweiterte GameOver/Game Params
- App.axaml.cs: ICoinService, IShopService, IRewardedAdService, ShopViewModel DI
- 24 neue Lokalisierungs-Keys in 6 Sprachen

## Rewarded Ads Integration (07.02.2026)

### Zentrale IRewardedAdService (Phase 1)
- Lokale `IRewardedAdService.cs` + `RewardedAdService.cs` **GELOESCHT** → nutzt zentrale Premium Library (`MeineApps.Core.Premium.Ava`)
- App.axaml.cs: `RewardedAdServiceFactory` Property fuer Android-Override, DI-Registrierung

### Power-Up Boost (Level >= 20)
- **LevelSelectViewModel**: Boost-Overlay vor Level-Start (ab Level 20), AcceptBoostCommand/DeclineBoostCommand
- **LevelSelectView.axaml**: Boost-Overlay UI (Beschreibung + Akzeptieren/Ablehnen Buttons)
- **GameEngine**: `ApplyBoostPowerUp()` - gibt alle 3 PowerUps (BombUp, FireUp, SpeedUp) zum Start

### Level-Skip (3x Game Over)
- **GameOverViewModel**: `CanSkipLevel` (nach 3 Versuchen am selben Level), `SkipLevelAsync` Command
- **GameOverView.axaml**: Skip-Level Button UI
- **IProgressService + ProgressService**: `GetFailCount(level)` / `IncrementFailCount(level)` / `ResetFailCount(level)`

### Android Integration (Phase 6)
- **BomberBlast.Android.csproj**: Linked `RewardedAdHelper.cs` + `AndroidRewardedAdService.cs`
- **MainActivity.cs**: RewardedAdHelper Lifecycle (init, load, dispose)

### Lokalisierung
- 6 neue resx-Keys in 6 Sprachen: PowerUpBoost, PowerUpBoostDesc, WithoutBoost, SkipLevel, SkipLevelDesc, SkipLevelInfo

## Score-Verdopplung nach Level-Complete (07.02.2026)
- **GameViewModel**: IRewardedAdService + IPurchaseService injiziert, Score-Double-Overlay (ShowScoreDoubleOverlay, LevelCompleteScore, CanDoubleScore)
- **GameViewModel**: DoubleScoreCommand (ShowAdAsync("score_double")), SkipDoubleScoreCommand, ProceedToNextLevel()
- **GameEngine**: DoubleScore() Methode (verdoppelt _player.Score, feuert OnScoreChanged + OnCoinsEarned)
- **GameView.axaml**: Score-Double-Overlay (ZIndex=50) mit Score-Anzeige, Video-Button, Weiter-Button
- **HandleLevelComplete**: Zeigt Overlay statt direkt NextLevel (nur fuer Free User mit verfuegbarer Ad)
- **Placement-Strings**: Alle ShowAdAsync()-Aufrufe mit spezifischen Placements versehen:
  - GameOverVM DoubleCoins/ContinueGame: "continue"
  - GameOverVM SkipLevel: "level_skip"
  - LevelSelectVM AcceptBoost: "power_up"
  - GameVM DoubleScore: "score_double"
- **Lokalisierung**: 4 neue Keys (ScoreDoubleTitle, ScoreDoubleDesc, WatchVideoDouble, ContinueWithout) in 6 Sprachen + Designer.cs

## Status
- Build: All 3 projects compile successfully (0 errors, 0 warnings)
- Ported from: BomberBlast MAUI v1.2.0

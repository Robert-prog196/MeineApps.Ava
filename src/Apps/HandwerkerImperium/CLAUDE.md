# HandwerkerImperium - Avalonia Port

## Architektur

```
HandwerkerImperium/
├── HandwerkerImperium.Shared/     # net10.0 (Shared code)
│   ├── App.axaml/.cs              # DI configuration
│   ├── Models/                    # 13 model files
│   │   ├── Enums/                 # WorkshopType, MiniGameType, MiniGameResult, OrderDifficulty, AchievementCategory
│   │   ├── Events/                # GameEvents (all event args)
│   │   ├── GameState.cs           # Main game state
│   │   ├── Workshop.cs            # Workshop model
│   │   ├── Worker.cs              # Worker model
│   │   ├── Order.cs               # Order/contract model
│   │   ├── DailyReward.cs         # Daily reward config
│   │   ├── Achievement.cs         # Achievement definitions
│   │   └── TutorialStep.cs        # Tutorial step definitions
│   ├── Services/                  # 22 files (11 interfaces + 11 implementations)
│   │   ├── Interfaces/            # All service interfaces
│   │   ├── GameStateService.cs    # Central game state management
│   │   ├── SaveGameService.cs     # JSON save/load
│   │   ├── GameLoopService.cs     # Main game tick loop
│   │   ├── AchievementService.cs  # Achievement tracking
│   │   ├── AudioService.cs        # Sound effects (stub)
│   │   ├── DailyRewardService.cs  # Daily login rewards
│   │   ├── OfflineProgressService.cs # Offline earnings
│   │   ├── OrderGeneratorService.cs  # Order generation
│   │   ├── PrestigeService.cs     # Prestige/rebirth system
│   │   ├── RewardedAdService.cs   # Ad rewards (simulated on desktop)
│   │   └── TutorialService.cs     # Tutorial system
│   ├── ViewModels/                # 11 ViewModels
│   │   ├── MainViewModel.cs       # Tab navigation, game init
│   │   ├── AchievementsViewModel.cs
│   │   ├── OrderViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   ├── ShopViewModel.cs
│   │   ├── StatisticsViewModel.cs
│   │   ├── WorkshopViewModel.cs
│   │   ├── SawingGameViewModel.cs
│   │   ├── PipePuzzleViewModel.cs
│   │   ├── WiringGameViewModel.cs
│   │   └── PaintingGameViewModel.cs
│   ├── Views/                     # 13 Views (axaml + cs)
│   │   ├── MainWindow.axaml/.cs
│   │   ├── MainView.axaml/.cs     # Tab container
│   │   ├── DashboardView.axaml/.cs
│   │   ├── WorkshopView.axaml/.cs
│   │   ├── OrderView.axaml/.cs
│   │   ├── ShopView.axaml/.cs
│   │   ├── SettingsView.axaml/.cs
│   │   ├── StatisticsView.axaml/.cs
│   │   ├── AchievementsView.axaml/.cs
│   │   └── MiniGames/
│   │       ├── SawingGameView.axaml/.cs
│   │       ├── PipePuzzleView.axaml/.cs
│   │       ├── WiringGameView.axaml/.cs
│   │       └── PaintingGameView.axaml/.cs
│   ├── Converters/                # 7 converters
│   ├── Helpers/                   # Icons, MoneyFormatter, AnimationHelper, AsyncExtensions
│   └── Resources/Strings/        # 6 languages + Designer.cs
├── HandwerkerImperium.Desktop/    # net10.0 (Windows/Linux)
└── HandwerkerImperium.Android/    # net10.0-android
```

## Features
- Idle Tycoon game with 6 workshop types
- 4 mini-games (Sawing, Pipe Puzzle, Wiring, Painting)
- Order/contract system with difficulty scaling
- Prestige system with permanent income multipliers
- Achievement system
- Daily rewards
- Offline earnings
- Tutorial system
- JSON save/load to LocalApplicationData
- Premium (remove ads + extended offline)
- 6 languages (DE, EN, ES, FR, IT, PT)

## Dependencies
- MeineApps.Core.Ava (themes, localization, preferences)
- MeineApps.Core.Premium.Ava (ads, purchases)
- MeineApps.UI (shared styles/controls)
- CommunityToolkit.Mvvm
- Avalonia 11.3.x

## Build
```bash
dotnet build HandwerkerImperium.Shared/HandwerkerImperium.Shared.csproj
dotnet build HandwerkerImperium.Desktop/HandwerkerImperium.Desktop.csproj
dotnet build HandwerkerImperium.Android/HandwerkerImperium.Android.csproj
```

## Migration Notes
- MAUI Shell navigation replaced with tab-based state management in MainViewModel
- All MAUI-specific APIs replaced with Avalonia equivalents
- DispatcherTimer from Avalonia.Threading (not System.Timers)
- IDisposable pattern for ViewModels with timers (prevents SIGSEGV)
- MiniGames Views in separate namespace `HandwerkerImperium.Views.MiniGames`
- Save game uses `Environment.GetFolderPath(LocalApplicationData)` instead of MAUI FileSystem
- Audio service is a stub (needs platform-specific implementation)

## DataContext Architecture
- **MainViewModel** holds 10 child VMs as properties (injected via constructor)
- **MainView.axaml** sets `DataContext="{Binding ShopViewModel}"` etc. on each sub-view
- **IsVisible bindings** use `#ContentPanel.((vm:MainViewModel)DataContext).IsXxxActive` to resolve against MainViewModel
- **NavigationRequested** events from child VMs are handled by `OnChildNavigation(route)` in MainViewModel
- All VMs are registered as **Singleton** in DI (MainViewModel holds references)
- DashboardView is the only sub-view that directly uses MainViewModel as DataContext

## Deep Code Review Fixes (06.02.2026)

### Models
- **Workshop.cs**: Integer division fix `(Level-1)/2` → `(Level-1)/2.0` in BaseIncomePerWorker
- **Worker.cs**: GUID no longer regenerated on deserialization (empty default, set only in CreateRandom()), HiredAt no longer initialized to UtcNow on deserialize, international names added

### Services
- **SaveGameService**: Atomic write (temp file + rename), backup file (.bak), SemaphoreSlim for thread-safe I/O, corruption recovery from backup, all Debug.WriteLine removed
- **GameStateService**: Full thread safety (locks on AddXp, TryUpgradeWorkshop, TryHireWorker, StartOrder, RecordMiniGameResult, CompleteActiveOrder, CancelActiveOrder, Reset), removed unused _isDirty field
- **DailyRewardService**: Switched from UTC to local time for daily boundary (IsRewardAvailable, WasStreakBroken, TimeUntilNextReward)
- **AchievementService**: Added IDisposable with proper event unsubscription

### ViewModels
- **MainViewModel**: Dialog overlay state properties + commands (LevelUp, OfflineEarnings, DailyReward, AchievementUnlocked), removed Debug.WriteLine/System.Diagnostics
- **SawingGameViewModel**: ILocalizationService injected, all hardcoded German strings localized (UpdateGameTypeVisuals), _isEnding race condition guard
- **PipePuzzleViewModel**: _isEnding race condition guard in EndGameAsync + OnTimerTick
- **WiringGameViewModel**: _isEnding race condition guard in EndGameAsync + OnTimerTick
- **PaintingGameViewModel**: _isEnding race condition guard in EndGameAsync + OnTimerTick

### Views
- **MainView.axaml**: 4 dialog overlays (OfflineEarnings, LevelUp, DailyReward, AchievementUnlocked) with Material Design styling

### Helpers
- **Icons.cs**: Fixed Roofer duplicate (was same as Home), fixed CurrencyEur duplicate (was same as ChartBar)
- **AsyncExtensions**: Switched from Debug.WriteLine to Trace.WriteLine (works in Release builds), added StackTrace to log
- **MoneyFormatter**: Fixed inconsistent threshold in FormatCompact (10_000 → 1_000)

### Localization (all 6 languages)
- 20+ new keys: PlaneNow, LayNow, MeasureNow, StopInGreenZone, StopForSmoothSurface, StopAtPerfectMoment, StopAtRightLength, LevelUpTitle, plus previous batch (ResetGameTitle, etc.)

## Game Loop & Income Fix (06.02.2026)
- **CRITICAL: Game Loop never started** - `_gameLoopService.Start()` was never called in `MainViewModel.Initialize()` → 0€/s passive income
- **CRITICAL: No starting worker** - `GameState.CreateNew()` created Carpenter with 0 workers → income was 0 even if loop ran
- **CRITICAL: No auto-save** - `SaveAsync()` was only called on prestige/import → progress lost on app close
- **Fix: Game Loop started** in `MainViewModel.Initialize()` after state load
- **Fix: Starting worker** added to Carpenter in `GameState.CreateNew()`
- **Fix: Auto-save** every 30 seconds in `GameLoopService.OnTimerTick()`
- **Fix: Save on pause/stop** in `GameLoopService.Pause()` and `Stop()`
- **Fix: Window lifecycle** in `MainWindow.axaml.cs` - Activated→Resume, Deactivated→Pause, Closing→Dispose
- **Fix: Dispose stops game loop** in `MainViewModel.Dispose()`
- **Fix: Workshop card icons** - Removed broken Opacity binding (made unlocked workshops invisible), Panel→Grid for icon overlay

## Order System Fix (06.02.2026)
- **CRITICAL: Multi-task orders broken** - `OnChildNavigation` used `route.StartsWith("..")` which caught ALL routes starting with "..", including relative routes like `"../minigame/sawing?orderId=X"` for next task navigation → navigated to dashboard instead of next mini-game, order stuck as active, all buttons greyed out
- **Fix: OnChildNavigation refactored** - Relative routes `"../minigame/..."` are now stripped of `"../"` prefix and re-routed correctly; pure back-navigation only matches exact `".."` or `"../.."`
- **Fix: Minigame route handling consolidated** - Eliminated duplicated switch statements for minigame navigation into `NavigateToMiniGame()` helper; both `orderId` and `difficulty` query params handled in single code path
- **Fix: OrderCompleted event subscribed** - MainViewModel now listens to `_gameStateService.OrderCompleted` to reset `HasActiveOrder` and replenish orders when running low
- **Fix: Double StartOrder removed** - `OrderViewModel.StartOrderAsync()` no longer calls `_gameStateService.StartOrder()` (already called by `MainViewModel.StartOrderAsync`)

## MiniGame Visual Fixes (06.02.2026)

### PipePuzzle
- **CRITICAL: Puzzle was UNSOLVABLE** - `CheckIfSolved()` passed `Direction.Right` but should be `Direction.Left` → water enters source tile from Left, not Right → TracePath always failed → puzzle could never be solved, always "time up"
- **Fix: Tile rendering replaced** - Unicode box-drawing characters (┃, ┏, ┣, ╋) replaced with colored Border segments (HasTopOpening, HasBottomOpening, HasLeftOpening, HasRightOpening) → works reliably on all platforms, no RotateTransform binding issues
- **Fix: OnRotationChanged notifies opening properties** - When tile is rotated, computed opening properties fire PropertyChanged so border segments update
- **Fix: WrapPanel width constraint** - Added `PuzzleGridWidth` computed property (GridSize * 64) bound to ItemsControl Width → tiles wrap correctly into grid

### PaintingGame
- **Fix: PaintCell visual updates** - `DisplayColor` and `IsPaintedCorrectly` are computed properties that didn't fire PropertyChanged when `IsPainted` changed → painted cells never showed background color or checkmark. Added `OnIsPaintedChanged` and `OnPaintColorChanged` partial methods
- **Fix: WrapPanel width constraint** - Added `PaintGridWidth` computed property (GridSize * 54) bound to ItemsControl Width

### WiringGame
- **Fix: Visual feedback for wire states** - Added `BackgroundColor` (green tint=connected, white highlight=selected, red tint=error), `ContentOpacity` (dimmed when connected), `BorderWidth` (thicker when selected) properties to Wire model
- **Fix: View updated** - Left/Right wire templates show background color, opacity, border width changes + checkmark icon overlay when connected
- **Fix: PropertyChanged notifications** - `OnIsSelectedChanged`, `OnIsConnectedChanged`, `OnHasErrorChanged` notify visual computed properties

## Version
- v2.0.0 (vc5) - Avalonia port

# BomberBlast - Avalonia Port

## Overview
Bomberman-clone game with SkiaSharp rendering, AI pathfinding, and multiple input methods.
Landscape-only on Android. Ported from .NET MAUI to Avalonia UI.

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
│   │   ├── GameRenderer.cs      # Full game renderer + HUD
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
│   │   ├── Grid/                # Cell, CellType, GameGrid (11x9)
│   │   └── Levels/              # Level, LevelGenerator (50 levels + arcade)
│   ├── Services/
│   │   ├── ISoundService.cs     # Sound abstraction
│   │   ├── IProgressService.cs  # Game progress persistence
│   │   ├── ProgressService.cs
│   │   ├── IHighScoreService.cs
│   │   └── HighScoreService.cs
│   ├── ViewModels/              # 9 ViewModels (MVVM)
│   │   ├── MainViewModel.cs     # Navigation/view switching
│   │   ├── GameViewModel.cs     # 60fps game loop (DispatcherTimer)
│   │   ├── MainMenuViewModel.cs
│   │   ├── LevelSelectViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   ├── HighScoresViewModel.cs
│   │   ├── GameOverViewModel.cs
│   │   ├── PauseViewModel.cs
│   │   └── HelpViewModel.cs
│   ├── Views/                   # 9 Avalonia views
│   │   ├── MainWindow.axaml(.cs)
│   │   ├── MainView.axaml(.cs)
│   │   ├── MainMenuView.axaml(.cs)
│   │   ├── GameView.axaml(.cs)  # SKCanvasView for SkiaSharp
│   │   ├── LevelSelectView.axaml(.cs)
│   │   ├── SettingsView.axaml(.cs)
│   │   ├── HighScoresView.axaml(.cs)
│   │   ├── GameOverView.axaml(.cs)
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

## Status
- Build: All 3 projects compile successfully (0 errors, 0 warnings)
- Ported from: BomberBlast MAUI v1.2.0

# HandwerkerImperium (Avalonia)

> Fuer Build-Befehle, Conventions und Troubleshooting siehe [Haupt-CLAUDE.md](../../../CLAUDE.md)

## App-Beschreibung

Idle-Game: Baue dein Handwerker-Imperium auf, stelle Mitarbeiter ein, kaufe Werkzeuge, erforsche Upgrades, schalte neue Workshop-Typen frei. Verdiene Geld durch automatische Auftraege oder spiele Mini-Games.

**Version:** 2.0.3 | **Package-ID:** com.meineapps.handwerkerimperium | **Status:** Geschlossener Test

## Haupt-Features

- **6 Workshop-Typen** (Tischlerei, Malerei, Sanitaer, Elektrik, Landschaftsbau, Renovierung)
- **3 Mini-Games** (Pipe Puzzle, Wire Match, Tile Swap)
- **Worker-System** mit 3 Tiers (Auszubildender, Geselle, Meister)
- **Goldschrauben-Economy** (Premium-Waehrung fuer Boosts/Unlock)
- **Research Tree** (16 Upgrades in 4 Kategorien)
- **Daily Challenges** (3 pro Tag)
- **Achievements** (33 Erfolge, inkl. Workshop-Level 100/250/500/1000, Player-Level 100/250/500/1000, Worker-Tiers SS/SSS/Legendary, 10B EUR)
- **Statistiken** (Gesamt-Verdienst, Workshop-Verteilung)

## Premium & Ads

### Premium-Modell
- **Preis**: 4,99 EUR (Lifetime)
- **Vorteile**: +50% Einkommen, +100% Goldschrauben aus Mini-Games, keine Werbung

### Rewarded (9 Placements)
1. `golden_screws` → 5 Goldschrauben (Dashboard)
2. `score_double` → Mini-Game Score verdoppeln
3. `market_refresh` → Arbeitermarkt-Pool neu wuerfeln
4. `workshop_speedup` → 2h Produktionsertrag sofort (WorkshopView)
5. `workshop_unlock` → Workshop ohne Level freischalten (Dashboard)
6. `worker_hire_bonus` → +1 Worker-Slot persistent (WorkerMarket)
7. `research_speedup` → Forschung sofort fertig gratis (ResearchView)
8. `daily_challenge_retry` → Challenge-Fortschritt zuruecksetzen (Dashboard)
9. `achievement_boost` → Achievement Progress +20% (AchievementsView)

## Architektur-Besonderheiten

### Game Loop
- **GameLoopService** (60 FPS Timer) → Workshop-Produktion, Worker-Animationen, Mini-Game-State
- **AutoSaveService** (alle 5 Min) → GameState → SQLite

### Workshop-Typen
Enum: `Carpentry`, `Painting`, `Plumbing`, `Electrical`, `Landscaping`, `Renovation`
Jeder Typ hat: `BaseIncome`, `BaseUpgradeCost`, `UnlockLevel`, `UnlockCost`

### Worker-System
10 Tiers via Enum: `F` (0.5x), `E` (0.75x), `D` (1.0x), `C` (1.3x), `B` (1.7x), `A` (2.2x), `S` (3.0x), `SS` (4.5x), `SSS` (7.0x), `Legendary` (12.0x)
`HireWorker()` → kostet Geld, erhoeht Workshop-Effizienz
Tier-Farben: F=Grau, E=Gruen, D=#0E7490(Teal), C=#B45309(DarkOrange), B=Amber, A=Rot, S=Gold, SS=Lila, SSS=Cyan, Legendary=Rainbow-Gradient

### Goldschrauben-Quellen
1. Mini-Games (3-10 Schrauben pro Win)
2. Daily Challenges (20 Schrauben)
3. Achievements (5-50 Schrauben)
4. Rewarded Ad (5 Schrauben)
5. IAP-Paket (100/500/2000 Schrauben)

### Research Tree
16 Upgrades in 4 Kategorien (Efficiency, Automation, Workers, Special)
Jedes Research braucht: `GoldScrews` + `ResearchPoints` (verdient via Workshop-Produktion)

### Mini-Games
- **Pipe Puzzle**: Rohre drehen um Durchfluss zu schaffen (3 Schwierigkeiten)
- **Wire Match**: Kabel-Farben verbinden (Simon Says mit Timing)
- **Tile Swap**: 3x3 Tile-Puzzle (Sliding Puzzle)

## App-spezifische Services

| Service | Zweck |
|---------|-------|
| `GameLoopService` | 60 FPS Update-Loop |
| `AutoSaveService` | Alle 5 Min GameState → SQLite |
| `DailyChallengeService` | 3 Challenges/Tag generieren (00:00 Reset) |
| `AchievementService` | 33 Erfolge tracken + Goldschrauben-Rewards |
| `WorkshopColorConverter` | Enum → Brush Mapping (warme Palette, keine kalten Farben) |

## Game Juice

| Feature | Implementierung |
|---------|-----------------|
| Workshop Cards | Farbiges BorderBrush nach Typ |
| Worker Avatars | 3 Tier-Icons (Apprentice=Hat, Journeyman=Hammer, Master=Crown) |
| Golden Screw Icon | Gold-Shimmer Animation (CSS scale+rotate Loop) |
| Level-Up | CelebrationOverlay mit Confetti (100 Partikel, 2s) |
| Income | FloatingTextOverlay (gruen, +100px, 1.5s) |
| Button Hover | Pulse Effect (scale 1.05) |

## Farbkonsistenz (Craft-Palette)

- **Alle Buttons** (Primary/Secondary/Outlined) ueberschrieben via App.axaml Style-Overrides → immer Craft-Orange/Braun
- **Keine `{DynamicResource PrimaryBrush}`** in Views → alles durch `{StaticResource CraftPrimaryBrush/LightBrush}` ersetzt
- **Workshop-Farben**: Carpenter=#A0522D, Plumber=#0E7490(Teal), Electrician=#F97316(Orange), Painter=#EC4899, Roofer=#DC2626, Contractor=#EA580C, Architect=#78716C(Stone), GeneralContractor=#FFD700
- **Tier-Farben**: F=Grau, E=Gruen, D=#0E7490(Teal), C=#B45309(DarkOrange), B=Amber, A=Rot, S=Gold
- **Branch-Farben**: Tools=#EA580C, Management=#92400E(Braun), Marketing=#65A30D(Lime)

## Daily Challenge Tracking

- `MiniGameResultRecorded` Event auf `IGameStateService` → `DailyChallengeService` subscribt automatisch
- Jedes MiniGame-Ergebnis trackt `PlayMiniGames` + `AchieveMinigameScore` Challenges
- Score-Mapping: Perfect=100%, Good=75%, Ok=50%, Miss=0%

## Changelog Highlights

- **v2.0.3 (12.02.2026)**: Statistik-Refresh beim Tab-Wechsel. Shop Instant-Cash: Belohnungen basieren auf stündlichem Einkommen statt fixem Level*Multiplikator (small=1h, large=4h, huge=12h, mega=48h). QuickJob-Belohnungen: ~5min Einkommen statt Cap bei 100€. Daily-Challenge-Belohnungen: ~10min Einkommen, 5 Level-Tiers (0-5/6-15/16-30/31-50/51+), bis zu 3 Goldschrauben. QuickJob-Timer Auto-Rotation im GameTick (UI aktualisiert bei Ablauf). Workshop-Auswahl-Dialog: Größere Buttons (MinHeight=56), sichtbarer Hintergrund+Rahmen. Research-ProgressBar: Background=SurfaceBrush (Fortschritt sichtbar).
- **v2.0.3 (12.02.2026)**: UI-Verbesserungen + Bugfixes: SawingGame IsResultShown-Reset in SetOrderId (Ergebnis-Screen blieb stehen). ToggleSwitch On/Off-Text entfernt. ConfirmDialog Buttons untereinander statt nebeneinander. Dialog-Overlay-Klick schließt Dialoge. WorkerProfile Training/Rest-Dauer-Anzeige (TrainingTimeDisplay, TrainingCostDisplay, RestTimeDisplay). Auto-Rest bei 100% Erschöpfung (WorkerService). 3 neue Lokalisierungs-Keys (TrainingDuration, TrainingCost, RestDuration) in 6 Sprachen.
- **v2.0.3 (12.02.2026)**: WorkshopView Kosten-Anzeige: Redundante TotalWorkerIncomeDisplay entfernt, stattdessen Income Stats Card mit Brutto/Netto/Kosten-Aufschlüsselung (Miete, Material, Löhne). Netto rot bei Verlust. MoneyFormatter.FormatPerHour() hinzugefügt. 6 neue Lokalisierungs-Keys (GrossIncome, NetIncome, RunningCosts, Rent, MaterialCosts, Wages) in 6 Sprachen.
- **v2.0.3 (12.02.2026)**: Achievement-XP-Rebalancing: Frühe/einfache Achievements XP um 50-75% reduziert (first_order 25→10, building_first 100→25, etc.). Level-Achievements geben 0 XP (Feedback-Loop verhindert: Level→XP→Level→XP), stattdessen höhere Geld+Goldschrauben-Belohnungen. Endgame-Achievements XP ebenfalls ~50% reduziert. Gesamtes XP-Tempo deutlich verlangsamt.
- **v2.0.3 (12.02.2026)**: Mini-Game Start-Button Fix: Panel-Wrapper mit !IsResultShown versteckt Button nach Spielende (alle 4 Games). Workshop-Gesamteinkommen: TotalWorkerIncomeDisplay in WorkshopView unter Worker-Liste. XP bei Workshop-Upgrades (5 + Level/10 pro Upgrade). Workshop-Freischaltung komplett überarbeitet: Level-Requirement muss erfüllt sein (kein Bypass per Video), Video-Ad gibt nur 50% Rabatt auf Kosten, TryPurchaseWorkshop/CanPurchaseWorkshop statt ForceUnlockWorkshop. Unlock-Kosten 5x erhöht (Plumber 25K, Electrician 250K, Painter 2.5M, etc.). Dashboard zeigt Kosten + offenes Schloss bei kaufbaren Workshops. 10 neue Lokalisierungs-Keys (6 Sprachen).
- **v2.0.3 (12.02.2026)**: WorkerProfileView Grid.Column="1" Fix (Text-Überlappung bei Persönlichkeit/Werkstatt/Effizienz). Workshop-Upgrade-Performance: RefreshWorkshops() nur für betroffenen Workshop statt alle (RefreshSingleWorkshop). Effizienz-Anzeige: EffectiveEfficiency + IncomeContribution (€/s) pro Worker in WorkshopView, WorkerMarket + WorkerProfile. Ad-Extra-Slot-Button auf WorkshopView (CanWatchSlotAd). Dashboard: Orders-Count-Badge, automatische Order-Regenerierung bei Rückkehr. OrdersCompleted Label lokalisiert (6 Sprachen).
- **v2.0.3 (11.02.2026)**: Worker-Tiers erweitert auf 10 (F/E/D/C/B/A/S/SS/SSS/Legendary). 9 neue Achievements (Workshop 100/250/500/1000, Level 100/250/500/1000, Worker SS/SSS/Legendary, 10B EUR). 2 neue Shop-Items (Huge/Mega Instant Cash). Lokalisierung in 6 Sprachen (33 neue Keys in AppStrings.resx/.de/.es/.fr/.it/.pt + AppStrings.Designer.cs)
- **v2.0.3 (11.02.2026)**: Order-Rewards: Doppel-Multiplikator-Bug gefixt (Difficulty wurde in BaseReward UND FinalReward angewendet → quadriert), Basis von 100 auf 300 erhöht für spürbar höhere Auftragsbelohnungen. Worker-Tiles in WorkshopView klickbar → navigiert zum Worker-Profil (Fire/Transfer/Train). Dashboard ScrollViewer HorizontalScrollBarVisibility=Disabled (rechter Rand Fix)
- **v2.0.3 (11.02.2026)**: Workshop-Karten im Dashboard auf 2-Spalten-Layout umgestellt (UniformGrid Columns=2). Vertikale Karten mit farbigem Header (Workshop-Farbe 10% Opacity), Icon+Name+Level-Badge, Worker/Income-Stats, Level-ProgressBar in Workshop-Farbe, Upgrade-Button über volle Breite mit abgerundeten unteren Ecken (CornerRadius=16)
- **v2.0.3 (11.02.2026)**: Bugfixes: Workshop-LevelProgress dividierte durch 10 statt MaxLevel (50), XP-Progress in OnXpGained nutzte falsche Formel (jetzt GameState.LevelProgress), DailyChallenge EarnMoney int-Cast→Math.Round, WorkshopView zeigte "/10" statt "/50", WorkshopDisplayModel hardcodierte 50.0→Workshop.MaxLevel
- **v2.0.3 (11.02.2026)**: 6 neue Rewarded-Ad-Placements implementiert (workshop_speedup, workshop_unlock, worker_hire_bonus, research_speedup, daily_challenge_retry, achievement_boost). Models erweitert (HasRetriedWithAd, HasUsedAdBoost, AdBonusWorkerSlots). Service-Methoden: RetryChallenge, ForceUnlockWorkshop, BoostAchievement. Lokalisierung in 6 Sprachen.
- **v2.0.2 (09.02.2026)**: Daily-Challenge-Bug: MiniGame-Ergebnisse werden jetzt via Event an DailyChallengeService gemeldet; 18 fehlende Lokalisierungs-Keys in 6 Sprachen ergaenzt; Farbkonsistenz-Fix: Alle Views auf warme Craft-Palette, Button-Style-Overrides, Workshop/Tier/Branch-Farben waermer
- **v2.0.2 (08.02.2026)**: Banner-Ad Overlap-Fix, WorkshopColorConverter, CelebrationOverlay + FloatingTextOverlay, Golden Screw Shimmer
- **v2.0.1 (07.02.2026)**: Rewarded Ads 7 Placements, Premium-Modell Sync (4,99)
- **v2.0.0 (05.02.2026)**: Initial Avalonia Migration, Research Tree + Mini-Games, Worker-System mit 3 Tiers

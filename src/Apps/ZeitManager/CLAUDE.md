# ZeitManager (Avalonia) - Timer, Stoppuhr, Wecker

## Status
**Version:** 2.0.0 | **Status:** In Entwicklung | **Preis:** Kostenlos (werbefrei)

## Features
- **Timer:** Mehrere gleichzeitig, Quick-Timer (1/5/10/15/30 min), Create-Dialog, Snooze
- **Stoppuhr:** Rundenzeiten, Undo-Funktion, Centisecond-Precision
- **Wecker:** CRUD, Weekday-Toggles, Challenge-Support (Math/Shake), Tonauswahl, Snooze
- **Schichtplan:** 15-Schicht (3 Gruppen Mo-Fr) + 21-Schicht (5 Gruppen 24/7), Kalender-Ansicht
- **Fullscreen Alarm-Overlay:** Pulsier-Animation, Dismiss + Snooze Buttons
- **Audio:** 6 eingebaute Toene, Vorhoer-Funktion, Timer-Sound in Settings konfigurierbar
- **Notifications:** Desktop (Windows Toast / Linux notify-send), Android (Foreground Service + AlarmManager)
- 4 Themes (Midnight/Aurora/Daylight/Forest), 6 Sprachen
- Material Icons, Empty States, Dialog Overlays
- **Game Juice:** FloatingTextOverlay (Stoppuhr-Runden, Timer fertig) + CelebrationOverlay (Confetti bei Timer-Ende)

## Architektur

### Projekt-Struktur
```
ZeitManager/
├── ZeitManager.Shared/       # RootNamespace=ZeitManager
│   ├── Models/               # 14 Dateien (Timer, Alarm, Shift, Challenge, SoundItem, Enums)
│   ├── ViewModels/           # Main, Timer, Stopwatch, Alarm, Settings, AlarmOverlay, ShiftSchedule
│   ├── Views/                # MainView + 4 Tab-Views + AlarmOverlayView + ShiftScheduleView
│   ├── Services/             # Database, Timer, Audio, AlarmScheduler, ShiftSchedule, Notification
│   ├── Resources/Strings/    # 6 Sprachen (.resx) + Designer.cs
│   ├── App.axaml / App.axaml.cs  # DI Setup
│   └── MainWindow.axaml/.cs
├── ZeitManager.Desktop/      # Program.cs
└── ZeitManager.Android/      # MainActivity.cs + Services (Foreground, Alarm, Boot, Notification)
```

### DI-Services (App.axaml.cs)
- `IPreferencesService` → PreferencesService("ZeitManager")
- `IThemeService` → ThemeService
- `ILocalizationService` → LocalizationService(AppStrings.ResourceManager)
- `IDatabaseService` → DatabaseService (SQLite: zeitmanager.db3, SemaphoreSlim-gesichert)
- `ITimerService` → TimerService (In-Memory Timer Management + Snooze)
- `IAudioService` → AudioService (6 Toene, Console.Beep / BEL Fallback)
- `IAlarmSchedulerService` → AlarmSchedulerService (30s Check-Timer, Weekday-Matching, Double-Trigger-Schutz)
- `IShiftScheduleService` → ShiftScheduleService (15/21-Schicht Berechnung)
- `INotificationService` → Plattform-spezifisch via `ConfigurePlatformServices` (Android: AndroidNotificationService, Desktop: DesktopNotificationService)
- ViewModels: MainVM, TimerVM, StopwatchVM, AlarmVM, SettingsVM, AlarmOverlayVM, ShiftScheduleVM (alle Transient)

### Tab-Navigation
MainView nutzt `Border.TabContent` + `.Active` CSS-Klassen (wie RechnerPlus):
- Tab 0: Timer (TimerSand/TimerSandEmpty)
- Tab 1: Stopwatch (Timer/Stop)
- Tab 2: Alarm (Alarm/AlarmOff) - mit Toggle: Wecker | Schichtplan
- Tab 3: Settings (Cog/CogOutline) - mit Timer-Sound Auswahl

### View-Patterns
- **Timer:** Quick-Timer Chips → Timer-Liste → FAB → Dialog Overlay
- **Stopwatch:** Circular Ring Display → Lap/Reset/Start Buttons → Lap-Liste
- **Alarm:** Toggle-Bar (Wecker|Schichtplan) → Alarm-Liste mit Toggle + Tonauswahl → Editor Overlay
- **ShiftSchedule:** Kalender-Grid (WrapPanel 46x46), Monats-Navigation, Legende, Editor Overlay
- **Settings:** Theme 2x2 Grid → Language WrapPanel → Timer Sound → About → Feedback/Privacy
- **AlarmOverlay:** Fullscreen Dark Overlay, pulsierender Kreis, Dismiss + Snooze Buttons

### Wichtige Patterns
- **UI-Thread:** System.Timers.Timer feuert auf ThreadPool → `Dispatcher.UIThread.Post()` fuer Property-Updates
- **PropertyChanged:** Abgeleitete Properties (RemainingTimeFormatted, IsRunning) muessen explizit notifiziert werden
- **Theme-Brushes:** `TextPrimaryBrush` (NICHT `TextBrush`), `TextMutedBrush`, `PrimaryBrush`, `PrimaryContrastBrush`
- **Delete-Bestätigung:** Alle Loeschaktionen (Timer, Alarm, Schichtplan) haben Confirmation-Overlay (IsDeleteConfirmVisible Pattern)
- **MessageRequested:** Alle ViewModels haben `EventHandler<string> MessageRequested` → MainVM abonniert + zeigt Snackbar (3s auto-dismiss)
- **x:DataType:** Alle DataTemplates haben Compiled Bindings (x:DataType auf DataTemplate)
- **Touch-Targets:** Alle Buttons mindestens 48x48px (MinWidth/MinHeight)
- **AlarmItem:** Erbt ObservableObject, IsEnabled nutzt SetProperty fuer UI-Notification
- **Plattform-DI:** `App.ConfigurePlatformServices` static Action fuer plattform-spezifische Services
- **DateOnly.Parse:** Mit CultureInfo.InvariantCulture (ShiftSchedule, ShiftException)
- **Stopwatch Undo:** TimeSpan _offset Pattern (Stopwatch supports keine direkte Elapsed-Zuweisung)
- **Thread-Safety:** TimerService nutzt `lock(_lock)` fuer _timers List, AudioService lock-swap fuer CTS, DesktopNotificationService ConcurrentDictionary
- **Snackbar:** MainView Overlay (Border.TabContent.Active), MainViewModel ShowSnackbar() mit CancellationTokenSource
- **ProgressPercent:** Math.Clamp(0, 100) gegen negative/ueberlaufende Werte
- **CustomShiftPattern:** ShortName() nutzt LocalizationManager.GetString() fuer lokalisierte Schicht-Kuerzel
- **AlarmItem.NotifyLocalizationChanged():** Public Methode fuer Sprachwechsel-Benachrichtigung (RepeatDaysFormatted)
- **Game Juice:** FloatingTextOverlay + CelebrationOverlay in MainView.axaml (MeineApps.UI.Controls), Events via MainViewModel.FloatingTextRequested/CelebrationRequested, StopwatchVM.FloatingTextRequested wird weitergeleitet

### Android-Services
```
ZeitManager.Android/Services/
├── TimerForegroundService.cs    # Foreground Service mit Notification (Timer-Countdown)
├── AlarmReceiver.cs             # BroadcastReceiver fuer Wecker-Ausloesung
├── BootReceiver.cs              # BOOT_COMPLETED → Wecker neu planen
└── AndroidNotificationService.cs # NotificationChannels + AlarmManager
```

**AndroidManifest Permissions:** FOREGROUND_SERVICE, SCHEDULE_EXACT_ALARM, RECEIVE_BOOT_COMPLETED, POST_NOTIFICATIONS, VIBRATE, USE_FULL_SCREEN_INTENT, WAKE_LOCK

### Lokalisierung
- ResourceManager-basiert via ILocalizationService.GetString()
- AppStrings.Designer.cs: Manuell erstellt (nicht auto-generiert bei CLI-Build)
- Neue Avalonia-Keys: ThemeMidnight, ThemeAurora, ThemeDaylight, ThemeForest, FeedbackButton, PrivacyPolicy
- Alle View-Strings lokalisiert (keine hardcodierten Texte in AXAML/Models)
- AlarmItem.RepeatDaysFormatted nutzt LocalizationManager.GetString() (statischer Zugriff in Model)
- Neue Keys: ConfirmDeleteTitle/Message, ConfirmDeactivateTitle/Message, ShiftEarlyShort/LateShort/NightShort, TimerDone

## Abhängigkeiten
- MeineApps.Core.Ava (Themes, Localization, Preferences)
- MeineApps.UI (Shared UI Components)
- sqlite-net-pcl + SQLitePCLRaw.bundle_green
- CommunityToolkit.Mvvm

**Kein MeineApps.Core.Premium - komplett werbefrei!**

## Bekannte Warnungen
- Android Build: 11x CA1416 (NotificationChannel APIs require API 26+, min SDK=24) - erwartet, Code hat `Build.VERSION.SdkInt` Guard

## Changelog
- **v2.0.0** - Avalonia Migration (Phase 3)
- **v2.0.0-bugfix** - Timer-Bug gefixt (PropertyChanged + UI-Thread), Theme-Farben korrigiert
- **v2.0.0-features** - Audio-Service, Snooze, Tonauswahl, Fullscreen Overlay, Schichtplan, Notifications, Android Services
- **v2.0.0-review** - Umfassender Code-Review + Fixes:
  - DatabaseService: SemaphoreSlim Double-Checked Locking (Race Condition)
  - StopwatchVM: Dispatcher.UIThread.Post + Undo mit _offset Pattern
  - App.axaml.cs: ConfigurePlatformServices fuer plattform-spezifische DI (DesktopNotification nicht auf Android)
  - AlarmSchedulerService: Double-Trigger-Schutz (_triggeredToday HashSet)
  - AlarmItem: ObservableObject + SetProperty(IsEnabled) + lokalisierte RepeatDaysFormatted
  - BootReceiver: Startet jetzt MainActivity (war no-op)
  - AlarmReceiver: SetFullScreenIntent mit echtem PendingIntent (war null)
  - Alle VMs: Delete-Bestätigung Overlays, MessageRequested Events
  - Alle Views: x:DataType auf DataTemplates, 48px Touch-Targets, lokalisierte Strings
  - ShiftSchedule/Exception: DateOnly.Parse mit CultureInfo.InvariantCulture
  - ShiftScheduleService: Redundanter Ternary-Fix
  - 7 neue resx-Keys in 6 Sprachen
- **v2.0.0-review2** - Tiefgehender Code-Review Round 2:
  - Android: Runtime Permission-Checks (POST_NOTIFICATIONS API 33+, SCHEDULE_EXACT_ALARM API 31+)
  - Android: AlarmReceiver FullScreenIntent API 12+ Guard (Fallback ContentIntent)
  - Android: TimerForegroundService StartCommandResult.NotSticky (kein Restart mit leerer Notification)
  - Android: BootReceiver try-catch + NoAnimation Flag
  - Android: AndroidNotificationService Math.Abs() fuer Notification-IDs, UTC Epoch korrekt
  - MainViewModel: Snackbar-System (SnackbarMessage, IsSnackbarVisible, 3s auto-dismiss)
  - MainViewModel: MessageRequested von allen Child-VMs verdrahtet (Timer, Alarm, Settings, ShiftSchedule)
  - MainView: Snackbar Overlay zwischen Content und AlarmOverlay
  - TimerService: Thread-safe _timers mit lock(_lock), Event-Unsubscribe in Dispose
  - AudioService: Lock-swap Pattern fuer CTS, using var process in PlaySoundLinux
  - DesktopNotificationService: ConcurrentDictionary, DateTime.UtcNow, EscapeXml/EscapeShell, using var process
  - TimerItem: ProgressPercent mit Math.Clamp(0, 100)
  - CustomShiftPattern: ShortName() lokalisiert (ShiftEarlyShort/LateShort/NightShort)
  - AlarmItem: NotifyLocalizationChanged() fuer RepeatDaysFormatted bei Sprachwechsel
  - AlarmViewModel: OnLanguageChanged ruft NotifyLocalizationChanged() auf allen Alarmen
  - Build: Desktop 0 Fehler/0 Warnungen, Android 0 Fehler/11 CA1416 (erwartet)
- **v2.0.0-gamejuice** - Game Juice Overlays:
  - FloatingTextOverlay + CelebrationOverlay in MainView eingebaut (MeineApps.UI.Controls)
  - StopwatchViewModel: FloatingTextRequested Event bei Lap (zeigt "#N" als info)
  - MainViewModel: FloatingTextRequested + CelebrationRequested Events, Timer-Ende zeigt "TimerDone" + Confetti
  - MainView.axaml.cs: Event-Handler fuer Floating Text + Celebration
  - Neuer resx-Key "TimerDone" in 6 Sprachen (EN: Done!, DE: Fertig!, ES: Listo!, FR: Fini!, IT: Fatto!, PT: Pronto!)

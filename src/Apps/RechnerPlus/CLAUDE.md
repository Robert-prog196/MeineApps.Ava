# RechnerPlus Avalonia

## Übersicht
Scientific Calculator mit Unit Converter - Avalonia Version

**Version:** 2.0.0
**Package:** org.rsdigital.rechnerplus
**Status:** Phase 2 abgeschlossen - Build erfolgreich

## Features
- Taschenrechner (Basic + Scientific Mode)
- Berechnungsverlauf (History Bottom-Sheet, Swipe-Up/Down, Session-basiert)
- Einheiten-Konverter (8 Kategorien, offset-basierte Temperature-Konvertierung)
- 4 Themes (Midnight, Aurora, Daylight, Forest) - dynamisches Switching
- 6 Sprachen (DE, EN, ES, FR, IT, PT) - vollständig lokalisiert
- Keyboard-Support (Desktop): Ziffern, Operatoren, Enter, Backspace, Escape
- Android + Windows + Linux

## Struktur

```
RechnerPlus/
├── RechnerPlus.Shared/             # Shared Code (net10.0)
│   ├── App.axaml/.cs               # DI Setup, Theme/Localization Init
│   ├── RechnerPlus.Shared.csproj   # RootNamespace=RechnerPlus
│   ├── ViewModels/
│   │   ├── MainViewModel.cs        # Tab-Navigation, lokalisierte Labels
│   │   ├── CalculatorViewModel.cs  # Basic+Scientific, Memory, History, lokalisierte Errors
│   │   ├── ConverterViewModel.cs   # 8 Kategorien, Temp-Offset-Fix, InvariantCulture
│   │   └── SettingsViewModel.cs    # Theme/Language Selection, Privacy/Feedback
│   ├── Views/
│   │   ├── MainWindow.axaml        # Desktop Window (400x700, min 360x500)
│   │   ├── MainView.axaml          # Tab-Content, Fade-Transition, History Bottom-Sheet
│   │   ├── CalculatorView.axaml    # Button Grid + Scientific Panel + Memory Row
│   │   ├── CalculatorView.axaml.cs # KeyDown Handler für Desktop Keyboard-Input
│   │   ├── ConverterView.axaml     # Category+Unit ComboBoxes, FAB Swap, Result
│   │   ├── MainView.axaml.cs       # Swipe-Gesten (Up=ShowHistory, Down=HideHistory)
│   │   └── SettingsView.axaml      # Theme Preview (neutrale Hintergründe, farbiger Rahmen), Language, About
│   └── Resources/Strings/
│       ├── AppStrings.resx         # EN (Base) - 80+ Keys
│       ├── AppStrings.de.resx      # DE
│       ├── AppStrings.es.resx      # ES
│       ├── AppStrings.fr.resx      # FR
│       ├── AppStrings.it.resx      # IT
│       └── AppStrings.pt.resx      # PT
├── RechnerPlus.Android/            # Android-spezifisch
│   ├── MainActivity.cs
│   ├── AndroidManifest.xml
│   └── Resources/
└── RechnerPlus.Desktop/            # Desktop (Win/Linux)
    └── Program.cs
```

## Build

```bash
# Desktop Debug
dotnet run --project src/Apps/RechnerPlus/RechnerPlus.Desktop

# Desktop Release
dotnet publish src/Apps/RechnerPlus/RechnerPlus.Desktop -c Release -r win-x64

# Android Debug
dotnet build src/Apps/RechnerPlus/RechnerPlus.Android

# Android Release (AAB)
dotnet publish src/Apps/RechnerPlus/RechnerPlus.Android -c Release
```

## Abhängigkeiten
- MeineApps.CalcLib (Calculator Engine + ExpressionParser)
- MeineApps.Core.Ava (Themes, Services, Converters, Behaviors)
- MeineApps.UI (Cards, FAB, Button/Text/Input Styles)

## Architektur-Hinweise

### Theme-Switching
- **KEIN** statisches Theme in App.axaml (nur ThemeColors.axaml für shared tokens)
- ThemeService lädt Theme dynamisch via `app.Styles.Add(StyleInclude)`
- ThemeService setzt `RequestedThemeVariant` (Dark/Light) basierend auf IsDarkTheme

### Tab-Navigation (MainView)
- Panel mit 3 Borders (.TabContent), jeweils ein Child-View
- Active-Tab via CSS-Klasse: `Classes.Active="{Binding IsXxxActive}"`
- Fade-Transition: DoubleTransition auf Opacity (150ms)
- **WICHTIG**: Border wrapping nötig, da DataContext der Child-Views eigener ViewModel ist

### Temperature-Konvertierung
- Offset-basierte Formel: `baseValue = value * ToBase + Offset`
- Celsius: ToBase=1, Offset=0 (Referenz)
- Fahrenheit: ToBase=5/9, Offset=-32*5/9
- Kelvin: ToBase=1, Offset=-273.15

### History (CalculatorViewModel + MainView)
- IHistoryService (aus CalcLib) als Singleton in DI registriert
- CalculatorViewModel: IsHistoryVisible, HistoryEntries, Show/Hide/Clear/SelectHistoryEntry Commands
- MainView: Bottom-Sheet Overlay mit Backdrop, Slide-Animation (TransformOperationsTransition)
- Swipe-Gesten in MainView.axaml.cs: Up=ShowHistory, Down=HideHistory (nur im Calculator-Tab)
- Session-basiert, max 100 Eintraege

### Calculator Buttons
- CalcButton CSS-Klasse: MinHeight=52, CornerRadius=12, Stretch-Fill
- Digit/Operator FontSize=24 (statt 20)

### Theme-Preview (SettingsView)
- Jeder Theme-Button hat hardcoded neutralen Hintergrund (passend zum Theme)
- Ausgewaehlter Button: farbiger Border (Primary-Farbe) + CheckCircle-Icon
- Nicht-ausgewaehlter Button: subtiler Border
- Swatch-Kreise (20px) immer gut sichtbar auf neutralem Hintergrund

### Keyboard (CalculatorView.axaml.cs)
- KeyDown-Handler auf dem UserControl (Focusable=true)
- Shift+8 = Multiplikation (*), OemPlus ohne Shift = Equals
- OemComma und OemPeriod = Dezimalpunkt

## Converter Kategorien
1. Length (m, km, mi, ft, in, cm, mm, yd)
2. Mass (kg, g, lb, oz, mg, t)
3. Temperature (°C, °F, K) - offset-basiert
4. Time (s, min, h, d, wk)
5. Volume (L, mL, gal, qt, pt, fl oz, cup)
6. Area (m², km², ha, ft², in², ac, yd²)
7. Speed (m/s, km/h, mph, kn)
8. Data (B, KB, MB, GB, TB)

## Deep Review Fixes (06.02.2026)
- **FormatResult lokalisiert**: CalculatorVM + ConverterVM - "Error" String durch `_localization.GetString("Error")` ersetzt (non-static)
- **SelectHistoryEntry**: ClearError() hinzugefuegt - HasError-Flag wird beim Waehlen eines History-Eintrags zurueckgesetzt
- **Tan() Validation**: Math.Tan()-Ergebnis > 1e15 wird als undefiniert erkannt (z.B. tan(90°) → Fehler statt riesiger Zahl)
- **NullRef Fix**: MainView.OnHistoryBackdropTapped - `vm?.CalculatorViewModel?.HideHistoryCommand` (zweites null-conditional)
- **Operator-Normalisierung**: Buttons + Keyboard senden jetzt einheitlich Unicode-Operatoren (×, ÷, −) fuer konsistente Expression-Anzeige
- **km/h Precision**: ConverterVM Speed-Faktor von 0.277778 auf `1.0 / 3.6` (exakter)

## Unterschiede zur MAUI-Version
- Avalonia statt MAUI
- Keine Werbung (kostenlose App)
- Desktop-Support (Windows + Linux)
- Moderneres Design mit neuen Themes
- Keyboard-Support auf Desktop
- Temperature-Konvertierung korrekt (Offset-Fix)

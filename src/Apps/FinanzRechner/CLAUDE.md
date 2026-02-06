# FinanzRechner (Avalonia) - CLAUDE.md

## Projektstruktur

```
FinanzRechner/
├── FinanzRechner.Shared/      # Shared Code (net10.0)
│   ├── Models/
│   │   ├── Expense.cs          # Expense, ExpenseCategory, TransactionType enums
│   │   ├── Budget.cs           # Budget, BudgetStatus record, BudgetAlertLevel
│   │   ├── RecurringTransaction.cs  # RecurringTransaction, RecurrencePattern
│   │   └── FinanceEngine.cs    # Zinseszins, Sparplan, Kredit, Tilgung, Rendite
│   ├── Services/
│   │   ├── IExpenseService.cs / ExpenseService.cs  # SQLite CRUD
│   │   ├── IExportService.cs / ExportService.cs    # CSV + PDF Export
│   │   └── INotificationService.cs / NotificationService.cs
│   ├── Helpers/
│   │   └── CategoryLocalizationHelper.cs
│   ├── Converters/             # 14 Avalonia IValueConverters
│   ├── ViewModels/
│   │   ├── MainViewModel.cs    # 4 Tabs (Home, Tracker, Stats, Settings)
│   │   ├── ExpenseTrackerViewModel.cs  # CRUD, Filter, Sort, Undo-Delete
│   │   ├── StatisticsViewModel.cs      # Charts (LiveCharts), Export
│   │   ├── SettingsViewModel.cs        # Theme, Language, Backup/Restore
│   │   ├── BudgetsViewModel.cs         # Budget Limits CRUD
│   │   ├── RecurringTransactionsViewModel.cs  # Recurring CRUD
│   │   └── Calculators/        # 5 Finanz-Rechner VMs
│   ├── Views/
│   │   ├── MainWindow.axaml    # Desktop Window Shell
│   │   ├── MainView.axaml      # Tab Navigation mit Fade-Transitions
│   │   ├── HomeView.axaml      # Dashboard
│   │   ├── ExpenseTrackerView.axaml  # Transaktionsliste + Add/Edit Dialog
│   │   ├── StatisticsView.axaml      # Charts + Export
│   │   ├── SettingsView.axaml        # Einstellungen
│   │   ├── BudgetsView.axaml         # Budget-Verwaltung
│   │   ├── RecurringTransactionsView.axaml  # Dauerauftraege
│   │   └── Calculators/        # 5 Rechner-Views
│   ├── Resources/
│   │   └── Strings/AppStrings.resx + 5 Uebersetzungen
│   ├── App.axaml / App.axaml.cs  # DI, Theme, MaterialIconStyles
│   └── FinanzRechner.Shared.csproj
│
├── FinanzRechner.Desktop/      # Desktop App (net10.0)
│   ├── Program.cs
│   └── FinanzRechner.Desktop.csproj
│
└── FinanzRechner.Android/      # Android App (net10.0-android)
    ├── MainActivity.cs
    └── FinanzRechner.Android.csproj
```

## Build

```bash
# Desktop
dotnet build src/Apps/FinanzRechner/FinanzRechner.Desktop/FinanzRechner.Desktop.csproj

# Android
dotnet build src/Apps/FinanzRechner/FinanzRechner.Android/FinanzRechner.Android.csproj
```

## Abhaengigkeiten

- MeineApps.Core.Ava (Themes, Services, Converters)
- MeineApps.Core.Premium.Ava (Ads, IAP)
- MeineApps.UI (Shared UI Components)
- LiveChartsCore.SkiaSharpView.Avalonia (Charts)
- PdfSharpCore (PDF Export - bringt SixLabors.ImageSharp 1.0.4 mit)

## Bekannte Patterns

### Command-Binding in DataTemplates
Commands auf dem ViewModel innerhalb von DataTemplates muessen mit Ancestor-Binding aufgeloest werden:
```xml
Command="{Binding $parent[UserControl].((vm:MyViewModel)DataContext).MyCommand}"
CommandParameter="{Binding}"
```

### Avalonia API-Unterschiede (vs. MAUI)
- `Application.TryGetResource()` statt `TryFindResource()`
- `Grid` hat kein `Padding` - stattdessen `Margin` verwenden
- `Button.Content` statt `Button.Text`
- `ItemsControl` statt `ItemsRepeater`
- `[RelayCommand] async Task FooAsync()` generiert `FooCommand` (ohne Async-Suffix)

## Status

- Desktop Build: 0 Fehler (NuGet-Warnungen zu SixLabors.ImageSharp ignorierbar)
- Android Build: nicht getestet
- UI-Test: ausstehend

## Bugfixes (05.02.2026 - Abend)

### SettingsView - Language/Theme Buttons + Lokalisierung
- Language-Buttons hatten KEINE Command-Bindings (nur Border mit Cursor="Hand" und TODO-Kommentare)
- Theme-Buttons gleiches Problem - keine Klick-Interaktion
- Alle Texte waren hardcoded English statt lokalisiert
- **Fix:** Komplette SettingsView.axaml umgeschrieben:
  - Language: WrapPanel mit Button-Paaren (Primary/Outlined) + SelectLanguageCommand
  - Themes: 4 Panel mit Button-Paaren (Selected/Unselected) + SelectThemeCommand + Farbvorschau (wie RechnerPlus)
  - Alle Texte via Binding zu lokalisierten Properties im SettingsViewModel

### SettingsViewModel - Lokalisierte Text-Properties
- 20+ neue Properties: SettingsTitleText, ChooseDesignText, LanguageText, PremiumText, BackupRestoreText, ThemeMidnightName, etc.
- UpdateLanguageProperties() aktualisiert alle Properties bei Sprachwechsel
- AppVersion korrigiert: v2.0.0 (statt v1.0.0)

### ExpenseTrackerView - Edit/Delete Buttons + Kategorie-Auswahl
- Transaktions-Items hatten KEINE Edit/Delete Buttons
- **Fix:** Edit (Pencil) + Delete Buttons mit Ancestor-Binding zu EditExpenseCommand/DeleteExpenseCommand
- Kategorie-Chips im Add/Edit Dialog hatten KEIN SelectCategory Command
- **Fix:** Panel mit Selected/Unselected Button-Paaren + SelectCategoryCommand via Ancestor-Binding

### MainViewModel - LanguageChanged Subscription
- NavTexts (Home, Tracker, Statistics, Settings) aktualisierten sich NICHT bei Sprachwechsel
- **Fix:** SettingsViewModel.LanguageChanged Event abonniert -> UpdateNavTexts()

### Vollstaendige Lokalisierung aller Views (05.02.2026 - Nacht)
- **Alle 10 Views** vollstaendig lokalisiert (0 hardcoded Strings mehr):
  - HomeView, ExpenseTrackerView, StatisticsView, BudgetsView, RecurringTransactionsView
  - CompoundInterestView, SavingsPlanView, LoanView, AmortizationView, YieldView
- **Alle 10 ViewModels** mit lokalisierten Text-Properties erweitert:
  - MainViewModel: 14 HomeView-Properties + UpdateHomeTexts()
  - ExpenseTrackerViewModel: 28 Properties + UpdateLocalizedTexts()
  - StatisticsViewModel: 20 Properties + UpdateLocalizedTexts()
  - BudgetsViewModel: 16 Properties
  - RecurringTransactionsViewModel: 18 Properties
  - 5 Calculator VMs: je 11-15 Properties + ILocalizationService injection
- **Sprachwechsel-Propagation:** MainVM.OnLanguageChanged() -> UpdateNavTexts() + UpdateHomeTexts() + ExpenseTrackerVM.UpdateLocalizedTexts() + StatisticsVM.UpdateLocalizedTexts()
- **Pattern in DataTemplates:** Ancestor-Binding `{Binding $parent[UserControl].((vm:MyVM)DataContext).PropText}`
- Build: 0 Fehler

### Export mit File-Dialog + Feedback (06.02.2026)
- Export speicherte bisher stumm in LocalApplicationData ohne Nutzer-Feedback
- **Neuer Service:** IFileDialogService / FileDialogService (Avalonia StorageProvider.SaveFilePickerAsync)
- **IExportService:** Alle 3 Methoden haben jetzt optionalen `targetPath` Parameter
- **ExportService:** Nutzt targetPath wenn angegeben, sonst Fallback auf Default-Pfad
- **ExpenseTrackerViewModel:** FileDialog vor CSV-Export, StatusMessage-Toast (4s auto-hide)
- **StatisticsViewModel:** FileDialog vor PDF-Export, StatusMessage-Toast (4s auto-hide)
- **Views:** Export-Status-Toast am unteren Rand (CheckCircle Icon + Nachricht)
- **DI:** IFileDialogService in App.axaml.cs registriert
- Build: 0 Fehler

### Calculator Views Redesign (06.02.2026)
- **Alle 5 Views** (CompoundInterest, SavingsPlan, Loan, Amortization, Yield) komplett neu gestaltet
- **Farbiges Header-Banner** mit abgerundeten Ecken, semi-transparentem Back-Button, Icon + Titel (Farbe je Rechner: Income/Info/Warning/Secondary/Expense)
- **Input-Felder** mit farbigen Material Icons in Labels, Watermark-Texte
- **Highlight-Ergebnis** als grosse farbige Box (28px FontSize, weiss auf Themenfarbe)
- **Sekundaere Ergebnisse** mit Trennlinien und farbcodierten Werten
- **Action-Buttons** mit Icons (Refresh + Calculator), 48px Hoehe, CornerRadius 12
- **Bug gefixt**: `Classes="CardElevated"` -> `Classes="Card Elevated"`
- **Navigation**: MainViewModel 5 Calculator-VMs + OpenCommand + GoBackAction
- **MainView**: Calculator-Overlay-Panel (ZIndex=50), HomeView-Karten sind Buttons
- Build: 0 Fehler

### Sort/Filter Lokalisierung + Sub-Page Navigation (06.02.2026)
- **Sort/Filter ComboBoxes:** ItemTemplate mit SortOptionToStringConverter/FilterTypeToStringConverter hinzugefuegt
- **Duplizierte Enums entfernt:** SortOption/FilterTypeOption waren in Converter-Dateien UND ViewModel definiert - Converter nutzen jetzt `using static ExpenseTrackerViewModel`
- **Budgets/Recurring Navigation:** MainViewModel haelt jetzt BudgetsVM + RecurringTransactionsVM per Constructor Injection
- **Sub-Page Overlay:** MainView zeigt BudgetsView/RecurringTransactionsView als Overlay (ZIndex=60) ueber Calculator-Overlay
- **ExpenseTrackerVM.NavigationRequested** wird in MainVM verdrahtet: "BudgetsPage"/"RecurringTransactionsPage" -> CurrentSubPage
- **GoBack aus Sub-Pages:** BudgetsVM/RecurringTransactionsVM NavigationRequested ".." -> CurrentSubPage = null
- Build: 0 Fehler

### Code Review Cleanup (06.02.2026)
Alle Verbesserungen aus anderen Apps (HandwerkerRechner, FitnessRechner, etc.) angewendet:

#### Hardcodierte deutsche Chart-Labels lokalisiert
- **CompoundInterestViewModel**: "Kapital" -> ChartCapital, "Jahre" -> ChartYears
- **SavingsPlanViewModel**: "Einzahlungen" -> ChartDeposits, "Kapital" -> ChartCapital, "Jahre" -> ChartYears
- **LoanViewModel**: "Tilgung" -> ChartRepayment, "Zinsen" -> ChartInterest
- **AmortizationViewModel**: "Restschuld" -> ChartRemainingDebt, "Jahre"/"Monate" -> ChartYears/ChartMonths
- **YieldViewModel**: "Anfangswert" -> ChartInitialValue, "Endwert" -> ChartFinalValue
- 10 neue Lokalisierungs-Keys in 6 Sprachen (ChartCapital, ChartDeposits, ChartRepayment, ChartInterest, ChartRemainingDebt, ChartInitialValue, ChartFinalValue, ChartYears, ChartMonths, FilteredCountFormat)

#### Tab-Wechsel schliesst Overlay
- **MainViewModel.OnSelectedTabChanged**: CloseCalculator() + CurrentSubPage=null bei Tab-Wechsel

#### Sonstige Fixes
- **ExpenseTrackerViewModel**: Hardcodiertes "von" durch FilteredCountFormat ersetzt, dead code GetCategoryName/GetCategoryIcon entfernt
- **Alle Calculator VMs + ExpenseTrackerVM**: "EUR" durch Unicode-Euro-Zeichen ersetzt
- Build: 0 Fehler

### Deep Code Review (06.02.2026)
Tiefgehende Pruefung aller Dateien mit 5 parallelen Review-Agents:

#### Debug.WriteLine -> MessageRequested Events
- **Alle 6 ViewModels** (Main, ExpenseTracker, Statistics, Settings, Budgets, RecurringTransactions): `Debug.WriteLine` durch `MessageRequested?.Invoke()` ersetzt
- **ExpenseService**: 4 interne Debug.WriteLine entfernt (Error-Handling bleibt)
- **NotificationService**: Debug.WriteLine entfernt (no-op Service)
- **ExpenseTrackerViewModel**: 2 Debug-Marker ("=== ShowBudgets CALLED ===") entfernt
- `public event Action<string, string>? MessageRequested` in allen 6 VMs hinzugefuegt

#### LanguageChanged Propagation
- **MainViewModel.OnLanguageChanged()**: BudgetsVM.UpdateLocalizedTexts() + RecurringTransactionsVM.UpdateLocalizedTexts() hinzugefuegt
- **BudgetsViewModel**: UpdateLocalizedTexts() mit 16 Properties
- **RecurringTransactionsViewModel**: UpdateLocalizedTexts() mit 18 Properties

#### Code Cleanup
- **5 Calculator VMs**: Ungenutztes `using System.Diagnostics` entfernt
- **CategoryLocalizationHelper**: Freelance-Kategorie ES/FR/IT/PT Uebersetzungen hinzugefuegt

#### Build: 0 Fehler

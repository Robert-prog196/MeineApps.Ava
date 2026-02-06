# MeineApps.Core.Premium.Ava - Premium Features Library

## Zweck
Monetarisierungs-Library fuer Avalonia Apps (Ads, IAP, Trial):
- Ad State Management (AdMob)
- In-App Purchases State Management
- 14-Day Trial System
- AdBannerView Control

**Depends on:** MeineApps.Core.Ava (IPreferencesService)

## Struktur
```
MeineApps.Core.Premium.Ava/
├── Services/
│   ├── IAdService.cs          # Ad service interface
│   ├── IPurchaseService.cs    # Purchase service interface
│   ├── ITrialService.cs       # Trial service interface
│   ├── AdConfig.cs            # AdMob IDs for all apps
│   ├── AdMobService.cs        # Ad state management
│   ├── PurchaseService.cs     # Purchase state (virtual methods for platform-specific billing)
│   └── TrialService.cs        # 14-day trial via Preferences
├── Controls/
│   ├── AdBannerView.axaml     # Ad banner placeholder control
│   └── AdBannerView.axaml.cs  # Code-behind with StyledProperties
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI: AddMeineAppsPremium()
```

## Verwendung
```csharp
// App.axaml.cs
services.AddMeineAppsPremium();

// oder mit eigener PurchaseService-Implementierung:
services.AddMeineAppsPremium<AndroidPurchaseService>();
```

## Unterschiede zu MAUI-Version
- Nutzt IPreferencesService statt Preferences.Default
- PurchaseService hat virtual Purchase-Methoden (plattformspezifisch ueberschreibbar)
- AdBannerView ist Avalonia UserControl statt MAUI ContentView
- Kein Plugin.InAppBilling direkt - Billing wird in Android-Projekten implementiert
- DI via IServiceCollection statt MauiAppBuilder

## Apps
| App | Premium | Modell |
|-----|---------|--------|
| RechnerPlus | Nein | Kostenlos |
| ZeitManager | Nein | Kostenlos |
| HandwerkerRechner | Ja | 3,99 EUR remove_ads |
| FinanzRechner | Ja | 3,99 EUR remove_ads |
| FitnessRechner | Ja | 3,99 EUR remove_ads |
| WorkTimePro | Ja | 3,99 EUR/Mo oder 19,99 EUR Lifetime |
| HandwerkerImperium | Ja | 4,99 EUR Premium |
| BomberBlast | Ja | 3,99 EUR remove_ads |

## Product IDs
- `remove_ads` - Legacy non-consumable
- `premium_monthly` - Abo (WorkTimePro)
- `premium_lifetime` - Einmalkauf (WorkTimePro)

**WICHTIG:** RechnerPlus und ZeitManager referenzieren NICHT Core.Premium!

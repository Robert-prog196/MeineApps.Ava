using MeineApps.Core.Ava.Services;
using MeineApps.Core.Premium.Ava.Services;

namespace FitnessRechner.Services;

/// <summary>
/// Begrenzt kostenlose Barcode-Scans auf 3 pro Tag.
/// Rewarded Ad gibt +5 Scans. Premium-Nutzer haben keine Begrenzung.
/// </summary>
public class ScanLimitService : IScanLimitService
{
    private readonly IPurchaseService _purchaseService;
    private readonly IPreferencesService _preferences;

    private const int DailyFreeScans = 3;
    private const int AdBonusScans = 5;
    private const string ScanCountKey = "ScanLimit_Count";
    private const string ScanDateKey = "ScanLimit_Date";

    private int _remainingScans;

    public ScanLimitService(IPurchaseService purchaseService, IPreferencesService preferences)
    {
        _purchaseService = purchaseService;
        _preferences = preferences;
        LoadState();
    }

    public int RemainingScans
    {
        get
        {
            ResetDailyIfNeeded();
            return _remainingScans;
        }
    }

    public bool CanScan
    {
        get
        {
            if (_purchaseService.IsPremium) return true;
            ResetDailyIfNeeded();
            return _remainingScans > 0;
        }
    }

    public void UseOneScan()
    {
        if (_purchaseService.IsPremium) return;

        ResetDailyIfNeeded();

        if (_remainingScans > 0)
        {
            _remainingScans--;
            SaveState();
        }
    }

    public void AddScans(int count)
    {
        ResetDailyIfNeeded();
        _remainingScans += count;
        SaveState();
    }

    /// <summary>
    /// Prueft ob ein neuer Tag begonnen hat und setzt ggf. das Tages-Kontingent zurueck.
    /// </summary>
    private void ResetDailyIfNeeded()
    {
        var savedDate = _preferences.Get(ScanDateKey, "");
        var todayStr = DateTime.Today.ToString("yyyy-MM-dd");

        if (savedDate != todayStr)
        {
            _remainingScans = DailyFreeScans;
            _preferences.Set(ScanDateKey, todayStr);
            _preferences.Set(ScanCountKey, _remainingScans);
        }
    }

    private void LoadState()
    {
        var savedDate = _preferences.Get(ScanDateKey, "");
        var todayStr = DateTime.Today.ToString("yyyy-MM-dd");

        if (savedDate == todayStr)
        {
            _remainingScans = _preferences.Get(ScanCountKey, DailyFreeScans);
        }
        else
        {
            // Neuer Tag oder erster Start
            _remainingScans = DailyFreeScans;
            _preferences.Set(ScanDateKey, todayStr);
            _preferences.Set(ScanCountKey, _remainingScans);
        }
    }

    private void SaveState()
    {
        _preferences.Set(ScanCountKey, _remainingScans);
    }
}

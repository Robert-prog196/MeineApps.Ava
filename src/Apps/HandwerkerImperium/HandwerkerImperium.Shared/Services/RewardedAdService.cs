using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Service for rewarded video ads.
/// In Avalonia/desktop mode, simulates ads for testing.
/// </summary>
public class RewardedAdService : IRewardedAdService
{
    private readonly IGameStateService _gameStateService;
    private bool _isDisabled;
    private bool _isLoaded;
    private bool _isLoading;

    public RewardedAdService(IGameStateService gameStateService)
    {
        _gameStateService = gameStateService;
    }

    public bool IsRewardedAdReady => _isLoaded && !_isDisabled;

    public bool IsAvailable => !_isDisabled && !_gameStateService.State.IsPremium;

    public async Task InitializeAsync()
    {
        if (_isDisabled || _gameStateService.State.IsPremium)
        {
            _isDisabled = true;
            return;
        }

        // In Avalonia/desktop, always simulate
        await Task.Delay(100);
        _isLoaded = true;
    }

    public async Task LoadAdAsync()
    {
        if (_isDisabled || _isLoading) return;

        _isLoading = true;
        _isLoaded = false;

        // Simulate ad loading
        await Task.Delay(500);
        _isLoaded = true;
        _isLoading = false;
    }

    public async Task<bool> ShowAdAsync()
    {
        if (!IsRewardedAdReady)
        {
            await LoadAdAsync();
            if (!_isLoaded) return false;
        }

        // Simulate watching an ad
        await Task.Delay(1500);

        // Reload for next use
        _isLoaded = false;
        _ = LoadAdAsync();

        return true;
    }

    public void Disable()
    {
        _isDisabled = true;
        _isLoaded = false;
    }
}

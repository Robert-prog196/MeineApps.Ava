using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MeineApps.Core.Premium.Ava.Services;

namespace MeineApps.Core.Premium.Ava.Controls;

/// <summary>
/// Cross-platform AdMob Banner View for Avalonia.
/// Shows ads based on Premium status.
/// </summary>
public partial class AdBannerView : UserControl
{
    private IAdService? _adService;
    private IPurchaseService? _purchaseService;
    private Border? _testAdBorder;

    public static readonly StyledProperty<string> AdUnitIdProperty =
        AvaloniaProperty.Register<AdBannerView, string>(nameof(AdUnitId), string.Empty);

    public static readonly StyledProperty<bool> ShowTestAdProperty =
        AvaloniaProperty.Register<AdBannerView, bool>(nameof(ShowTestAd), false);

    public static readonly StyledProperty<string> AppNameProperty =
        AvaloniaProperty.Register<AdBannerView, string>(nameof(AppName), string.Empty);

    /// <summary>
    /// AdMob Unit ID for the banner
    /// </summary>
    public string AdUnitId
    {
        get => GetValue(AdUnitIdProperty);
        set => SetValue(AdUnitIdProperty, value);
    }

    /// <summary>
    /// App name for automatic Ad-Unit-ID selection (Debug/Release)
    /// </summary>
    public string AppName
    {
        get => GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }

    /// <summary>
    /// Shows a test placeholder instead of real ads
    /// </summary>
    public bool ShowTestAd
    {
        get => GetValue(ShowTestAdProperty);
        set => SetValue(ShowTestAdProperty, value);
    }

    public AdBannerView()
    {
        AvaloniaXamlLoader.Load(this);
        _testAdBorder = this.FindControl<Border>("TestAdBorder");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Try to resolve services from the visual tree's service provider
        // Services are typically injected via the app's DI container
        if (_adService == null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            // Services need to be set externally via SetServices() or DI
        }

        UpdateVisibility();
    }

    /// <summary>
    /// Set services from DI container (called during initialization)
    /// </summary>
    public void SetServices(IAdService? adService, IPurchaseService? purchaseService)
    {
        // Unsubscribe from previous
        if (_adService != null)
            _adService.AdsStateChanged -= OnAdsStateChanged;
        if (_purchaseService != null)
            _purchaseService.PremiumStatusChanged -= OnPurchaseStatusChanged;

        _adService = adService;
        _purchaseService = purchaseService;

        if (_adService != null)
            _adService.AdsStateChanged += OnAdsStateChanged;
        if (_purchaseService != null)
            _purchaseService.PremiumStatusChanged += OnPurchaseStatusChanged;

        UpdateVisibility();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowTestAdProperty && _testAdBorder != null)
        {
            _testAdBorder.IsVisible = change.GetNewValue<bool>();
        }
        else if (change.Property == AppNameProperty)
        {
            var appName = change.GetNewValue<string>();
            if (!string.IsNullOrEmpty(appName))
            {
                AdUnitId = AdConfig.GetBannerAdUnitId(appName);
#if DEBUG
                ShowTestAd = true;
#else
                ShowTestAd = false;
#endif
            }
        }
    }

    private void OnAdsStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateVisibility);
    }

    private void OnPurchaseStatusChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(UpdateVisibility);
    }

    private void UpdateVisibility()
    {
        IsVisible = !(_purchaseService?.IsPremium ?? false);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_adService != null)
            _adService.AdsStateChanged -= OnAdsStateChanged;
        if (_purchaseService != null)
            _purchaseService.PremiumStatusChanged -= OnPurchaseStatusChanged;
    }
}

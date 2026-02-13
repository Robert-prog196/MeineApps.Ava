using Avalonia.Controls;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.ViewModels;

namespace HandwerkerImperium.Views;

public partial class WorkshopView : UserControl
{
    public WorkshopView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is WorkshopViewModel vm)
        {
            vm.UpgradeEffectRequested += OnUpgradeEffect;
        }
    }

    private async void OnUpgradeEffect(object? sender, EventArgs e)
    {
        // Level-Badge Scale-Pop Animation
        var badge = this.FindControl<Border>("LevelBadge");
        if (badge != null)
        {
            await AnimationHelper.ScaleUpDownAsync(badge, 1.0, 1.25, TimeSpan.FromMilliseconds(250));
        }
    }
}

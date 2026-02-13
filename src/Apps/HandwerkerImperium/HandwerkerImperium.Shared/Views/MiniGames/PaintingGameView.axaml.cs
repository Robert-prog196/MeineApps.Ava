using Avalonia.Controls;
using HandwerkerImperium.Helpers;
using HandwerkerImperium.ViewModels;

namespace HandwerkerImperium.Views.MiniGames;

public partial class PaintingGameView : UserControl
{
    private PaintingGameViewModel? _vm;

    public PaintingGameView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null)
            _vm.ComboIncreased -= OnComboIncreased;

        _vm = DataContext as PaintingGameViewModel;

        if (_vm != null)
            _vm.ComboIncreased += OnComboIncreased;
    }

    private async void OnComboIncreased(object? sender, EventArgs e)
    {
        var badge = this.FindControl<Border>("ComboBadge");
        if (badge != null)
            await AnimationHelper.ScaleUpDownAsync(badge, 1.0, 1.3, TimeSpan.FromMilliseconds(250));
    }
}

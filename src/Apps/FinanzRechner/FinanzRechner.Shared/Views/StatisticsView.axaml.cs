using Avalonia;
using Avalonia.Controls;
using FinanzRechner.ViewModels;

namespace FinanzRechner.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is StatisticsViewModel vm)
            await vm.LoadStatisticsCommand.ExecuteAsync(null);
    }
}

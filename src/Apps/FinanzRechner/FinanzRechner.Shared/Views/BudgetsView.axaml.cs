using Avalonia;
using Avalonia.Controls;
using FinanzRechner.ViewModels;

namespace FinanzRechner.Views;

public partial class BudgetsView : UserControl
{
    public BudgetsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is BudgetsViewModel vm)
            await vm.LoadBudgetsCommand.ExecuteAsync(null);
    }
}

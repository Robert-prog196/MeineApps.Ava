using Avalonia;
using Avalonia.Controls;
using FinanzRechner.ViewModels;

namespace FinanzRechner.Views;

public partial class RecurringTransactionsView : UserControl
{
    public RecurringTransactionsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is RecurringTransactionsViewModel vm)
            await vm.LoadTransactionsAsync();
    }
}

using Avalonia;
using Avalonia.Controls;
using FinanzRechner.ViewModels;

namespace FinanzRechner.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.OnAppearingAsync();
    }
}

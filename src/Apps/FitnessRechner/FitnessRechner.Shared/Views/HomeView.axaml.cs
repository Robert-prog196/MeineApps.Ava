using Avalonia;
using Avalonia.Controls;
using FitnessRechner.ViewModels;

namespace FitnessRechner.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        try
        {
            if (DataContext is MainViewModel vm)
                await vm.OnAppearingAsync();
        }
        catch (Exception)
        {
            // async void darf keine Exception werfen → App-Crash verhindern
        }
    }
}

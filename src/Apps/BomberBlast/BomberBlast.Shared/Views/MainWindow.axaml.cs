using Avalonia.Controls;
using Avalonia.Input;
using BomberBlast.ViewModels;

namespace BomberBlast.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Handle keyboard input at window level so it always works
        // regardless of which child control has focus.
        KeyDown += OnWindowKeyDown;
        KeyUp += OnWindowKeyUp;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.IsGameActive)
        {
            mainVm.GameVm.OnKeyDown(e.Key);
            e.Handled = true;
        }
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel mainVm && mainVm.IsGameActive)
        {
            mainVm.GameVm.OnKeyUp(e.Key);
            e.Handled = true;
        }
    }
}

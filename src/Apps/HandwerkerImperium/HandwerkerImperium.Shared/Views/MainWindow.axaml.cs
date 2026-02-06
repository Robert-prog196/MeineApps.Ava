using Avalonia.Controls;
using HandwerkerImperium.ViewModels;

namespace HandwerkerImperium.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += OnWindowClosing;
        Activated += OnWindowActivated;
        Deactivated += OnWindowDeactivated;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ResumeGameLoop();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.PauseGameLoop();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Dispose();
    }
}

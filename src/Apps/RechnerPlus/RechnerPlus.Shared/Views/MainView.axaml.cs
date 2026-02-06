using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using RechnerPlus.ViewModels;

namespace RechnerPlus.Views;

public partial class MainView : UserControl
{
    private Point _swipeStart;
    private bool _isSwiping;
    private const double SwipeThreshold = 50;

    public MainView()
    {
        InitializeComponent();

        // Use Tunnel routing to detect swipes even over buttons
        AddHandler(PointerPressedEvent, OnPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _swipeStart = e.GetPosition(this);
        _isSwiping = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSwiping) return;
        _isSwiping = false;

        var vm = DataContext as MainViewModel;
        if (vm == null || !vm.IsCalculatorActive) return;

        var end = e.GetPosition(this);
        var deltaY = end.Y - _swipeStart.Y;

        if (deltaY < -SwipeThreshold)
        {
            // Swipe up -> show history
            vm.CalculatorViewModel.ShowHistoryCommand.Execute(null);
        }
        else if (deltaY > SwipeThreshold && vm.CalculatorViewModel.IsHistoryVisible)
        {
            // Swipe down -> hide history
            vm.CalculatorViewModel.HideHistoryCommand.Execute(null);
        }
    }

    private void OnHistoryBackdropTapped(object? sender, PointerPressedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.CalculatorViewModel?.HideHistoryCommand.Execute(null);
    }
}

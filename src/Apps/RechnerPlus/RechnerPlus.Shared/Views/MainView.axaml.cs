using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using RechnerPlus.ViewModels;

namespace RechnerPlus.Views;

public partial class MainView : UserControl
{
    private Point _swipeStart;
    private bool _isSwiping;
    private const double SwipeThreshold = 50;
    private MainViewModel? _vm;

    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Tunnel-Routing fuer Swipe-Erkennung auch ueber Buttons
        AddHandler(PointerPressedEvent, OnPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes ViewModel abmelden
        if (_vm != null)
            _vm.FloatingTextRequested -= OnFloatingText;

        _vm = DataContext as MainViewModel;

        // Neues ViewModel anmelden
        if (_vm != null)
            _vm.FloatingTextRequested += OnFloatingText;
    }

    private void OnFloatingText(string text, string category)
    {
        var color = category switch
        {
            "result" => Color.Parse("#6366F1"),
            _ => Color.Parse("#6366F1")
        };
        var w = FloatingTextCanvas.Bounds.Width;
        if (w < 10) w = 300;
        var h = FloatingTextCanvas.Bounds.Height;
        if (h < 10) h = 400;
        FloatingTextCanvas.ShowFloatingText(text, w * 0.3, h * 0.3, color, 14);
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
            // Swipe hoch -> Verlauf anzeigen
            vm.CalculatorViewModel.ShowHistoryCommand.Execute(null);
        }
        else if (deltaY > SwipeThreshold && vm.CalculatorViewModel.IsHistoryVisible)
        {
            // Swipe runter -> Verlauf ausblenden
            vm.CalculatorViewModel.HideHistoryCommand.Execute(null);
        }
    }

    private void OnHistoryBackdropTapped(object? sender, PointerPressedEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        vm?.CalculatorViewModel?.HideHistoryCommand.Execute(null);
    }
}

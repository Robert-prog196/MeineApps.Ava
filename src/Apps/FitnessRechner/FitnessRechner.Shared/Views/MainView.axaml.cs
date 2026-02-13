using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FitnessRechner.ViewModels;

namespace FitnessRechner.Views;

public partial class MainView : UserControl
{
    private MainViewModel? _vm;
    private readonly Random _rng = new();

    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Events sauber abmelden bei Entfernung aus dem Visual Tree
        if (_vm != null)
        {
            _vm.FloatingTextRequested -= OnFloatingText;
            _vm.CelebrationRequested -= OnCelebration;
            _vm = null;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Alte Events abmelden
        if (_vm != null)
        {
            _vm.FloatingTextRequested -= OnFloatingText;
            _vm.CelebrationRequested -= OnCelebration;
        }

        _vm = DataContext as MainViewModel;

        // Neue Events abonnieren
        if (_vm != null)
        {
            _vm.FloatingTextRequested += OnFloatingText;
            _vm.CelebrationRequested += OnCelebration;
        }
    }

    private void OnFloatingText(string text, string category)
    {
        var color = category switch
        {
            "success" => Color.Parse("#22C55E"),
            "info" => Color.Parse("#3B82F6"),
            _ => Color.Parse("#3B82F6")
        };
        var w = FloatingTextCanvas.Bounds.Width;
        if (w < 10) w = 300;
        var h = FloatingTextCanvas.Bounds.Height;
        if (h < 10) h = 400;
        FloatingTextCanvas.ShowFloatingText(text, w * (0.2 + _rng.NextDouble() * 0.6), h * 0.35, color, 16);
    }

    private void OnCelebration()
    {
        CelebrationCanvas.ShowConfetti();
    }
}

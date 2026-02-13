using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using HandwerkerImperium.ViewModels;

namespace HandwerkerImperium.Views;

public partial class DashboardView : UserControl
{
    private MainViewModel? _vm;
    private readonly Random _random = new();
    private TranslateTransform? _headerTranslate;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Parallax: ScrollViewer-Event abonnieren
        var scrollViewer = this.FindControl<ScrollViewer>("DashboardScrollViewer");
        if (scrollViewer != null)
            scrollViewer.ScrollChanged += OnScrollChanged;
    }

    /// <summary>
    /// Parallax-Effekt: Header-Background verschiebt sich leicht beim Scrollen.
    /// translateY = -scrollOffset * 0.3, maximal 20px.
    /// </summary>
    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var header = this.FindControl<Border>("HeaderBorder");
        var scrollViewer = sender as ScrollViewer;
        if (header == null || scrollViewer == null) return;

        _headerTranslate ??= new TranslateTransform();
        header.RenderTransform = _headerTranslate;

        var offset = Math.Min(scrollViewer.Offset.Y * 0.3, 20);
        _headerTranslate.Y = -offset;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes VM abmelden
        if (_vm != null)
        {
            _vm.FloatingTextRequested -= OnFloatingTextRequested;
            _vm = null;
        }

        // Neues VM abonnieren
        if (DataContext is MainViewModel vm)
        {
            _vm = vm;
            _vm.FloatingTextRequested += OnFloatingTextRequested;
        }
    }

    private void OnFloatingTextRequested(string text, string category)
    {
        // Farbe je nach Kategorie bestimmen
        var color = category switch
        {
            "money" => Color.Parse("#22C55E"),          // Gruen fuer Geld
            "xp" => Color.Parse("#FFD700"),             // Gold fuer XP
            "golden_screws" => Color.Parse("#FFD700"),   // Gold fuer Goldschrauben
            "level" => Color.Parse("#D97706"),            // Craft-Primaer fuer Level
            _ => Color.Parse("#FFFFFF")
        };

        // FontSize je nach Kategorie
        var fontSize = category switch
        {
            "level" => 20.0,
            "golden_screws" => 18.0,
            _ => 16.0
        };

        // X-Position: zufaellig im sichtbaren Bereich (20-80% der Breite)
        var canvasWidth = FloatingTextCanvas.Bounds.Width;
        if (canvasWidth < 10) canvasWidth = 300; // Fallback
        var x = canvasWidth * (0.2 + _random.NextDouble() * 0.6);

        // Y-Position: ~40% der Hoehe (Mitte-oben)
        var canvasHeight = FloatingTextCanvas.Bounds.Height;
        if (canvasHeight < 10) canvasHeight = 400; // Fallback
        var y = canvasHeight * 0.4;

        FloatingTextCanvas.ShowFloatingText(text, x, y, color, fontSize);
    }
}

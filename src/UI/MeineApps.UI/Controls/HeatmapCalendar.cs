using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace MeineApps.UI.Controls;

/// <summary>
/// GitHub-Style Aktivitäts-Heatmap (7 Zeilen × N Wochen).
/// Zeigt Aktivitäts-Level pro Tag als farbige Zellen.
/// Level 0=keine Aktivität, 1=wenig, 2=mittel, 3=viel, 4=alle Ziele.
/// </summary>
public class HeatmapCalendar : UserControl
{
    public static readonly StyledProperty<Dictionary<DateTime, int>> DataProperty =
        AvaloniaProperty.Register<HeatmapCalendar, Dictionary<DateTime, int>>(
            nameof(Data), new Dictionary<DateTime, int>());

    public static readonly StyledProperty<int> MonthsProperty =
        AvaloniaProperty.Register<HeatmapCalendar, int>(nameof(Months), 3);

    public static readonly StyledProperty<double> CellSizeProperty =
        AvaloniaProperty.Register<HeatmapCalendar, double>(nameof(CellSize), 12);

    public static readonly StyledProperty<double> CellSpacingProperty =
        AvaloniaProperty.Register<HeatmapCalendar, double>(nameof(CellSpacing), 2);

    public static readonly StyledProperty<IBrush?> ColorBrushProperty =
        AvaloniaProperty.Register<HeatmapCalendar, IBrush?>(nameof(ColorBrush));

    /// <summary>
    /// Aktivitäts-Daten pro Tag (Key=Datum, Value=Level 0-4).
    /// </summary>
    public Dictionary<DateTime, int> Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>
    /// Angezeigte Monate (Standard: 3).
    /// </summary>
    public int Months
    {
        get => GetValue(MonthsProperty);
        set => SetValue(MonthsProperty, value);
    }

    /// <summary>
    /// Zellengröße in Pixel (Standard: 12).
    /// </summary>
    public double CellSize
    {
        get => GetValue(CellSizeProperty);
        set => SetValue(CellSizeProperty, value);
    }

    /// <summary>
    /// Abstand zwischen Zellen (Standard: 2).
    /// </summary>
    public double CellSpacing
    {
        get => GetValue(CellSpacingProperty);
        set => SetValue(CellSpacingProperty, value);
    }

    /// <summary>
    /// Basis-Farbe für aktive Zellen (Standard: PrimaryBrush).
    /// </summary>
    public IBrush? ColorBrush
    {
        get => GetValue(ColorBrushProperty);
        set => SetValue(ColorBrushProperty, value);
    }

    static HeatmapCalendar()
    {
        DataProperty.Changed.AddClassHandler<HeatmapCalendar>((c, _) => c.Rebuild());
        MonthsProperty.Changed.AddClassHandler<HeatmapCalendar>((c, _) => c.Rebuild());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Loaded);
    }

    private void Rebuild()
    {
        var data = Data;
        var months = Math.Max(1, Math.Min(Months, 12));
        var cellSize = CellSize;
        var spacing = CellSpacing;

        // Zeitraum berechnen
        var endDate = DateTime.Today;
        var startDate = endDate.AddMonths(-months);
        // Auf Montag der Startwoche runden
        var dayOfWeek = ((int)startDate.DayOfWeek + 6) % 7; // Mo=0, Di=1, ..., So=6
        startDate = startDate.AddDays(-dayOfWeek);

        var totalDays = (endDate - startDate).Days + 1;
        var totalWeeks = (int)Math.Ceiling(totalDays / 7.0);

        // Basis-Farbe ermitteln
        var baseColor = GetBaseColor();

        // Canvas als einfaches Panel mit absoluter Positionierung
        var canvas = new Canvas
        {
            Width = totalWeeks * (cellSize + spacing),
            Height = 7 * (cellSize + spacing)
        };

        // Track-Brush für leere Zellen
        IBrush? trackBrush = null;
        if (Application.Current?.TryGetResource("BorderSubtleBrush", Application.Current.ActualThemeVariant, out var res) == true)
            trackBrush = res as IBrush;
        trackBrush ??= new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));

        for (int week = 0; week < totalWeeks; week++)
        {
            for (int dow = 0; dow < 7; dow++)
            {
                var date = startDate.AddDays(week * 7 + dow);
                if (date > endDate) continue;

                var level = data.TryGetValue(date.Date, out var l) ? Math.Min(l, 4) : 0;

                var cell = new Border
                {
                    Width = cellSize,
                    Height = cellSize,
                    CornerRadius = new CornerRadius(2),
                    Background = level > 0
                        ? new SolidColorBrush(Color.FromArgb(GetOpacity(level), baseColor.R, baseColor.G, baseColor.B))
                        : trackBrush
                };

                Canvas.SetLeft(cell, week * (cellSize + spacing));
                Canvas.SetTop(cell, dow * (cellSize + spacing));
                canvas.Children.Add(cell);
            }
        }

        Content = canvas;
    }

    private Color GetBaseColor()
    {
        if (ColorBrush is SolidColorBrush scb)
            return scb.Color;

        // Fallback: PrimaryBrush aus Theme
        if (Application.Current?.TryGetResource("PrimaryBrush", Application.Current.ActualThemeVariant, out var res) == true &&
            res is SolidColorBrush primary)
            return primary.Color;

        return Color.FromRgb(34, 197, 94); // Grün als Fallback
    }

    private static byte GetOpacity(int level) => level switch
    {
        1 => 64,   // 25%
        2 => 128,  // 50%
        3 => 191,  // 75%
        4 => 255,  // 100%
        _ => 0
    };
}

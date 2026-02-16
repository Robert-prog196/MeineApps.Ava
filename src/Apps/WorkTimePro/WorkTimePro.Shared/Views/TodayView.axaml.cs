using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using SkiaSharp;
using WorkTimePro.Graphics;
using WorkTimePro.Models;
using WorkTimePro.ViewModels;

namespace WorkTimePro.Views;

public partial class TodayView : UserControl
{
    public TodayView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainViewModel vm)
        {
            // Bei relevanten Property-Änderungen Timeline-Canvas invalidieren
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(vm.TodayEntries) or nameof(vm.TodayPauses)
                    or nameof(vm.CurrentStatus) or nameof(vm.CurrentWorkTime))
                {
                    TimelineCanvas?.InvalidateSurface();
                }
            };
        }
    }

    /// <summary>
    /// Zeichnet die Tages-Timeline (Arbeitsblöcke + Pausen als farbige Segmente).
    /// </summary>
    private void OnPaintTimeline(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var bounds = canvas.LocalClipBounds;

        if (DataContext is not MainViewModel vm) return;

        // TimeEntries in TimeBlocks konvertieren (CheckIn/CheckOut-Paare → Arbeitsblöcke)
        var blocks = BuildTimeBlocks(vm);
        float currentHour = DateTime.Now.Hour + DateTime.Now.Minute / 60f;

        DayTimelineVisualization.Render(canvas, bounds, blocks, currentHour);
    }

    /// <summary>
    /// Konvertiert TodayEntries (CheckIn/CheckOut-Paare) + TodayPauses in TimeBlocks.
    /// </summary>
    private static DayTimelineVisualization.TimeBlock[] BuildTimeBlocks(MainViewModel vm)
    {
        var blocks = new List<DayTimelineVisualization.TimeBlock>();

        // 1. Arbeitsblöcke aus TimeEntry-Paaren (CheckIn → CheckOut)
        var entries = vm.TodayEntries.OrderBy(e => e.Timestamp).ToList();
        DateTime? lastCheckIn = null;

        foreach (var entry in entries)
        {
            if (entry.Type == EntryType.CheckIn)
            {
                lastCheckIn = entry.Timestamp;
            }
            else if (entry.Type == EntryType.CheckOut && lastCheckIn != null)
            {
                float startH = lastCheckIn.Value.Hour + lastCheckIn.Value.Minute / 60f;
                float endH = entry.Timestamp.Hour + entry.Timestamp.Minute / 60f;
                blocks.Add(new DayTimelineVisualization.TimeBlock(startH, endH, false));
                lastCheckIn = null;
            }
        }

        // Offener CheckIn → bis jetzt zeichnen
        if (lastCheckIn != null && vm.CurrentStatus != TrackingStatus.Idle)
        {
            float startH = lastCheckIn.Value.Hour + lastCheckIn.Value.Minute / 60f;
            float endH = DateTime.Now.Hour + DateTime.Now.Minute / 60f;
            blocks.Add(new DayTimelineVisualization.TimeBlock(startH, endH, false));
        }

        // 2. Pausen als separate Blöcke überlagern
        foreach (var pause in vm.TodayPauses)
        {
            float startH = pause.StartTime.Hour + pause.StartTime.Minute / 60f;
            float endH = pause.EndTime.HasValue
                ? pause.EndTime.Value.Hour + pause.EndTime.Value.Minute / 60f
                : DateTime.Now.Hour + DateTime.Now.Minute / 60f;
            blocks.Add(new DayTimelineVisualization.TimeBlock(startH, endH, true));
        }

        return blocks.ToArray();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
    }
}

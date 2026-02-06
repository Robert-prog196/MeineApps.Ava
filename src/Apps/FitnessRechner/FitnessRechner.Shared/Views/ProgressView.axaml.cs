using Avalonia.Controls;
using FitnessRechner.ViewModels;

namespace FitnessRechner.Views;

public partial class ProgressView : UserControl
{
    public ProgressView()
    {
        InitializeComponent();
    }

    // Note: Avalonia UserControls don't have OnDisappearing like MAUI ContentPages.
    // The ProgressViewModel implements IDisposable to clean up SkiaSharp/LiveCharts resources.
    // Disposal should be triggered by the parent (MainViewModel) when the view is no longer needed,
    // or by the window closing event. This prevents SIGSEGV crashes from GC finalizer threads
    // accessing native SkiaSharp paint objects while charts are still rendering.
    //
    // If needed, override OnDetachedFromVisualTree to dispose:
    // protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    // {
    //     base.OnDetachedFromVisualTree(e);
    //     (DataContext as IDisposable)?.Dispose();
    // }
}

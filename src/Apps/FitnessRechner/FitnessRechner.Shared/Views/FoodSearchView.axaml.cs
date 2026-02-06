using Avalonia.Controls;

namespace FitnessRechner.Views;

public partial class FoodSearchView : UserControl
{
    public FoodSearchView()
    {
        InitializeComponent();
    }

    // Note: The FoodSearchViewModel implements IDisposable to clean up
    // search debounce cancellation tokens and undo timers.
    // Disposal is handled by the parent when the application shuts down.
}

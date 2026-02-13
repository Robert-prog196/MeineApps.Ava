using Avalonia.Controls;
using RechnerPlus.ViewModels;

namespace RechnerPlus.Views;

public partial class ConverterView : UserControl
{
    private ConverterViewModel? _currentVm;

    public ConverterView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Altes VM abmelden
        if (_currentVm != null)
            _currentVm.ClipboardCopyRequested -= OnClipboardCopy;

        _currentVm = DataContext as ConverterViewModel;

        if (_currentVm != null)
            _currentVm.ClipboardCopyRequested += OnClipboardCopy;
    }

    private async Task OnClipboardCopy(string text)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(text);
    }
}

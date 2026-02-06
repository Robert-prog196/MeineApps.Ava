using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MeineApps.UI.Controls;

public partial class EmptyStateView : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<EmptyStateView, MaterialIconKind>(nameof(Icon), MaterialIconKind.InboxOutline);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<EmptyStateView, string>(nameof(Title), "No items");

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<EmptyStateView, string>(nameof(Subtitle), "");

    public static readonly StyledProperty<string> ActionTextProperty =
        AvaloniaProperty.Register<EmptyStateView, string>(nameof(ActionText), "");

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<EmptyStateView, ICommand?>(nameof(ActionCommand));

    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public EmptyStateView()
    {
        InitializeComponent();
        UpdateBindings();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty ||
            change.Property == TitleProperty ||
            change.Property == SubtitleProperty ||
            change.Property == ActionTextProperty ||
            change.Property == ActionCommandProperty)
        {
            UpdateBindings();
        }
    }

    private void UpdateBindings()
    {
        if (IconElement != null)
            IconElement.Kind = Icon;

        if (TitleText != null)
            TitleText.Text = Title;

        if (SubtitleText != null)
        {
            SubtitleText.Text = Subtitle;
            SubtitleText.IsVisible = !string.IsNullOrEmpty(Subtitle);
        }

        if (ActionButton != null)
        {
            ActionButton.Content = ActionText;
            ActionButton.Command = ActionCommand;
            ActionButton.IsVisible = !string.IsNullOrEmpty(ActionText) && ActionCommand != null;
        }
    }
}

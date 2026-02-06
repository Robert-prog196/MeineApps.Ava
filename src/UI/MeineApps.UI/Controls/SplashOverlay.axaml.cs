using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace MeineApps.UI.Controls;

public partial class SplashOverlay : UserControl
{
    public static readonly StyledProperty<string> AppNameProperty =
        AvaloniaProperty.Register<SplashOverlay, string>(nameof(AppName), "App");

    public static readonly StyledProperty<IImage?> IconSourceProperty =
        AvaloniaProperty.Register<SplashOverlay, IImage?>(nameof(IconSource));

    public string AppName
    {
        get => GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }

    public IImage? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public SplashOverlay()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AppNameProperty)
            AppNameText.Text = change.GetNewValue<string>();
        else if (change.Property == IconSourceProperty)
            AppIconImage.Source = change.GetNewValue<IImage?>();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        AppNameText.Text = AppName;
        AppIconImage.Source = IconSource;

        // Start loading bar animation
        DispatcherTimer.RunOnce(() => LoadingBar.Width = 180, TimeSpan.FromMilliseconds(100));

        // Fade out after delay
        DispatcherTimer.RunOnce(() => Opacity = 0, TimeSpan.FromMilliseconds(1500));

        // Hide after fade completes
        DispatcherTimer.RunOnce(() =>
        {
            IsVisible = false;
            IsHitTestVisible = false;
        }, TimeSpan.FromMilliseconds(2000));
    }
}

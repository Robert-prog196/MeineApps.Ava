using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.Graphics;
using HandwerkerImperium.Models.Enums;
using HandwerkerImperium.ViewModels;
using SkiaSharp;

namespace HandwerkerImperium.Views;

/// <summary>
/// Wrapper-Control für die Workshop-SkiaSharp-Illustration in DataTemplates.
/// Bindet sich automatisch an den WorkshopDisplayModel-DataContext
/// und rendert die passende Workshop-Szene via WorkshopCardRenderer.
/// </summary>
public class WorkshopIllustrationView : SKCanvasView
{
    public WorkshopIllustrationView()
    {
        // Höhe fix setzen
        Height = 48;
        PaintSurface += OnPaintSurface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var bounds = canvas.LocalClipBounds;
        canvas.Clear(SKColors.Transparent);

        // DataContext auslesen
        if (DataContext is not WorkshopDisplayModel model) return;

        WorkshopCardRenderer.Render(canvas, bounds, model.Type, model.IsUnlocked, model.Level);
    }

    /// <summary>
    /// Override: Bei DataContext-Änderung neu zeichnen.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        InvalidateSurface();
    }

    /// <summary>
    /// Override: Bei Property-Änderung neu zeichnen (z.B. Level-Up).
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty)
        {
            InvalidateSurface();
        }
    }
}

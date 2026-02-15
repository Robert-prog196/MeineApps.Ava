using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using HandwerkerImperium.Graphics;
using HandwerkerImperium.Models.Enums;
using SkiaSharp;

namespace HandwerkerImperium.Controls;

/// <summary>
/// Wiederverwendbares Control das einen Pixel-Art Worker-Avatar per SkiaSharp rendert.
/// Nutzt den WorkerAvatarRenderer mit Cache.
/// </summary>
public class WorkerAvatarControl : Control
{
    // ═══════════════════════════════════════════════════════════════════════
    // STYLED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Worker-ID als Seed fuer deterministische Avatar-Generierung.
    /// </summary>
    public static readonly StyledProperty<string> IdSeedProperty =
        AvaloniaProperty.Register<WorkerAvatarControl, string>(nameof(IdSeed), string.Empty);

    /// <summary>
    /// Worker-Tier bestimmt die Helm-Farbe.
    /// </summary>
    public static readonly StyledProperty<WorkerTier> TierProperty =
        AvaloniaProperty.Register<WorkerAvatarControl, WorkerTier>(nameof(Tier), WorkerTier.E);

    /// <summary>
    /// Stimmung (0-100), bestimmt Gesichtsausdruck.
    /// </summary>
    public static readonly StyledProperty<decimal> MoodProperty =
        AvaloniaProperty.Register<WorkerAvatarControl, decimal>(nameof(Mood), 70m);

    /// <summary>
    /// Groesse des Avatars in dp.
    /// </summary>
    public static readonly StyledProperty<int> AvatarSizeProperty =
        AvaloniaProperty.Register<WorkerAvatarControl, int>(nameof(AvatarSize), 48);

    /// <summary>
    /// Geschlecht: true = weiblich, false = maennlich.
    /// </summary>
    public static readonly StyledProperty<bool> IsFemaleProperty =
        AvaloniaProperty.Register<WorkerAvatarControl, bool>(nameof(IsFemale));

    public string IdSeed
    {
        get => GetValue(IdSeedProperty);
        set => SetValue(IdSeedProperty, value);
    }

    public WorkerTier Tier
    {
        get => GetValue(TierProperty);
        set => SetValue(TierProperty, value);
    }

    public decimal Mood
    {
        get => GetValue(MoodProperty);
        set => SetValue(MoodProperty, value);
    }

    public int AvatarSize
    {
        get => GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, value);
    }

    public bool IsFemale
    {
        get => GetValue(IsFemaleProperty);
        set => SetValue(IsFemaleProperty, value);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FELDER
    // ═══════════════════════════════════════════════════════════════════════

    private readonly SKCanvasView _canvasView;
    private SKBitmap? _currentBitmap;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    public WorkerAvatarControl()
    {
        _canvasView = new SKCanvasView();
        _canvasView.PaintSurface += OnPaintSurface;

        // SKCanvasView als visuelles Kind einhaengen
        ((ISetLogicalParent)_canvasView).SetParent(this);
        VisualChildren.Add(_canvasView);
        LogicalChildren.Add(_canvasView);

        // Bei Property-Aenderungen neu rendern
        IdSeedProperty.Changed.AddClassHandler<WorkerAvatarControl>((c, _) => c.InvalidateAvatar());
        TierProperty.Changed.AddClassHandler<WorkerAvatarControl>((c, _) => c.InvalidateAvatar());
        MoodProperty.Changed.AddClassHandler<WorkerAvatarControl>((c, _) => c.InvalidateAvatar());
        AvatarSizeProperty.Changed.AddClassHandler<WorkerAvatarControl>((c, _) => c.InvalidateAvatar());
        IsFemaleProperty.Changed.AddClassHandler<WorkerAvatarControl>((c, _) => c.InvalidateAvatar());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RENDERING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bitmap neu generieren und Canvas invalidieren.
    /// </summary>
    private void InvalidateAvatar()
    {
        // Altes Bitmap freigeben
        _currentBitmap?.Dispose();
        _currentBitmap = null;

        // Neues Bitmap generieren
        var idStr = IdSeed ?? string.Empty;
        int renderSize = AvatarSize switch
        {
            <= 32 => 32,
            <= 64 => 64,
            _ => 128
        };

        _currentBitmap = WorkerAvatarRenderer.RenderAvatar(
            idStr, Tier, Mood, renderSize, IsFemale);

        _canvasView.InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_currentBitmap == null)
        {
            // Erstes Rendern: Bitmap generieren
            InvalidateAvatar();
            if (_currentBitmap == null) return;
        }

        // Bitmap in die verfuegbare Flaeche zeichnen (skaliert)
        var bounds = _canvasView.CanvasSize;
        var destRect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
        var srcRect = new SKRect(0, 0, _currentBitmap.Width, _currentBitmap.Height);

        using var paint = new SKPaint { IsAntialias = false };
        canvas.DrawBitmap(_currentBitmap, srcRect, destRect, paint);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LAYOUT
    // ═══════════════════════════════════════════════════════════════════════

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(AvatarSize, AvatarSize);
        _canvasView.Measure(size);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rect = new Rect(0, 0, AvatarSize, AvatarSize);
        _canvasView.Arrange(rect);
        return new Size(AvatarSize, AvatarSize);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════════════

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _currentBitmap?.Dispose();
        _currentBitmap = null;
    }
}

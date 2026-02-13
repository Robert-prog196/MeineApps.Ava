using SkiaSharp;

namespace BomberBlast.Graphics;

/// <summary>
/// Sprite-Sheet-Platzhalter. Aktuell wird alles prozedural via GameRenderer gerendert.
/// Die Klasse bleibt als DI-Service erhalten fuer zukuenftiges Sprite-basiertes Rendering.
/// </summary>
public class SpriteSheet : IDisposable
{
    private readonly Dictionary<string, SKBitmap> _bitmaps = new();

    /// <summary>Ob Sprites geladen sind (aktuell immer false - prozedurales Rendering)</summary>
    public bool IsLoaded => false;

    /// <summary>Sprites laden (aktuell No-Op, Platzhalter fuer Asset-Pipeline)</summary>
    public Task LoadAsync() => Task.CompletedTask;

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps.Values)
        {
            bitmap.Dispose();
        }
        _bitmaps.Clear();
    }
}

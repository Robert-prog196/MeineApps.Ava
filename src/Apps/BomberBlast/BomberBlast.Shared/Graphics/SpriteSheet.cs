using SkiaSharp;

namespace BomberBlast.Graphics;

/// <summary>
/// Manages loading and rendering of sprite sheets.
/// Uses IAssetLoader abstraction instead of MAUI FileSystem.
/// </summary>
public class SpriteSheet : IDisposable
{
    private readonly Dictionary<string, SKBitmap> _bitmaps = new();
    private readonly Dictionary<string, SpriteInfo> _sprites = new();
    private bool _isLoaded;

    // TODO: Replace bomb_party.png with actual sprite sheet
    // Currently forcing fallback rendering because the file is a screenshot, not a sprite atlas
    public bool IsLoaded => false; // _isLoaded;

    /// <summary>
    /// Load all sprite sheets from resources.
    /// Currently a no-op stub. Override or implement asset loading
    /// when the asset pipeline is set up for Avalonia.
    /// </summary>
    public Task LoadAsync()
    {
        if (_isLoaded)
            return Task.CompletedTask;

        // TODO: Implement asset loading for Avalonia
        // Example: load from avares:// URIs or embedded resources
        // var uri = new Uri("avares://BomberBlast/Assets/Sprites/bomb_party.png");
        // using var stream = AssetLoader.Open(uri);
        // var bitmap = SKBitmap.Decode(stream);

        // Define sprite regions from bomb_party sprite sheet
        DefineBombPartySprites();

        _isLoaded = true;
        return Task.CompletedTask;
    }

    private void DefineBombPartySprites()
    {
        const string sheet = "Sprites/bomb_party.png";
        const int tileSize = 16;

        // Define player sprites (assuming top of sheet)
        for (int dir = 0; dir < 4; dir++)
        {
            for (int frame = 0; frame < 4; frame++)
            {
                _sprites[$"player_{dir}_{frame}"] = new SpriteInfo(
                    sheet, frame * tileSize, dir * tileSize, tileSize, tileSize);
            }
        }

        // Define bomb sprites (row 4)
        for (int frame = 0; frame < 4; frame++)
        {
            _sprites[$"bomb_{frame}"] = new SpriteInfo(
                sheet, frame * tileSize, 4 * tileSize, tileSize, tileSize);
        }

        // Define explosion sprites (row 5-6)
        _sprites["explosion_center"] = new SpriteInfo(sheet, 0, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_h_mid"] = new SpriteInfo(sheet, tileSize, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_v_mid"] = new SpriteInfo(sheet, 2 * tileSize, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_left"] = new SpriteInfo(sheet, 3 * tileSize, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_right"] = new SpriteInfo(sheet, 4 * tileSize, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_top"] = new SpriteInfo(sheet, 5 * tileSize, 5 * tileSize, tileSize, tileSize);
        _sprites["explosion_bottom"] = new SpriteInfo(sheet, 6 * tileSize, 5 * tileSize, tileSize, tileSize);

        // Define tile sprites (row 7)
        _sprites["tile_floor"] = new SpriteInfo(sheet, 0, 7 * tileSize, tileSize, tileSize);
        _sprites["tile_wall"] = new SpriteInfo(sheet, tileSize, 7 * tileSize, tileSize, tileSize);
        _sprites["tile_block"] = new SpriteInfo(sheet, 2 * tileSize, 7 * tileSize, tileSize, tileSize);
        _sprites["tile_exit"] = new SpriteInfo(sheet, 3 * tileSize, 7 * tileSize, tileSize, tileSize);

        // Define power-up sprites (row 8)
        var powerUps = new[] { "bomb_up", "fire", "speed", "wallpass", "detonator", "bombpass", "flamepass", "mystery" };
        for (int i = 0; i < powerUps.Length; i++)
        {
            _sprites[$"powerup_{powerUps[i]}"] = new SpriteInfo(
                sheet, i * tileSize, 8 * tileSize, tileSize, tileSize);
        }

        // Define enemy sprites (rows 9-16, 8 enemy types)
        for (int type = 0; type < 8; type++)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                for (int frame = 0; frame < 2; frame++)
                {
                    _sprites[$"enemy_{type}_{dir}_{frame}"] = new SpriteInfo(
                        sheet, (dir * 2 + frame) * tileSize, (9 + type) * tileSize, tileSize, tileSize);
                }
            }
        }
    }

    /// <summary>
    /// Draw a named sprite
    /// </summary>
    public void DrawSprite(SKCanvas canvas, string spriteName, float x, float y, float width, float height)
    {
        if (!_sprites.TryGetValue(spriteName, out var sprite))
        {
            // Draw placeholder
            DrawPlaceholder(canvas, x, y, width, height, SKColors.Magenta);
            return;
        }

        if (!_bitmaps.TryGetValue(sprite.SheetName, out var bitmap))
        {
            DrawPlaceholder(canvas, x, y, width, height, SKColors.Yellow);
            return;
        }

        var srcRect = new SKRect(sprite.X, sprite.Y, sprite.X + sprite.Width, sprite.Y + sprite.Height);
        var destRect = new SKRect(x, y, x + width, y + height);

        canvas.DrawBitmap(bitmap, srcRect, destRect);
    }

    /// <summary>
    /// Draw sprite with animation frame
    /// </summary>
    public void DrawAnimatedSprite(SKCanvas canvas, string baseName, int frame, float x, float y, float width, float height)
    {
        DrawSprite(canvas, $"{baseName}_{frame}", x, y, width, height);
    }

    /// <summary>
    /// Draw placeholder rectangle when sprite not found
    /// </summary>
    public static void DrawPlaceholder(SKCanvas canvas, float x, float y, float width, float height, SKColor color)
    {
        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(x, y, width, height, paint);
    }

    /// <summary>
    /// Check if a specific sprite exists
    /// </summary>
    public bool HasSprite(string spriteName)
    {
        return _sprites.ContainsKey(spriteName);
    }

    public void Dispose()
    {
        foreach (var bitmap in _bitmaps.Values)
        {
            bitmap.Dispose();
        }
        _bitmaps.Clear();
        _sprites.Clear();
    }
}

/// <summary>
/// Information about a sprite region in a sprite sheet
/// </summary>
public record SpriteInfo(string SheetName, int X, int Y, int Width, int Height);

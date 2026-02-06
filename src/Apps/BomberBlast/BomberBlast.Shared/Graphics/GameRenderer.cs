using BomberBlast.Models.Entities;
using BomberBlast.Models.Grid;
using SkiaSharp;

namespace BomberBlast.Graphics;

/// <summary>
/// Renders the game using SkiaSharp
/// </summary>
public class GameRenderer : IDisposable
{
    private bool _disposed;
    private readonly SpriteSheet _spriteSheet;

    // Rendering settings
    private float _scale = 1f;
    private float _offsetX, _offsetY;

    // Color palette for fallback rendering (GBA-style bright colors)
    private static readonly SKColor FloorColor = new(120, 200, 80);      // Bright green grass
    private static readonly SKColor WallColor = new(100, 100, 120);      // Blue-gray stone
    private static readonly SKColor BlockColor = new(210, 150, 80);      // Light brown/orange brick
    private static readonly SKColor ExitColor = new(50, 255, 150);       // Bright cyan-green
    private static readonly SKColor PlayerColor = new(255, 255, 255);    // White
    private static readonly SKColor BombColor = new(40, 40, 50);         // Dark blue-black
    private static readonly SKColor ExplosionColor = new(255, 150, 50);  // Bright orange-yellow

    // Animation timing
    private float _globalTimer;

    // Pooled SKPaint objects to reduce GC pressure
    private readonly SKPaint _floorPaint = new() { Color = FloorColor };
    private readonly SKPaint _wallPaint = new() { Color = WallColor };
    private readonly SKPaint _wallHighlightPaint = new() { Color = new SKColor(100, 100, 100) };
    private readonly SKPaint _blockPaint = new() { Color = BlockColor };
    private readonly SKPaint _blockLinePaint = new() { Color = new SKColor(100, 60, 30), StrokeWidth = 1, Style = SKPaintStyle.Stroke };
    private readonly SKPaint _exitPaint = new() { Style = SKPaintStyle.Fill };
    private readonly SKPaint _exitDoorPaint = new() { Color = new SKColor(0, 180, 80) };
    private readonly SKPaint _bombPaint = new() { Color = BombColor, Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _fusePaint = new() { StrokeWidth = 2, Style = SKPaintStyle.Stroke, IsAntialias = true };
    private readonly SKPaint _sparkPaint = new() { Color = SKColors.Yellow, Style = SKPaintStyle.Fill };
    private readonly SKPaint _explosionPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _explosionGlowPaint = new() { Style = SKPaintStyle.Fill };
    private readonly SKPaint _playerPaint = new() { Color = PlayerColor, Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _playerDirPaint = new() { Color = SKColors.Blue, Style = SKPaintStyle.Fill };
    private readonly SKPaint _playerDeathPaint = new() { Style = SKPaintStyle.Fill };
    private readonly SKPaint _enemyPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _enemyEyePaint = new() { Color = SKColors.White, Style = SKPaintStyle.Fill };
    private readonly SKPaint _enemyPupilPaint = new() { Color = SKColors.Black, Style = SKPaintStyle.Fill };
    private readonly SKPaint _powerUpBgPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _powerUpTextPaint = new() { Color = SKColors.White, IsAntialias = true };
    private readonly SKFont _powerUpFont = new() { Size = 14, Embolden = true };
    private readonly SKPaint _hudBgPaint = new() { Color = new SKColor(40, 60, 40, 230), Style = SKPaintStyle.Fill };
    private readonly SKPaint _hudTextPaint = new() { Color = SKColors.White, IsAntialias = true };
    private readonly SKFont _hudFont = new(SKTypeface.FromFamilyName("monospace"), 20);

    // Pooled objects to avoid per-frame allocations at 60fps
    private readonly SKPaint _bombGlowPaint = new() { Style = SKPaintStyle.Fill, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6) };
    private readonly SKPaint _outerGlowPaint = new() { Style = SKPaintStyle.Fill, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 4) };
    private readonly SKPath _fusePath = new();
    private readonly SKPaint _hudLinePaint = new() { Color = new SKColor(0, 210, 255, 180), StrokeWidth = 2, Style = SKPaintStyle.Stroke };
    private readonly SKMaskFilter _hudGlowFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, 2);
    private SKShader? _hudGradientShader;
    private float _lastHudScreenWidth;

    public float Scale => _scale;
    public float OffsetX => _offsetX;
    public float OffsetY => _offsetY;

    public GameRenderer(SpriteSheet spriteSheet)
    {
        _spriteSheet = spriteSheet;
    }

    /// <summary>
    /// Calculate rendering scale and offset to center game on screen (Landscape layout)
    /// </summary>
    public void CalculateViewport(float screenWidth, float screenHeight, int gridPixelWidth, int gridPixelHeight)
    {
        // Guard against invalid dimensions
        if (screenWidth <= 0 || screenHeight <= 0 || gridPixelWidth <= 0 || gridPixelHeight <= 0)
            return;

        // Compact HUD at top for landscape
        float hudHeight = 40;
        // Controls overlay directly on game field (no reserved space)
        float availableHeight = screenHeight - hudHeight;

        // Calculate scale to fit grid
        float scaleX = screenWidth / gridPixelWidth;
        float scaleY = availableHeight / gridPixelHeight;
        _scale = Math.Min(scaleX, scaleY);

        // Center horizontally
        _offsetX = (screenWidth - gridPixelWidth * _scale) / 2;
        // Position below HUD
        _offsetY = hudHeight + (availableHeight - gridPixelHeight * _scale) / 2;
    }

    /// <summary>
    /// Update animation timer
    /// </summary>
    public void Update(float deltaTime)
    {
        _globalTimer += deltaTime;
    }

    /// <summary>
    /// Render the entire game
    /// </summary>
    public void Render(SKCanvas canvas, GameGrid grid, Player player,
        IEnumerable<Enemy> enemies, IEnumerable<Bomb> bombs,
        IEnumerable<Explosion> explosions, IEnumerable<PowerUp> powerUps,
        float remainingTime, int score, int lives)
    {
        // Clear background
        canvas.Clear(new SKColor(60, 120, 60));  // GBA-style green background

        // Save canvas state and apply transform
        canvas.Save();
        canvas.Translate(_offsetX, _offsetY);
        canvas.Scale(_scale);

        // Draw grid (floor, walls, blocks)
        RenderGrid(canvas, grid);

        // Draw exit if visible
        RenderExit(canvas, grid);

        // Draw power-ups
        foreach (var powerUp in powerUps)
        {
            if (powerUp.IsActive && powerUp.IsVisible)
            {
                RenderPowerUp(canvas, powerUp);
            }
        }

        // Draw bombs
        foreach (var bomb in bombs)
        {
            if (bomb.IsActive)
            {
                RenderBomb(canvas, bomb);
            }
        }

        // Draw explosions
        foreach (var explosion in explosions)
        {
            if (explosion.IsActive)
            {
                RenderExplosion(canvas, explosion);
            }
        }

        // Draw enemies
        foreach (var enemy in enemies)
        {
            RenderEnemy(canvas, enemy);
        }

        // Draw player
        if (player != null)
        {
            RenderPlayer(canvas, player);
        }

        canvas.Restore();

        // Draw HUD (not scaled with game)
        RenderHUD(canvas, remainingTime, score, lives, player);
    }

    private void RenderGrid(SKCanvas canvas, GameGrid grid)
    {
        int cellSize = GameGrid.CELL_SIZE;

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var cell = grid[x, y];
                float px = x * cellSize;
                float py = y * cellSize;

                // Draw floor first
                if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite("tile_floor"))
                {
                    _spriteSheet.DrawSprite(canvas, "tile_floor", px, py, cellSize, cellSize);
                }
                else
                {
                    canvas.DrawRect(px, py, cellSize, cellSize, _floorPaint);
                }

                // Draw cell content
                switch (cell.Type)
                {
                    case CellType.Wall:
                        if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite("tile_wall"))
                        {
                            _spriteSheet.DrawSprite(canvas, "tile_wall", px, py, cellSize, cellSize);
                        }
                        else
                        {
                            canvas.DrawRect(px, py, cellSize, cellSize, _wallPaint);

                            // Add 3D effect
                            canvas.DrawRect(px, py, cellSize, 3, _wallHighlightPaint);
                            canvas.DrawRect(px, py, 3, cellSize, _wallHighlightPaint);
                        }
                        break;

                    case CellType.Block:
                        if (cell.IsDestroying)
                        {
                            // Destruction animation
                            float progress = cell.DestructionProgress;
                            byte alpha = (byte)(255 * (1 - progress));
                            _blockPaint.Color = BlockColor.WithAlpha(alpha);
                            float shrink = progress * cellSize * 0.3f;
                            canvas.DrawRect(px + shrink, py + shrink,
                                cellSize - shrink * 2, cellSize - shrink * 2, _blockPaint);
                            _blockPaint.Color = BlockColor; // Reset
                        }
                        else
                        {
                            if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite("tile_block"))
                            {
                                _spriteSheet.DrawSprite(canvas, "tile_block", px, py, cellSize, cellSize);
                            }
                            else
                            {
                                canvas.DrawRect(px + 2, py + 2, cellSize - 4, cellSize - 4, _blockPaint);

                                // Brick pattern
                                canvas.DrawLine(px + 2, py + cellSize / 2, px + cellSize - 2, py + cellSize / 2, _blockLinePaint);
                                canvas.DrawLine(px + cellSize / 2, py + 2, px + cellSize / 2, py + cellSize / 2, _blockLinePaint);
                            }
                        }
                        break;
                }
            }
        }
    }

    private void RenderExit(SKCanvas canvas, GameGrid grid)
    {
        foreach (var cell in grid.GetCellsOfType(CellType.Exit))
        {
            float px = cell.X * GameGrid.CELL_SIZE;
            float py = cell.Y * GameGrid.CELL_SIZE;

            // Pulsing animation
            float pulse = MathF.Sin(_globalTimer * 3) * 0.2f + 0.8f;

            if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite("tile_exit"))
            {
                _spriteSheet.DrawSprite(canvas, "tile_exit", px, py, GameGrid.CELL_SIZE, GameGrid.CELL_SIZE);
            }
            else
            {
                _exitPaint.Color = ExitColor.WithAlpha((byte)(255 * pulse));
                float inset = 4;
                canvas.DrawRect(px + inset, py + inset,
                    GameGrid.CELL_SIZE - inset * 2, GameGrid.CELL_SIZE - inset * 2, _exitPaint);

                // Door shape
                canvas.DrawRect(px + 10, py + 8, 12, 16, _exitDoorPaint);
            }
        }
    }

    private void RenderBomb(SKCanvas canvas, Bomb bomb)
    {
        float size = GameGrid.CELL_SIZE;
        float px = bomb.X - size / 2;
        float py = bomb.Y - size / 2;

        // Pulsing animation
        float pulse = MathF.Sin(_globalTimer * 8) * 0.1f + 0.9f;
        float drawSize = size * pulse;
        float offset = (size - drawSize) / 2;

        int frame = (int)(_globalTimer * 4) % 4;

        // Pulsing glow effect behind bomb
        float glowPulse = MathF.Sin(_globalTimer * 6) * 0.3f + 0.5f;
        _bombGlowPaint.Color = new SKColor(255, 100, 0, (byte)(100 * glowPulse));
        canvas.DrawCircle(bomb.X, bomb.Y, drawSize * 0.5f, _bombGlowPaint);

        if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite($"bomb_{frame}"))
        {
            _spriteSheet.DrawSprite(canvas, $"bomb_{frame}",
                px + offset, py + offset, drawSize, drawSize);
        }
        else
        {
            // Fallback: simple bomb shape
            float radius = drawSize * 0.4f;
            canvas.DrawCircle(bomb.X, bomb.Y, radius, _bombPaint);

            // Fuse
            _fusePaint.Color = bomb.IsAboutToExplode ? SKColors.Red : SKColors.Orange;
            _fusePath.Reset();
            _fusePath.MoveTo(bomb.X, bomb.Y - radius);
            _fusePath.QuadTo(bomb.X + 5, bomb.Y - radius - 8, bomb.X + 8, bomb.Y - radius - 4);
            canvas.DrawPath(_fusePath, _fusePaint);

            // Spark
            if (((int)(_globalTimer * 10) % 2) == 0)
            {
                canvas.DrawCircle(bomb.X + 8, bomb.Y - radius - 4, 3, _sparkPaint);
            }
        }
    }

    private void RenderExplosion(SKCanvas canvas, Explosion explosion)
    {
        int frame = (int)(_globalTimer * 12) % 4;
        float alpha = 1f - explosion.Timer / Explosion.DURATION * 0.3f;

        // Outer glow ring for explosion
        _outerGlowPaint.Color = new SKColor(255, 150, 50, (byte)(80 * alpha));

        foreach (var cell in explosion.AffectedCells)
        {
            float glowPx = cell.X * GameGrid.CELL_SIZE;
            float glowPy = cell.Y * GameGrid.CELL_SIZE;
            float glowSize = GameGrid.CELL_SIZE;
            canvas.DrawRect(glowPx - 2, glowPy - 2, glowSize + 4, glowSize + 4, _outerGlowPaint);
        }

        foreach (var cell in explosion.AffectedCells)
        {
            float px = cell.X * GameGrid.CELL_SIZE;
            float py = cell.Y * GameGrid.CELL_SIZE;
            float size = GameGrid.CELL_SIZE;

            string spriteName = cell.Type switch
            {
                ExplosionCellType.Center => "explosion_center",
                ExplosionCellType.HorizontalMiddle => "explosion_h_mid",
                ExplosionCellType.VerticalMiddle => "explosion_v_mid",
                ExplosionCellType.LeftEnd => "explosion_left",
                ExplosionCellType.RightEnd => "explosion_right",
                ExplosionCellType.TopEnd => "explosion_top",
                ExplosionCellType.BottomEnd => "explosion_bottom",
                _ => "explosion_center"
            };

            if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite(spriteName))
            {
                _spriteSheet.DrawSprite(canvas, spriteName, px, py, size, size);
            }
            else
            {
                // Fallback explosion rendering
                _explosionPaint.Color = ExplosionColor.WithAlpha((byte)(255 * alpha));

                float inset = 4;
                switch (cell.Type)
                {
                    case ExplosionCellType.Center:
                        canvas.DrawRect(px + inset, py + inset, size - inset * 2, size - inset * 2, _explosionPaint);
                        break;
                    case ExplosionCellType.HorizontalMiddle:
                        canvas.DrawRect(px, py + inset, size, size - inset * 2, _explosionPaint);
                        break;
                    case ExplosionCellType.VerticalMiddle:
                        canvas.DrawRect(px + inset, py, size - inset * 2, size, _explosionPaint);
                        break;
                    default:
                        canvas.DrawRect(px + inset, py + inset, size - inset * 2, size - inset * 2, _explosionPaint);
                        break;
                }

                // Inner glow
                _explosionGlowPaint.Color = SKColors.Yellow.WithAlpha((byte)(200 * alpha));
                canvas.DrawRect(px + size / 4, py + size / 4, size / 2, size / 2, _explosionGlowPaint);
            }
        }
    }

    private void RenderPlayer(SKCanvas canvas, Player player)
    {
        float size = GameGrid.CELL_SIZE;
        float px = player.X - size / 2;
        float py = player.Y - size / 2;

        // Invincibility/spawn protection blink
        if ((player.IsInvincible || player.HasSpawnProtection) && ((int)(_globalTimer * 10) % 2) == 0)
        {
            return; // Skip frame for blink effect
        }

        if (player.IsDying)
        {
            // Death animation
            float progress = player.DeathTimer / 1.5f;
            byte alpha = (byte)(255 * (1 - progress));
            float scale = 1 + progress * 0.5f;

            _playerDeathPaint.Color = SKColors.Red.WithAlpha(alpha);
            float drawSize = size * scale;
            canvas.DrawCircle(player.X, player.Y, drawSize / 3, _playerDeathPaint);
            return;
        }

        int dirIndex = player.FacingDirection switch
        {
            Direction.Up => 0,
            Direction.Down => 1,
            Direction.Left => 2,
            Direction.Right => 3,
            _ => 1
        };

        int frame = player.IsMoving ? player.AnimationFrame % 4 : 0;
        string spriteName = $"player_{dirIndex}_{frame}";

        if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite(spriteName))
        {
            _spriteSheet.DrawSprite(canvas, spriteName, px, py, size, size);
        }
        else
        {
            // Fallback: simple player shape
            canvas.DrawCircle(player.X, player.Y, size * 0.35f, _playerPaint);

            // Direction indicator
            float dx = player.FacingDirection.GetDeltaX() * 6;
            float dy = player.FacingDirection.GetDeltaY() * 6;
            canvas.DrawCircle(player.X + dx, player.Y + dy, 4, _playerDirPaint);
        }
    }

    private void RenderEnemy(SKCanvas canvas, Enemy enemy)
    {
        float size = GameGrid.CELL_SIZE;
        float px = enemy.X - size / 2;
        float py = enemy.Y - size / 2;

        if (enemy.IsDying)
        {
            // Death animation
            float progress = enemy.DeathTimer / 0.8f;
            byte alpha = (byte)(255 * (1 - progress));

            var (r, g, b) = enemy.Type.GetColor();
            _enemyPaint.Color = new SKColor(r, g, b, alpha);
            float drawSize = size * (1 - progress * 0.5f);
            canvas.DrawCircle(enemy.X, enemy.Y, drawSize / 3, _enemyPaint);
            return;
        }

        int typeIndex = (int)enemy.Type;
        int dirIndex = enemy.FacingDirection switch
        {
            Direction.Up => 0,
            Direction.Down => 1,
            Direction.Left => 2,
            Direction.Right => 3,
            _ => 1
        };

        int frame = enemy.IsMoving ? enemy.AnimationFrame % 2 : 0;
        string spriteName = $"enemy_{typeIndex}_{dirIndex}_{frame}";

        if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite(spriteName))
        {
            _spriteSheet.DrawSprite(canvas, spriteName, px, py, size, size);
        }
        else
        {
            // Fallback: colored circle
            var (r, g, b) = enemy.Type.GetColor();
            _enemyPaint.Color = new SKColor(r, g, b);
            canvas.DrawCircle(enemy.X, enemy.Y, size * 0.35f, _enemyPaint);

            // Eyes
            float eyeOffset = 4;
            canvas.DrawCircle(enemy.X - eyeOffset, enemy.Y - 2, 3, _enemyEyePaint);
            canvas.DrawCircle(enemy.X + eyeOffset, enemy.Y - 2, 3, _enemyEyePaint);

            // Pupils
            float pupilDx = enemy.FacingDirection.GetDeltaX() * 1.5f;
            float pupilDy = enemy.FacingDirection.GetDeltaY() * 1.5f;
            canvas.DrawCircle(enemy.X - eyeOffset + pupilDx, enemy.Y - 2 + pupilDy, 1.5f, _enemyPupilPaint);
            canvas.DrawCircle(enemy.X + eyeOffset + pupilDx, enemy.Y - 2 + pupilDy, 1.5f, _enemyPupilPaint);
        }
    }

    private void RenderPowerUp(SKCanvas canvas, PowerUp powerUp)
    {
        float size = GameGrid.CELL_SIZE;
        float px = powerUp.X - size / 2;
        float py = powerUp.Y - size / 2;

        // Blinking when about to expire
        if (powerUp.IsBlinking && ((int)(_globalTimer * 8) % 2) == 0)
        {
            return;
        }

        // Bobbing animation
        float bob = MathF.Sin(_globalTimer * 3) * 2;
        py += bob;

        string spriteName = $"powerup_{powerUp.Type.ToString().ToLower()}";

        if (_spriteSheet.IsLoaded && _spriteSheet.HasSprite(spriteName))
        {
            _spriteSheet.DrawSprite(canvas, spriteName, px, py, size, size);
        }
        else
        {
            // Fallback: colored rectangle with icon
            SKColor color = powerUp.Type switch
            {
                PowerUpType.BombUp => new SKColor(100, 100, 255),
                PowerUpType.Fire => new SKColor(255, 100, 50),
                PowerUpType.Speed => new SKColor(100, 255, 100),
                PowerUpType.Wallpass => new SKColor(150, 100, 50),
                PowerUpType.Detonator => new SKColor(255, 50, 50),
                PowerUpType.Bombpass => new SKColor(50, 50, 100),
                PowerUpType.Flamepass => new SKColor(255, 200, 50),
                PowerUpType.Mystery => new SKColor(200, 100, 255),
                _ => SKColors.White
            };

            _powerUpBgPaint.Color = color;
            canvas.DrawRoundRect(px + 4, py + 4, size - 8, size - 8, 4, 4, _powerUpBgPaint);

            // Icon text
            string icon = powerUp.Type switch
            {
                PowerUpType.BombUp => "B",
                PowerUpType.Fire => "F",
                PowerUpType.Speed => "S",
                PowerUpType.Wallpass => "W",
                PowerUpType.Detonator => "D",
                PowerUpType.Bombpass => "P",
                PowerUpType.Flamepass => "I",
                PowerUpType.Mystery => "?",
                _ => "?"
            };

            canvas.DrawText(icon, powerUp.X, powerUp.Y + bob + 5, SKTextAlign.Center, _powerUpFont, _powerUpTextPaint);
        }
    }

    private void RenderHUD(SKCanvas canvas, float remainingTime, int score, int lives, Player? player)
    {
        float hudHeight = 40;
        float screenWidth = canvas.LocalClipBounds.Width;

        // Gradient background (cached, recreate only when screen width changes)
        if (_hudGradientShader == null || Math.Abs(_lastHudScreenWidth - screenWidth) > 1f)
        {
            _hudGradientShader?.Dispose();
            _hudGradientShader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(screenWidth, hudHeight),
                new[] { new SKColor(20, 30, 60, 240), new SKColor(40, 60, 100, 240) },
                null,
                SKShaderTileMode.Clamp);
            _lastHudScreenWidth = screenWidth;
        }
        _hudBgPaint.Shader = _hudGradientShader;
        canvas.DrawRect(0, 0, screenWidth, hudHeight, _hudBgPaint);
        _hudBgPaint.Shader = null;

        // Neon-blue separator line under HUD
        canvas.DrawLine(0, hudHeight, screenWidth, hudHeight, _hudLinePaint);

        float y = 27;
        float margin = 15;

        // Time (left)
        bool timeWarning = remainingTime <= 30;
        _hudTextPaint.Color = timeWarning ? SKColors.Red : SKColors.White;
        _hudTextPaint.MaskFilter = _hudGlowFilter;
        _hudFont.Size = 18;
        string timeText = $"TIME: {(int)remainingTime:D3}";
        canvas.DrawText(timeText, margin, y, SKTextAlign.Left, _hudFont, _hudTextPaint);

        // Score (center)
        _hudTextPaint.Color = SKColors.Yellow;
        string scoreText = $"SCORE: {score:D6}";
        canvas.DrawText(scoreText, screenWidth / 2, y, SKTextAlign.Center, _hudFont, _hudTextPaint);

        // Lives (right)
        _hudTextPaint.Color = SKColors.White;
        string livesText = $"LIVES: {lives}";
        canvas.DrawText(livesText, screenWidth - margin, y, SKTextAlign.Right, _hudFont, _hudTextPaint);

        _hudTextPaint.MaskFilter = null;

        // Power-up indicators (if player exists) - inline in HUD
        if (player != null)
        {
            _hudFont.Size = 12;
            _hudTextPaint.Color = new SKColor(150, 220, 255);

            string bombInfo = $"B:{player.MaxBombs} F:{player.FireRange}";
            canvas.DrawText(bombInfo, margin, hudHeight - 4, SKTextAlign.Left, _hudFont, _hudTextPaint);

            // Active power-ups
            List<string> activePowers = new();
            if (player.HasSpeed) activePowers.Add("SPD");
            if (player.HasWallpass) activePowers.Add("WLP");
            if (player.HasDetonator) activePowers.Add("DET");
            if (player.HasBombpass) activePowers.Add("BMP");
            if (player.HasFlamepass) activePowers.Add("FLP");
            if (player.IsInvincible) activePowers.Add($"INV:{(int)player.InvincibilityTimer}");

            if (activePowers.Count > 0)
            {
                _hudTextPaint.Color = SKColors.Cyan;
                string powersText = string.Join(" ", activePowers);
                canvas.DrawText(powersText, screenWidth - margin, hudHeight - 4, SKTextAlign.Right, _hudFont, _hudTextPaint);
            }
        }
    }

    /// <summary>
    /// Convert screen coordinates to grid coordinates
    /// </summary>
    public (int gridX, int gridY) ScreenToGrid(float screenX, float screenY)
    {
        float gameX = (screenX - _offsetX) / _scale;
        float gameY = (screenY - _offsetY) / _scale;

        int gridX = (int)(gameX / GameGrid.CELL_SIZE);
        int gridY = (int)(gameY / GameGrid.CELL_SIZE);

        return (
            Math.Clamp(gridX, 0, GameGrid.WIDTH - 1),
            Math.Clamp(gridY, 0, GameGrid.HEIGHT - 1)
        );
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _floorPaint.Dispose();
        _wallPaint.Dispose();
        _wallHighlightPaint.Dispose();
        _blockPaint.Dispose();
        _blockLinePaint.Dispose();
        _exitPaint.Dispose();
        _exitDoorPaint.Dispose();
        _bombPaint.Dispose();
        _fusePaint.Dispose();
        _sparkPaint.Dispose();
        _explosionPaint.Dispose();
        _explosionGlowPaint.Dispose();
        _playerPaint.Dispose();
        _playerDirPaint.Dispose();
        _playerDeathPaint.Dispose();
        _enemyPaint.Dispose();
        _enemyEyePaint.Dispose();
        _enemyPupilPaint.Dispose();
        _powerUpBgPaint.Dispose();
        _powerUpTextPaint.Dispose();
        _powerUpFont.Dispose();
        _hudBgPaint.Dispose();
        _hudTextPaint.Dispose();
        _hudFont.Dispose();
        _bombGlowPaint.Dispose();
        _outerGlowPaint.Dispose();
        _fusePath.Dispose();
        _hudLinePaint.Dispose();
        _hudGlowFilter.Dispose();
        _hudGradientShader?.Dispose();
    }
}

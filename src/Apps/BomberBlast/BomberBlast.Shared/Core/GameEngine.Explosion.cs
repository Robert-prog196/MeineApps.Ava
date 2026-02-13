using BomberBlast.Graphics;
using BomberBlast.Models.Entities;
using BomberBlast.Models.Grid;

namespace BomberBlast.Core;

/// <summary>
/// Bomben, Explosionen und Block-Zerstörung
/// </summary>
public partial class GameEngine
{
    private void PlaceBomb()
    {
        int gridX = _player.GridX;
        int gridY = _player.GridY;

        var cell = _grid[gridX, gridY];

        // Prüfen ob schon eine Bombe hier liegt
        if (cell.Bomb != null)
            return;

        // Power-Bomb: Einzelne Mega-Bombe mit maximaler Reichweite, verbraucht alle Slots
        if (_player.HasPowerBomb && _player.ActiveBombs == 0)
        {
            PlacePowerBomb(gridX, gridY, cell);
            return;
        }

        // Line-Bomb: Alle verfügbaren Bomben in einer Linie platzieren
        if (_player.HasLineBomb && _player.ActiveBombs == 0)
        {
            PlaceLineBombs(gridX, gridY);
            return;
        }

        // Normale Bombe erstellen
        var bomb = Bomb.CreateAtGrid(gridX, gridY, _player);
        _bombs.Add(bomb);
        cell.Bomb = bomb;
        _player.ActiveBombs++;
        _bombsUsed++;

        _soundManager.PlaySound(SoundManager.SFX_PLACE_BOMB);
        _soundManager.PlaySound(SoundManager.SFX_FUSE);
    }

    /// <summary>
    /// Power-Bomb: Eine einzelne Bombe die alle Slots verbraucht und maximale Reichweite hat
    /// </summary>
    private void PlacePowerBomb(int gridX, int gridY, Cell cell)
    {
        // Reichweite = FireRange + (MaxBombs - 1), mindestens FireRange
        int megaRange = _player.FireRange + _player.MaxBombs - 1;
        var bomb = new Bomb(
            gridX * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f,
            gridY * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f,
            _player, megaRange, _player.HasDetonator);
        _bombs.Add(bomb);
        cell.Bomb = bomb;
        _player.ActiveBombs = _player.MaxBombs; // Alle Slots belegt
        _bombsUsed++;

        _soundManager.PlaySound(SoundManager.SFX_PLACE_BOMB);
        _soundManager.PlaySound(SoundManager.SFX_FUSE);
    }

    /// <summary>
    /// Line-Bomb: Bomben in Blickrichtung auf leeren Zellen platzieren
    /// </summary>
    private void PlaceLineBombs(int startX, int startY)
    {
        int dx = _player.FacingDirection.GetDeltaX();
        int dy = _player.FacingDirection.GetDeltaY();
        if (dx == 0 && dy == 0) { dx = 0; dy = 1; } // Fallback: nach unten

        int placed = 0;
        int maxBombs = _player.MaxBombs;

        for (int i = 0; i < maxBombs; i++)
        {
            int gx = startX + dx * i;
            int gy = startY + dy * i;

            var cell = _grid.TryGetCell(gx, gy);
            if (cell == null || cell.Type != CellType.Empty || cell.Bomb != null)
                break;

            var bomb = Bomb.CreateAtGrid(gx, gy, _player);
            _bombs.Add(bomb);
            cell.Bomb = bomb;
            _player.ActiveBombs++;
            _bombsUsed++;
            placed++;
        }

        if (placed > 0)
        {
            _soundManager.PlaySound(SoundManager.SFX_PLACE_BOMB);
            _soundManager.PlaySound(SoundManager.SFX_FUSE);
        }
    }

    private void DetonateAllBombs()
    {
        foreach (var bomb in _bombs)
        {
            if (bomb.IsManualDetonation && bomb.IsActive && !bomb.HasExploded)
            {
                bomb.ShouldExplode = true;
            }
        }
    }

    private void UpdateBombs(float deltaTime)
    {
        foreach (var bomb in _bombs)
        {
            bomb.Update(deltaTime);

            // Kick-Sliding: Bombe gleitet in Richtung bis Hindernis
            if (bomb.IsSliding && !bomb.HasExploded)
            {
                UpdateBombSlide(bomb, deltaTime);
            }

            // Prüfen ob Spieler komplett von Bombe runtergelaufen ist
            if (bomb.PlayerOnTop)
            {
                float size = GameGrid.CELL_SIZE * 0.35f;

                bool stillOnBomb = false;
                float[] cornersX = { _player.X - size, _player.X + size };
                float[] cornersY = { _player.Y - size, _player.Y + size };

                foreach (float cx in cornersX)
                {
                    foreach (float cy in cornersY)
                    {
                        int cellX = (int)MathF.Floor(cx / GameGrid.CELL_SIZE);
                        int cellY = (int)MathF.Floor(cy / GameGrid.CELL_SIZE);
                        if (cellX == bomb.GridX && cellY == bomb.GridY)
                        {
                            stillOnBomb = true;
                            break;
                        }
                    }
                    if (stillOnBomb) break;
                }

                if (!stillOnBomb)
                {
                    bomb.PlayerOnTop = false;
                }
            }

            // Explosion auslösen wenn fällig
            if (bomb.ShouldExplode && !bomb.HasExploded)
            {
                TriggerExplosion(bomb);
            }
        }
    }

    /// <summary>
    /// Gleitende Bombe aktualisieren (Kick-Mechanik)
    /// </summary>
    private void UpdateBombSlide(Bomb bomb, float deltaTime)
    {
        float dx = bomb.SlideDirection.GetDeltaX() * Bomb.SLIDE_SPEED * deltaTime;
        float dy = bomb.SlideDirection.GetDeltaY() * Bomb.SLIDE_SPEED * deltaTime;

        float newX = bomb.X + dx;
        float newY = bomb.Y + dy;

        // Ziel-Grid-Position berechnen
        int targetGridX = (int)MathF.Floor(newX / GameGrid.CELL_SIZE);
        int targetGridY = (int)MathF.Floor(newY / GameGrid.CELL_SIZE);

        // Prüfen ob Zielzelle blockiert ist
        var targetCell = _grid.TryGetCell(targetGridX, targetGridY);
        if (targetCell == null || targetCell.Type != CellType.Empty ||
            (targetCell.Bomb != null && targetCell.Bomb != bomb))
        {
            // Hindernis: Bombe stoppen und an aktuelle Zellenmitte einrasten
            bomb.StopSlide();
            // Bombe in aktuelle Grid-Zelle registrieren
            var snapCell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
            if (snapCell != null) snapCell.Bomb = bomb;
            return;
        }

        // Prüfen ob ein Gegner auf der Zielzelle steht
        foreach (var enemy in _enemies)
        {
            if (enemy.IsActive && !enemy.IsDying &&
                enemy.GridX == targetGridX && enemy.GridY == targetGridY)
            {
                bomb.StopSlide();
                var snapCell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
                if (snapCell != null) snapCell.Bomb = bomb;
                return;
            }
        }

        // Alte Grid-Zelle freiräumen
        var oldCell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
        if (oldCell != null && oldCell.Bomb == bomb) oldCell.Bomb = null;

        // Bombe bewegen
        bomb.X = newX;
        bomb.Y = newY;

        // Neue Grid-Zelle setzen
        var newCell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
        if (newCell != null) newCell.Bomb = bomb;
    }

    private void TriggerExplosion(Bomb bomb)
    {
        bomb.Explode();

        // Bombe aus Grid entfernen
        var cell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
        if (cell != null)
        {
            cell.Bomb = null;
        }

        // Explosion erstellen
        var explosion = new Explosion(bomb);
        explosion.CalculateSpread(_grid, bomb.Range);
        _explosions.Add(explosion);

        _soundManager.PlaySound(SoundManager.SFX_EXPLOSION);

        // Game-Feel: Screen-Shake und Explosions-Partikel
        _screenShake.Trigger(3f, 0.2f);
        float px = bomb.X;
        float py = bomb.Y;
        _particleSystem.Emit(px, py, 8, ParticleColors.Explosion, 100f, 0.5f);
        _particleSystem.Emit(px, py, 4, ParticleColors.ExplosionLight, 60f, 0.3f);

        // Explosionseffekte sofort verarbeiten
        ProcessExplosion(explosion);
    }

    private void ProcessExplosion(Explosion explosion)
    {
        foreach (var cell in explosion.AffectedCells)
        {
            var gridCell = _grid.TryGetCell(cell.X, cell.Y);
            if (gridCell == null)
                continue;

            // Blöcke zerstören
            if (gridCell.Type == CellType.Block && !gridCell.IsDestroying)
            {
                DestroyBlock(gridCell);
            }

            // Kettenreaktion mit anderen Bomben
            if (gridCell.Bomb != null && !gridCell.Bomb.HasExploded)
            {
                gridCell.Bomb.TriggerChainReaction();
            }

            // PowerUps auf dem Boden zerstören
            if (gridCell.PowerUp != null)
            {
                gridCell.PowerUp.IsMarkedForRemoval = true;
                gridCell.PowerUp = null;
            }
        }
    }

    private const float BLOCK_DESTROY_DURATION = 0.3f;

    private void DestroyBlock(Cell cell)
    {
        cell.IsDestroying = true;
        cell.DestructionProgress = 0f;
    }

    /// <summary>
    /// Timer-basierte Block-Zerstörung (statt Dispatcher.Post + Task.Delay)
    /// </summary>
    private void UpdateDestroyingBlocks(float deltaTime)
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                var cell = _grid[x, y];
                if (!cell.IsDestroying)
                    continue;

                cell.DestructionProgress += deltaTime / BLOCK_DESTROY_DURATION;

                if (cell.DestructionProgress >= 1f)
                {
                    cell.Type = CellType.Empty;
                    cell.IsDestroying = false;
                    cell.DestructionProgress = 0f;

                    // Block-Zerstörungs-Partikel
                    float bpx = cell.X * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f;
                    float bpy = cell.Y * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f;
                    _particleSystem.Emit(bpx, bpy, 5, ParticleColors.BlockDestroy, 50f, 0.4f);
                    _particleSystem.Emit(bpx, bpy, 3, ParticleColors.BlockDestroyLight, 30f, 0.3f);

                    // Exit aufdecken wenn unter diesem Block versteckt (klassisches Bomberman)
                    if (cell.HasHiddenExit)
                    {
                        cell.HasHiddenExit = false;
                        cell.Type = CellType.Exit;
                        _exitRevealed = true;
                        _exitCell = cell;
                        _soundManager.PlaySound(SoundManager.SFX_EXIT_APPEAR);

                        // Exit-Reveal Partikel (grün)
                        _particleSystem.Emit(bpx, bpy, 12, ParticleColors.ExitReveal, 60f, 0.8f);
                        _particleSystem.Emit(bpx, bpy, 6, ParticleColors.ExitRevealLight, 40f, 0.5f);
                    }
                    // PowerUp anzeigen wenn versteckt
                    else if (cell.HiddenPowerUp.HasValue)
                    {
                        var powerUp = PowerUp.CreateAtGrid(cell.X, cell.Y, cell.HiddenPowerUp.Value);
                        _powerUps.Add(powerUp);
                        cell.PowerUp = powerUp;
                        cell.HiddenPowerUp = null;

                        _soundManager.PlaySound(SoundManager.SFX_POWERUP);
                    }

                    CheckExitReveal();
                }
            }
        }
    }

    private void UpdateExplosions(float deltaTime)
    {
        foreach (var explosion in _explosions)
        {
            explosion.Update(deltaTime);

            if (explosion.IsMarkedForRemoval)
            {
                explosion.ClearFromGrid(_grid);
            }
        }

        // Nachglüh-Timer der Zellen aktualisieren
        UpdateAfterglow(deltaTime);
    }

    private void UpdateAfterglow(float deltaTime)
    {
        for (int y = 0; y < _grid.Height; y++)
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                var cell = _grid[x, y];
                if (cell.AfterglowTimer > 0)
                {
                    cell.AfterglowTimer -= deltaTime;
                    if (cell.AfterglowTimer < 0)
                        cell.AfterglowTimer = 0;
                }
            }
        }
    }
}

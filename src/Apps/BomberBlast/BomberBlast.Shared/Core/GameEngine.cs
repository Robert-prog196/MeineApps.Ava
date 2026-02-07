using Avalonia.Threading;
using BomberBlast.AI;
using BomberBlast.Graphics;
using BomberBlast.Input;
using BomberBlast.Models.Entities;
using BomberBlast.Models.Grid;
using BomberBlast.Models.Levels;
using BomberBlast.Services;
using MeineApps.Core.Ava.Localization;
using SkiaSharp;

namespace BomberBlast.Core;

/// <summary>
/// Main game engine that manages game logic and state
/// </summary>
public class GameEngine : IDisposable
{
    // Dependencies
    private readonly SoundManager _soundManager;
    private readonly SpriteSheet _spriteSheet;
    private readonly IProgressService _progressService;
    private readonly IHighScoreService _highScoreService;
    private readonly InputManager _inputManager;
    private readonly ILocalizationService _localizationService;
    private readonly IGameStyleService _gameStyleService;
    private readonly GameRenderer _renderer;

    // Game state
    private GameState _state = GameState.Menu;
    private GameTimer _timer;
    private GameGrid _grid;
    private EnemyAI _enemyAI;

    // Entities
    private Player _player;
    private readonly List<Enemy> _enemies = new();
    private readonly List<Bomb> _bombs = new();
    private readonly List<Explosion> _explosions = new();
    private readonly List<PowerUp> _powerUps = new();

    // Level info
    private Level? _currentLevel;
    private int _currentLevelNumber;
    private bool _isArcadeMode;
    private int _arcadeWave;
    private bool _levelCompleteHandled;

    // Statistics
    private int _bombsUsed;
    private int _enemiesKilled;
    private bool _exitRevealed;

    // Timing
    private float _stateTimer;
    private const float START_DELAY = 2f;
    private const float DEATH_DELAY = 2f;
    private const float LEVEL_COMPLETE_DELAY = 3f;

    // Gecachte SKPaint/SKFont fuer Overlay-Rendering (vermeidet Allokationen pro Frame)
    private readonly SKPaint _overlayBgPaint = new();
    private readonly SKPaint _overlayTextPaint = new() { IsAntialias = true };
    private readonly SKFont _overlayFont = new() { Embolden = true };
    private readonly SKMaskFilter _overlayGlowFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 3);
    private readonly SKMaskFilter _overlayGlowFilterLarge = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4);

    // Events
    public event Action? OnGameOver;
    public event Action? OnLevelComplete;
    public event Action<int>? OnScoreChanged;

    public GameState State => _state;
    public int Score => _player?.Score ?? 0;
    public int Lives => _player?.Lives ?? 0;
    public int CurrentLevel => _currentLevelNumber;
    public int ArcadeWave => _arcadeWave;
    public float RemainingTime => _timer?.RemainingTime ?? 0;
    public bool IsArcadeMode => _isArcadeMode;
    public bool IsCurrentScoreHighScore => _highScoreService.IsHighScore(Score);

    // Touch input forwarding
    public void OnTouchStart(float x, float y, float screenWidth, float screenHeight)
        => _inputManager.OnTouchStart(x, y, screenWidth, screenHeight);

    public void OnTouchMove(float x, float y)
        => _inputManager.OnTouchMove(x, y);

    public void OnTouchEnd()
        => _inputManager.OnTouchEnd();

    // Keyboard input forwarding
    public void OnKeyDown(Avalonia.Input.Key key)
        => _inputManager.OnKeyDown(key);

    public void OnKeyUp(Avalonia.Input.Key key)
        => _inputManager.OnKeyUp(key);

    public GameEngine(
        SoundManager soundManager,
        SpriteSheet spriteSheet,
        IProgressService progressService,
        IHighScoreService highScoreService,
        InputManager inputManager,
        ILocalizationService localizationService,
        IGameStyleService gameStyleService)
    {
        _soundManager = soundManager;
        _spriteSheet = spriteSheet;
        _progressService = progressService;
        _highScoreService = highScoreService;
        _inputManager = inputManager;
        _localizationService = localizationService;
        _gameStyleService = gameStyleService;

        _renderer = new GameRenderer(_spriteSheet, _gameStyleService);
        _grid = new GameGrid();
        _timer = new GameTimer();
        _enemyAI = new EnemyAI(_grid);
        _player = new Player(0, 0);

        // Subscribe to timer events
        _timer.OnWarning += OnTimeWarning;
        _timer.OnExpired += OnTimeExpired;
    }

    /// <summary>
    /// Start a new game in story mode
    /// </summary>
    public async Task StartStoryModeAsync(int levelNumber)
    {
        _isArcadeMode = false;
        _currentLevelNumber = levelNumber;
        _currentLevel = LevelGenerator.GenerateLevel(levelNumber);

        _player.ResetForNewGame();
        await LoadLevelAsync();

        _soundManager.PlayMusic(_currentLevel.MusicTrack == "boss"
            ? SoundManager.MUSIC_BOSS
            : SoundManager.MUSIC_GAMEPLAY);
    }

    /// <summary>
    /// Start a new game in arcade mode
    /// </summary>
    public async Task StartArcadeModeAsync()
    {
        _isArcadeMode = true;
        _arcadeWave = 1;
        _currentLevelNumber = 1;
        _currentLevel = LevelGenerator.GenerateArcadeLevel(1);

        _player.ResetForNewGame();
        _player.Lives = 1; // Arcade mode: 1 life
        await LoadLevelAsync();

        _soundManager.PlayMusic(SoundManager.MUSIC_GAMEPLAY);
    }

    /// <summary>
    /// Load and initialize current level
    /// </summary>
    private async Task LoadLevelAsync()
    {
        if (_currentLevel == null)
            return;

        // Reset state
        _state = GameState.Starting;
        _stateTimer = 0;
        _bombsUsed = 0;
        _enemiesKilled = 0;
        _exitRevealed = false;

        // Clear entities
        _enemies.Clear();
        _bombs.Clear();
        _explosions.Clear();
        _powerUps.Clear();

        // Setup grid
        _grid.Reset();
        _grid.SetupClassicPattern();

        var random = new Random(_currentLevel.Seed ?? DateTime.Now.Millisecond);

        // Place blocks
        _grid.PlaceBlocks(_currentLevel.BlockDensity, random);

        // Spawn player at (1,1)
        _player.SetGridPosition(1, 1);
        _player.MovementDirection = Direction.None;

        // Place power-ups in blocks
        PlacePowerUps(random);

        // Place exit under a block
        PlaceExit(random);

        // Spawn enemies
        SpawnEnemies(random);

        // Reset timer
        _timer.Reset(_currentLevel.TimeLimit);

        // Ensure player is active
        _player.IsActive = true;

        // Load sprites if needed
        if (!_spriteSheet.IsLoaded)
        {
            await _spriteSheet.LoadAsync();
        }
    }

    private void PlacePowerUps(Random random)
    {
        var blocks = _grid.GetCellsOfType(CellType.Block).ToList();
        if (blocks.Count == 0 || _currentLevel?.PowerUps == null)
            return;

        // Shuffle blocks
        blocks = blocks.OrderBy(_ => random.Next()).ToList();

        int blockIndex = 0;
        foreach (var powerUp in _currentLevel.PowerUps)
        {
            if (blockIndex >= blocks.Count)
                break;

            Cell targetCell;
            if (powerUp.X.HasValue && powerUp.Y.HasValue)
            {
                targetCell = _grid.TryGetCell(powerUp.X.Value, powerUp.Y.Value) ?? blocks[blockIndex++];
            }
            else
            {
                targetCell = blocks[blockIndex++];
            }

            if (targetCell.Type == CellType.Block)
            {
                targetCell.HiddenPowerUp = powerUp.Type;
            }
        }
    }

    private void PlaceExit(Random random)
    {
        var blocks = _grid.GetCellsOfType(CellType.Block)
            .Where(c => c.HiddenPowerUp == null) // Don't place on power-up blocks
            .ToList();

        if (blocks.Count == 0)
            return;

        // Place exit in a random block (or specified position)
        Cell exitCell;
        if (_currentLevel?.ExitPosition != null)
        {
            exitCell = _grid.TryGetCell(_currentLevel.ExitPosition.Value.x, _currentLevel.ExitPosition.Value.y)
                ?? blocks[random.Next(blocks.Count)];
        }
        else
        {
            // Prefer exit far from player spawn
            blocks = blocks.OrderByDescending(c =>
                Math.Abs(c.X - 1) + Math.Abs(c.Y - 1)).ToList();
            exitCell = blocks.First();
        }

        // Mark this cell to become exit when destroyed
        exitCell.HiddenPowerUp = null; // Use a special marker
    }

    private void SpawnEnemies(Random random)
    {
        if (_currentLevel?.Enemies == null)
            return;

        // Get valid spawn positions (not near player, not on walls/blocks)
        var validPositions = new List<(int x, int y)>();
        for (int x = 1; x < GameGrid.WIDTH - 1; x++)
        {
            for (int y = 1; y < GameGrid.HEIGHT - 1; y++)
            {
                // Keep away from player spawn area
                if (x <= 3 && y <= 3)
                    continue;

                var cell = _grid[x, y];
                if (cell.Type == CellType.Empty)
                {
                    validPositions.Add((x, y));
                }
            }
        }

        foreach (var spawn in _currentLevel.Enemies)
        {
            for (int i = 0; i < spawn.Count; i++)
            {
                (int x, int y) pos;
                if (spawn.X.HasValue && spawn.Y.HasValue)
                {
                    pos = (spawn.X.Value, spawn.Y.Value);
                }
                else if (validPositions.Count > 0)
                {
                    int index = random.Next(validPositions.Count);
                    pos = validPositions[index];
                    validPositions.RemoveAt(index);
                }
                else
                {
                    // Fallback: random position
                    pos = (random.Next(5, GameGrid.WIDTH - 2), random.Next(5, GameGrid.HEIGHT - 2));
                }

                var enemy = Enemy.CreateAtGrid(pos.x, pos.y, spawn.Type);
                _enemies.Add(enemy);
            }
        }
    }

    /// <summary>
    /// Update game state (call every frame)
    /// </summary>
    public void Update(float deltaTime)
    {
        _renderer.Update(deltaTime);

        switch (_state)
        {
            case GameState.Starting:
                UpdateStarting(deltaTime);
                break;

            case GameState.Playing:
                UpdatePlaying(deltaTime);
                break;

            case GameState.PlayerDied:
                UpdatePlayerDied(deltaTime);
                break;

            case GameState.LevelComplete:
                UpdateLevelComplete(deltaTime);
                break;

            case GameState.Paused:
                // Do nothing
                break;
        }
    }

    private void UpdateStarting(float deltaTime)
    {
        _stateTimer += deltaTime;
        if (_stateTimer >= START_DELAY)
        {
            _state = GameState.Playing;
            _timer.Start();
        }
    }

    private void UpdatePlaying(float deltaTime)
    {
        // Update timer
        _timer.Update(deltaTime);

        // Update player FIRST (reads BombPressed before it's consumed)
        UpdatePlayer(deltaTime);

        // Update input AFTER player (consumes BombPressed)
        _inputManager.Update(deltaTime);

        // Update bombs
        UpdateBombs(deltaTime);

        // Update explosions
        UpdateExplosions(deltaTime);

        // Update enemies
        UpdateEnemies(deltaTime);

        // Update power-ups
        UpdatePowerUps(deltaTime);

        // Check collisions
        CheckCollisions();

        // Check win condition
        CheckWinCondition();

        // Clean up dead entities
        CleanupEntities();
    }

    private void UpdatePlayer(float deltaTime)
    {
        if (_player.IsDying || !_player.IsActive)
        {
            _player.Update(deltaTime);
            return;
        }

        // Apply input
        _player.MovementDirection = _inputManager.MovementDirection;
        _player.Move(deltaTime, _grid);
        _player.Update(deltaTime);

        // Check for bomb placement
        if (_inputManager.BombPressed && _player.CanPlaceBomb())
        {
            PlaceBomb();
        }

        // Check for manual detonation
        if (_inputManager.DetonatePressed && _player.HasDetonator)
        {
            DetonateAllBombs();
        }
    }

    private void PlaceBomb()
    {
        int gridX = _player.GridX;
        int gridY = _player.GridY;

        var cell = _grid[gridX, gridY];

        // Check if bomb already here
        if (cell.Bomb != null)
            return;

        // Create bomb
        var bomb = Bomb.CreateAtGrid(gridX, gridY, _player);
        _bombs.Add(bomb);
        cell.Bomb = bomb;
        _player.ActiveBombs++;
        _bombsUsed++;

        _soundManager.PlaySound(SoundManager.SFX_PLACE_BOMB);
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

            // Check if player moved off bomb completely (all 4 corners of hitbox)
            if (bomb.PlayerOnTop)
            {
                // Player hitbox half-size (same as in Player.CanMoveTo)
                float size = GameGrid.CELL_SIZE * 0.35f;

                // Check if ANY corner of player hitbox is still on the bomb cell
                bool stillOnBomb = false;
                float[] cornersX = { _player.X - size, _player.X + size };
                float[] cornersY = { _player.Y - size, _player.Y + size };

                foreach (float cx in cornersX)
                {
                    foreach (float cy in cornersY)
                    {
                        int cellX = (int)(cx / GameGrid.CELL_SIZE);
                        int cellY = (int)(cy / GameGrid.CELL_SIZE);
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

            // Explode if triggered
            if (bomb.ShouldExplode && !bomb.HasExploded)
            {
                TriggerExplosion(bomb);
            }
        }
    }

    private void TriggerExplosion(Bomb bomb)
    {
        bomb.Explode();

        // Remove bomb from grid
        var cell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
        if (cell != null)
        {
            cell.Bomb = null;
        }

        // Create explosion
        var explosion = new Explosion(bomb);
        explosion.CalculateSpread(_grid, bomb.Range);
        _explosions.Add(explosion);

        _soundManager.PlaySound(SoundManager.SFX_EXPLOSION);

        // Process explosion effects immediately
        ProcessExplosion(explosion);
    }

    private void ProcessExplosion(Explosion explosion)
    {
        foreach (var cell in explosion.AffectedCells)
        {
            var gridCell = _grid.TryGetCell(cell.X, cell.Y);
            if (gridCell == null)
                continue;

            // Destroy blocks
            if (gridCell.Type == CellType.Block && !gridCell.IsDestroying)
            {
                DestroyBlock(gridCell);
            }

            // Chain reaction with other bombs
            if (gridCell.Bomb != null && !gridCell.Bomb.HasExploded)
            {
                gridCell.Bomb.TriggerChainReaction();
            }

            // Destroy power-ups on ground
            if (gridCell.PowerUp != null)
            {
                gridCell.PowerUp.IsMarkedForRemoval = true;
                gridCell.PowerUp = null;
            }
        }
    }

    private void DestroyBlock(Cell cell)
    {
        cell.IsDestroying = true;

        // Schedule block removal and power-up reveal on UI thread
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(300); // Destruction animation time

            if (cell.Type != CellType.Block)
                return;

            cell.Type = CellType.Empty;
            cell.IsDestroying = false;

            // Reveal power-up if hidden
            if (cell.HiddenPowerUp.HasValue)
            {
                var powerUp = PowerUp.CreateAtGrid(cell.X, cell.Y, cell.HiddenPowerUp.Value);
                _powerUps.Add(powerUp);
                cell.PowerUp = powerUp;
                cell.HiddenPowerUp = null;

                _soundManager.PlaySound(SoundManager.SFX_POWERUP);
            }

            // Check if this was the exit block
            CheckExitReveal();
        });
    }

    private void CheckExitReveal()
    {
        // Reveal exit when all enemies are defeated
        if (!_exitRevealed && _enemies.All(e => !e.IsActive || e.IsDying))
        {
            RevealExit();
        }
    }

    private void RevealExit()
    {
        _exitRevealed = true;

        // Find a random empty cell far from player for exit
        var emptyCells = new List<Cell>();
        for (int x = 1; x < GameGrid.WIDTH - 1; x++)
        {
            for (int y = 1; y < GameGrid.HEIGHT - 1; y++)
            {
                var cell = _grid[x, y];
                if (cell.Type == CellType.Empty && cell.Bomb == null && cell.PowerUp == null)
                {
                    emptyCells.Add(cell);
                }
            }
        }

        if (emptyCells.Count > 0)
        {
            // Pick cell furthest from player
            var exitCell = emptyCells.OrderByDescending(c =>
                Math.Abs(c.X - _player.GridX) + Math.Abs(c.Y - _player.GridY)).First();

            exitCell.Type = CellType.Exit;
            _soundManager.PlaySound(SoundManager.SFX_EXIT_APPEAR);
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
    }

    private void UpdateEnemies(float deltaTime)
    {
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive && !enemy.IsDying)
                continue;

            // AI update
            if (enemy.IsActive && !enemy.IsDying)
            {
                _enemyAI.Update(enemy, _player, _bombs, deltaTime);
            }

            enemy.Update(deltaTime);
        }
    }

    private void UpdatePowerUps(float deltaTime)
    {
        foreach (var powerUp in _powerUps)
        {
            powerUp.Update(deltaTime);
        }
    }

    private void CheckCollisions()
    {
        // Player collision with explosions
        foreach (var explosion in _explosions)
        {
            if (!explosion.IsActive)
                continue;

            foreach (var cell in explosion.AffectedCells)
            {
                if (_player.GridX == cell.X && _player.GridY == cell.Y)
                {
                    if (!_player.HasFlamepass && !_player.IsInvincible && !_player.HasSpawnProtection)
                    {
                        KillPlayer();
                    }
                }
            }
        }

        // Player collision with enemies
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsActive || enemy.IsDying)
                continue;

            if (_player.CollidesWith(enemy))
            {
                if (!_player.IsInvincible && !_player.HasSpawnProtection)
                {
                    KillPlayer();
                }
            }
        }

        // Player collision with power-ups (Rueckwaerts-Iteration statt .ToList())
        for (int i = _powerUps.Count - 1; i >= 0; i--)
        {
            var powerUp = _powerUps[i];
            if (!powerUp.IsActive || powerUp.IsMarkedForRemoval)
                continue;

            if (_player.GridX == powerUp.GridX && _player.GridY == powerUp.GridY)
            {
                _player.CollectPowerUp(powerUp);
                powerUp.IsMarkedForRemoval = true;

                var cell = _grid.TryGetCell(powerUp.GridX, powerUp.GridY);
                if (cell != null)
                {
                    cell.PowerUp = null;
                }

                _soundManager.PlaySound(SoundManager.SFX_POWERUP);
                OnScoreChanged?.Invoke(_player.Score);
            }
        }

        // Player collision with exit
        if (_exitRevealed)
        {
            foreach (var cell in _grid.GetCellsOfType(CellType.Exit))
            {
                if (_player.GridX == cell.X && _player.GridY == cell.Y)
                {
                    CompleteLevel();
                }
            }
        }

        // Enemy collision with explosions
        foreach (var explosion in _explosions)
        {
            if (!explosion.IsActive)
                continue;

            foreach (var cell in explosion.AffectedCells)
            {
                foreach (var enemy in _enemies)
                {
                    if (!enemy.IsActive || enemy.IsDying)
                        continue;

                    if (enemy.GridX == cell.X && enemy.GridY == cell.Y)
                    {
                        KillEnemy(enemy);
                    }
                }
            }
        }
    }

    private void KillPlayer()
    {
        if (_player.IsDying)
            return;

        _player.Kill();
        _timer.Pause();
        _state = GameState.PlayerDied;
        _stateTimer = 0;

        _soundManager.PlaySound(SoundManager.SFX_PLAYER_DEATH);
    }

    private void KillEnemy(Enemy enemy)
    {
        enemy.Kill();
        _enemiesKilled++;
        _player.Score += enemy.Points;

        _soundManager.PlaySound(SoundManager.SFX_ENEMY_DEATH);
        OnScoreChanged?.Invoke(_player.Score);

        // Check if all enemies dead
        CheckExitReveal();
    }

    private void CompleteLevel()
    {
        _state = GameState.LevelComplete;
        _stateTimer = 0;
        _levelCompleteHandled = false;
        _timer.Pause();

        // Calculate bonuses
        int timeBonus = (int)_timer.RemainingTime * 10;
        int efficiencyBonus = _bombsUsed <= 7 ? 10000 : 0;
        _player.Score += timeBonus + efficiencyBonus;

        _soundManager.PlaySound(SoundManager.SFX_LEVEL_COMPLETE);
        OnScoreChanged?.Invoke(_player.Score);
    }

    private void UpdatePlayerDied(float deltaTime)
    {
        _stateTimer += deltaTime;

        // Update player death animation
        _player.Update(deltaTime);

        if (_stateTimer >= DEATH_DELAY)
        {
            _player.Lives--;

            if (_player.Lives <= 0)
            {
                _state = GameState.GameOver;
                _soundManager.PlaySound(SoundManager.SFX_GAME_OVER);
                _soundManager.StopMusic();

                // Save high score for arcade mode
                if (_isArcadeMode)
                {
                    if (_highScoreService.IsHighScore(_player.Score))
                    {
                        _highScoreService.AddScore("PLAYER", _player.Score, _arcadeWave);
                    }
                }

                OnGameOver?.Invoke();
            }
            else
            {
                // Respawn
                RespawnPlayer();
            }
        }
    }

    private void RespawnPlayer()
    {
        // Respawn at start position
        _player.Respawn(
            1 * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f,
            1 * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f);

        _state = GameState.Starting;
        _stateTimer = 0;

        // Clear bombs and explosions
        foreach (var bomb in _bombs)
        {
            var cell = _grid.TryGetCell(bomb.GridX, bomb.GridY);
            if (cell != null) cell.Bomb = null;
        }
        _bombs.Clear();

        foreach (var explosion in _explosions)
        {
            explosion.ClearFromGrid(_grid);
        }
        _explosions.Clear();

        _inputManager.Reset();
    }

    private void UpdateLevelComplete(float deltaTime)
    {
        _stateTimer += deltaTime;

        if (_stateTimer >= LEVEL_COMPLETE_DELAY && !_levelCompleteHandled)
        {
            _levelCompleteHandled = true;

            // Fortschritt speichern
            if (!_isArcadeMode)
            {
                _progressService.CompleteLevel(_currentLevelNumber);
                _progressService.SetLevelBestScore(_currentLevelNumber, _player.Score);
            }

            OnLevelComplete?.Invoke();
        }
    }

    private void CheckWinCondition()
    {
        // Already handled in CheckExitReveal and player-exit collision
    }

    private void CleanupEntities()
    {
        _bombs.RemoveAll(b => b.IsMarkedForRemoval);
        _explosions.RemoveAll(e => e.IsMarkedForRemoval);
        _enemies.RemoveAll(e => e.IsMarkedForRemoval);
        _powerUps.RemoveAll(p => p.IsMarkedForRemoval);
    }

    private void OnTimeWarning()
    {
        _soundManager.PlaySound(SoundManager.SFX_TIME_WARNING);
    }

    private void OnTimeExpired()
    {
        // Spawn Pontans as punishment!
        SpawnPontanPunishment();
    }

    private void SpawnPontanPunishment()
    {
        // Spawn Pontans as punishment
        var random = new Random();
        for (int i = 0; i < 4; i++)
        {
            int x = random.Next(3, GameGrid.WIDTH - 1);
            int y = random.Next(3, GameGrid.HEIGHT - 1);

            var enemy = Enemy.CreateAtGrid(x, y, EnemyType.Pontan);
            _enemies.Add(enemy);
        }
    }

    /// <summary>
    /// Pause the game
    /// </summary>
    public void Pause()
    {
        if (_state == GameState.Playing)
        {
            _state = GameState.Paused;
            _timer.Pause();
            _soundManager.PauseMusic();
        }
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void Resume()
    {
        if (_state == GameState.Paused)
        {
            _state = GameState.Playing;
            _timer.Resume();
            _soundManager.ResumeMusic();
        }
    }

    /// <summary>
    /// Advance to next level
    /// </summary>
    public async Task NextLevelAsync()
    {
        if (_isArcadeMode)
        {
            _arcadeWave++;
            _currentLevel = LevelGenerator.GenerateArcadeLevel(_arcadeWave);
        }
        else
        {
            _currentLevelNumber++;
            if (_currentLevelNumber > 50)
            {
                _state = GameState.Victory;
                return;
            }
            _currentLevel = LevelGenerator.GenerateLevel(_currentLevelNumber);
        }

        await LoadLevelAsync();
    }

    /// <summary>
    /// Render the game
    /// </summary>
    public void Render(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        // Don't render if not initialized
        if (_state == GameState.Menu)
        {
            canvas.Clear(new SKColor(20, 20, 30));
            return;
        }

        // Update viewport
        _renderer.CalculateViewport(screenWidth, screenHeight, _grid.PixelWidth, _grid.PixelHeight);

        // Render game
        _renderer.Render(canvas, _grid, _player,
            _enemies, _bombs, _explosions, _powerUps,
            _timer.RemainingTime, _player.Score, _player.Lives);

        // Render input controls
        _inputManager.Render(canvas, screenWidth, screenHeight);

        // Render state overlays
        RenderStateOverlay(canvas, screenWidth, screenHeight);
    }

    private void RenderStateOverlay(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        switch (_state)
        {
            case GameState.Starting:
                RenderStartingOverlay(canvas, screenWidth, screenHeight);
                break;

            case GameState.Paused:
                RenderPausedOverlay(canvas, screenWidth, screenHeight);
                break;

            case GameState.LevelComplete:
                RenderLevelCompleteOverlay(canvas, screenWidth, screenHeight);
                break;

            case GameState.GameOver:
                RenderGameOverOverlay(canvas, screenWidth, screenHeight);
                break;
        }
    }

    private void RenderStartingOverlay(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        _overlayBgPaint.Color = new SKColor(0, 0, 0, 180);
        canvas.DrawRect(0, 0, screenWidth, screenHeight, _overlayBgPaint);

        _overlayFont.Size = 48;
        _overlayTextPaint.Color = SKColors.White;
        _overlayTextPaint.MaskFilter = _overlayGlowFilter;

        string text = _isArcadeMode
            ? string.Format(_localizationService.GetString("WaveOverlay"), _arcadeWave)
            : string.Format(_localizationService.GetString("StageOverlay"), _currentLevelNumber);

        canvas.DrawText(text, screenWidth / 2, screenHeight / 2, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        // Countdown
        int countdown = (int)(START_DELAY - _stateTimer) + 1;
        _overlayFont.Size = 72;
        _overlayTextPaint.Color = SKColors.Yellow;
        canvas.DrawText(countdown.ToString(), screenWidth / 2, screenHeight / 2 + 80, SKTextAlign.Center, _overlayFont, _overlayTextPaint);
    }

    private void RenderPausedOverlay(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        _overlayBgPaint.Color = new SKColor(0, 0, 0, 200);
        canvas.DrawRect(0, 0, screenWidth, screenHeight, _overlayBgPaint);

        _overlayFont.Size = 48;
        _overlayTextPaint.Color = SKColors.White;
        _overlayTextPaint.MaskFilter = _overlayGlowFilter;

        canvas.DrawText(_localizationService.GetString("Paused"), screenWidth / 2, screenHeight / 2, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        _overlayFont.Size = 24;
        _overlayTextPaint.MaskFilter = null;
        canvas.DrawText(_localizationService.GetString("TapToResume"), screenWidth / 2, screenHeight / 2 + 50, SKTextAlign.Center, _overlayFont, _overlayTextPaint);
    }

    private void RenderLevelCompleteOverlay(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        _overlayBgPaint.Color = new SKColor(0, 50, 0, 200);
        canvas.DrawRect(0, 0, screenWidth, screenHeight, _overlayBgPaint);

        _overlayFont.Size = 48;
        _overlayTextPaint.Color = SKColors.Green;
        _overlayTextPaint.MaskFilter = _overlayGlowFilterLarge;

        canvas.DrawText(_localizationService.GetString("LevelComplete"), screenWidth / 2, screenHeight / 2 - 50, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        _overlayTextPaint.Color = SKColors.Yellow;
        _overlayTextPaint.MaskFilter = null;
        _overlayFont.Size = 32;
        canvas.DrawText(string.Format(_localizationService.GetString("ScoreFormat"), _player.Score), screenWidth / 2, screenHeight / 2 + 20, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        _overlayTextPaint.Color = SKColors.Cyan;
        _overlayFont.Size = 24;
        int timeBonus = (int)_timer.RemainingTime * 10;
        canvas.DrawText(string.Format(_localizationService.GetString("TimeBonusFormat"), timeBonus), screenWidth / 2, screenHeight / 2 + 60, SKTextAlign.Center, _overlayFont, _overlayTextPaint);
    }

    private void RenderGameOverOverlay(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        _overlayBgPaint.Color = new SKColor(50, 0, 0, 220);
        canvas.DrawRect(0, 0, screenWidth, screenHeight, _overlayBgPaint);

        _overlayFont.Size = 64;
        _overlayTextPaint.Color = SKColors.Red;
        _overlayTextPaint.MaskFilter = _overlayGlowFilterLarge;

        canvas.DrawText(_localizationService.GetString("GameOver"), screenWidth / 2, screenHeight / 2 - 50, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        _overlayTextPaint.Color = SKColors.White;
        _overlayTextPaint.MaskFilter = null;
        _overlayFont.Size = 32;
        canvas.DrawText(string.Format(_localizationService.GetString("FinalScore"), _player.Score), screenWidth / 2, screenHeight / 2 + 20, SKTextAlign.Center, _overlayFont, _overlayTextPaint);

        _overlayFont.Size = 24;
        if (_isArcadeMode)
        {
            canvas.DrawText(string.Format(_localizationService.GetString("WaveReached"), _arcadeWave), screenWidth / 2, screenHeight / 2 + 60, SKTextAlign.Center, _overlayFont, _overlayTextPaint);
        }
        else
        {
            canvas.DrawText(string.Format(_localizationService.GetString("LevelFormat"), _currentLevelNumber), screenWidth / 2, screenHeight / 2 + 60, SKTextAlign.Center, _overlayFont, _overlayTextPaint);
        }
    }

    public void Dispose()
    {
        _timer.OnWarning -= OnTimeWarning;
        _timer.OnExpired -= OnTimeExpired;

        // Gecachte Overlay-Objekte freigeben
        _overlayBgPaint.Dispose();
        _overlayTextPaint.Dispose();
        _overlayFont.Dispose();
        _overlayGlowFilter.Dispose();
        _overlayGlowFilterLarge.Dispose();
    }
}

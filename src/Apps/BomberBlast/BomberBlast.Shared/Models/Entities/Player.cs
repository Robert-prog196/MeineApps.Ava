using BomberBlast.Models.Grid;

namespace BomberBlast.Models.Entities;

/// <summary>
/// The player character (Bomberman)
/// </summary>
public class Player : Entity
{
    // Base stats
    private const float BASE_SPEED = 80f; // Pixel pro Sekunde
    private const float SPEED_BOOST = 20f; // Pro Speed-Level
    private const int MAX_BOMB_COUNT = 10;
    private const int MAX_FIRE_RANGE = 10;
    private const int MAX_SPEED_LEVEL = 3;

    // Movement
    public Direction FacingDirection { get; set; } = Direction.Down;
    public Direction MovementDirection { get; set; } = Direction.None;
    public bool IsMoving => MovementDirection != Direction.None;

    // Power-up stats (permanent, per Shop-Upgrades auch von aussen setzbar)
    public int MaxBombs { get; set; } = 1;
    public int FireRange { get; set; } = 1;

    // Power-up abilities (lost on death)
    public int SpeedLevel { get; set; }
    /// <summary>Kompatibilitäts-Property: true wenn SpeedLevel > 0</summary>
    public bool HasSpeed
    {
        get => SpeedLevel > 0;
        set => SpeedLevel = value ? Math.Max(SpeedLevel, 1) : 0;
    }
    public bool HasWallpass { get; set; }
    public bool HasDetonator { get; set; }
    public bool HasBombpass { get; set; }
    public bool HasFlamepass { get; set; }
    public bool HasKick { get; set; }
    public bool HasLineBomb { get; set; }
    public bool HasPowerBomb { get; set; }

    // Skull/Curse-System
    public CurseType ActiveCurse { get; set; } = CurseType.None;
    public float CurseTimer { get; set; }
    public bool IsCursed => ActiveCurse != CurseType.None;
    private const float CURSE_DURATION = 10f;
    // Diarrhea: Auto-Bomben-Timer
    public float DiarrheaTimer { get; set; }

    // Schutzschild (absorbiert 1 Gegnerkontakt)
    public bool HasShield { get; set; }

    // Temporary invincibility (Mystery power-up)
    public bool IsInvincible { get; private set; }
    public float InvincibilityTimer { get; private set; }

    // Spawn invincibility (brief immunity after respawn)
    public bool HasSpawnProtection { get; private set; }
    public float SpawnProtectionTimer { get; private set; }
    private const float SPAWN_PROTECTION_DURATION = 2f;

    // Bomb tracking
    public int ActiveBombs { get; set; }

    // Death state
    public bool IsDying { get; private set; }
    public float DeathTimer { get; private set; }
    private const float DEATH_ANIMATION_DURATION = 1.5f;

    // Lives
    public int Lives { get; set; } = 3;

    // Score
    public int Score { get; set; }

    // Collision box is slightly smaller than sprite for forgiving gameplay
    public override (float left, float top, float right, float bottom) BoundingBox
    {
        get
        {
            // Use smaller hitbox (80% of cell size) centered on sprite
            float size = GameGrid.CELL_SIZE * 0.4f;
            return (X - size, Y - size, X + size, Y + size);
        }
    }

    /// <summary>
    /// Aktuelle Bewegungsgeschwindigkeit (staffelbar mit SpeedLevel 0-3, halbiert bei Slow-Curse)
    /// </summary>
    public float Speed
    {
        get
        {
            float speed = BASE_SPEED + SpeedLevel * SPEED_BOOST;
            if (ActiveCurse == CurseType.Slow) speed *= 0.5f;
            return speed;
        }
    }

    public Player(float x, float y) : base(x, y)
    {
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Update invincibility timer
        if (IsInvincible)
        {
            InvincibilityTimer -= deltaTime;
            if (InvincibilityTimer <= 0)
            {
                IsInvincible = false;
                InvincibilityTimer = 0;
            }
        }

        // Update spawn protection timer
        if (HasSpawnProtection)
        {
            SpawnProtectionTimer -= deltaTime;
            if (SpawnProtectionTimer <= 0)
            {
                HasSpawnProtection = false;
                SpawnProtectionTimer = 0;
            }
        }

        // Curse-Timer aktualisieren
        if (IsCursed)
        {
            CurseTimer -= deltaTime;
            if (CurseTimer <= 0)
            {
                ActiveCurse = CurseType.None;
                CurseTimer = 0;
            }

            // Diarrhea: Auto-Bomben alle 0.5s
            if (ActiveCurse == CurseType.Diarrhea)
            {
                DiarrheaTimer -= deltaTime;
            }
        }

        // Update death animation
        if (IsDying)
        {
            DeathTimer += deltaTime;
            // Spieler wird nie aus der Engine entfernt (Respawn oder GameOver)
            // → kein IsMarkedForRemoval setzen
        }
    }

    protected override int GetAnimationFrameCount()
    {
        if (IsDying) return 4;
        return IsMoving ? 4 : 1;
    }

    /// <summary>
    /// Move player in current direction
    /// </summary>
    public void Move(float deltaTime, GameGrid grid)
    {
        if (IsDying || MovementDirection == Direction.None)
            return;

        FacingDirection = MovementDirection;

        float dx = MovementDirection.GetDeltaX() * Speed * deltaTime;
        float dy = MovementDirection.GetDeltaY() * Speed * deltaTime;

        // Try to move with collision detection
        TryMove(dx, dy, grid);
    }

    private void TryMove(float dx, float dy, GameGrid grid)
    {
        float newX = X + dx;
        float newY = Y + dy;

        // Check collision at new position
        if (CanMoveTo(newX, newY, grid))
        {
            X = newX;
            Y = newY;
        }
        else
        {
            // Try sliding along walls (for smoother movement)
            if (dx != 0 && CanMoveTo(newX, Y, grid))
            {
                X = newX;
            }
            else if (dy != 0 && CanMoveTo(X, newY, grid))
            {
                Y = newY;
            }
            // Apply corner sliding (helps player round corners smoothly)
            else if (dx != 0)
            {
                TryCornerSlide(newX, Y, grid, dy: true);
            }
            else if (dy != 0)
            {
                TryCornerSlide(X, newY, grid, dy: false);
            }
        }

        // Keep within grid bounds
        float halfSize = GameGrid.CELL_SIZE * 0.4f;
        X = Math.Clamp(X, halfSize, grid.PixelWidth - halfSize);
        Y = Math.Clamp(Y, halfSize, grid.PixelHeight - halfSize);
    }

    private void TryCornerSlide(float targetX, float targetY, GameGrid grid, bool dy)
    {
        // Corner sliding: if blocked, try to nudge perpendicular to help round corners
        float slideAmount = Speed * 0.016f; // Small nudge

        if (dy)
        {
            // Try sliding up or down
            if (CanMoveTo(targetX, Y - slideAmount, grid))
                Y -= slideAmount;
            else if (CanMoveTo(targetX, Y + slideAmount, grid))
                Y += slideAmount;
        }
        else
        {
            // Try sliding left or right
            if (CanMoveTo(X - slideAmount, targetY, grid))
                X -= slideAmount;
            else if (CanMoveTo(X + slideAmount, targetY, grid))
                X += slideAmount;
        }
    }

    private bool CanMoveTo(float newX, float newY, GameGrid grid)
    {
        float halfSize = GameGrid.CELL_SIZE * 0.35f;
        return CollisionHelper.CanMoveToPlayer(newX, newY, halfSize, grid, HasWallpass, HasBombpass);
    }

    /// <summary>
    /// Collect a power-up
    /// </summary>
    public void CollectPowerUp(PowerUp powerUp)
    {
        Score += powerUp.Type.GetPoints();

        switch (powerUp.Type)
        {
            case PowerUpType.BombUp:
                MaxBombs = Math.Min(MaxBombs + 1, MAX_BOMB_COUNT);
                break;

            case PowerUpType.Fire:
                FireRange = Math.Min(FireRange + 1, MAX_FIRE_RANGE);
                break;

            case PowerUpType.Speed:
                SpeedLevel = Math.Min(SpeedLevel + 1, MAX_SPEED_LEVEL);
                break;

            case PowerUpType.Wallpass:
                HasWallpass = true;
                break;

            case PowerUpType.Detonator:
                HasDetonator = true;
                break;

            case PowerUpType.Bombpass:
                HasBombpass = true;
                break;

            case PowerUpType.Flamepass:
                HasFlamepass = true;
                break;

            case PowerUpType.Mystery:
                ActivateInvincibility(PowerUpType.Mystery.GetDuration());
                break;

            case PowerUpType.Kick:
                HasKick = true;
                break;

            case PowerUpType.LineBomb:
                HasLineBomb = true;
                break;

            case PowerUpType.PowerBomb:
                HasPowerBomb = true;
                break;

            case PowerUpType.Skull:
                ActivateCurse();
                break;
        }
    }

    /// <summary>
    /// Zufälligen Fluch aktivieren (Skull-PowerUp)
    /// </summary>
    public void ActivateCurse()
    {
        var curses = new[] { CurseType.Diarrhea, CurseType.Slow, CurseType.Constipation, CurseType.ReverseControls };
        ActiveCurse = curses[Random.Shared.Next(curses.Length)];
        CurseTimer = CURSE_DURATION;

        if (ActiveCurse == CurseType.Diarrhea)
            DiarrheaTimer = 0.5f;
    }

    /// <summary>
    /// Activate invincibility for specified duration
    /// </summary>
    public void ActivateInvincibility(float duration)
    {
        IsInvincible = true;
        InvincibilityTimer = duration;
    }

    /// <summary>
    /// Kill the player (starts death animation)
    /// </summary>
    public void Kill()
    {
        // Flamepass wird NICHT hier geprüft - nur in der Explosions-Kollision (GameEngine.Collision.cs)
        // Kill() wird auch bei Gegner-Kontakt aufgerufen, wo Flamepass nicht schützen soll
        if (IsDying || IsInvincible || HasSpawnProtection)
            return;

        IsDying = true;
        DeathTimer = 0;
        AnimationFrame = 0;
        AnimationTimer = 0;
        IsActive = false;
    }

    /// <summary>
    /// Reset player state for respawn (loses non-permanent power-ups)
    /// </summary>
    public void Respawn(float x, float y)
    {
        X = x;
        Y = y;
        IsDying = false;
        DeathTimer = 0;
        IsActive = true;
        IsMarkedForRemoval = false;
        FacingDirection = Direction.Down;
        MovementDirection = Direction.None;
        AnimationFrame = 0;
        AnimationTimer = 0;
        ActiveBombs = 0;

        // Lose non-permanent power-ups
        SpeedLevel = 0;
        HasWallpass = false;
        HasDetonator = false;
        HasBombpass = false;
        HasFlamepass = false;
        HasKick = false;
        HasLineBomb = false;
        HasPowerBomb = false;
        HasShield = false;
        IsInvincible = false;
        InvincibilityTimer = 0;
        ActiveCurse = CurseType.None;
        CurseTimer = 0;

        // Grant spawn protection
        HasSpawnProtection = true;
        SpawnProtectionTimer = SPAWN_PROTECTION_DURATION;

        // Keep permanent power-ups (MaxBombs, FireRange)
    }

    /// <summary>
    /// Reset all stats for new game
    /// </summary>
    public void ResetForNewGame()
    {
        MaxBombs = 1;
        FireRange = 1;
        SpeedLevel = 0;
        HasWallpass = false;
        HasDetonator = false;
        HasBombpass = false;
        HasFlamepass = false;
        HasKick = false;
        HasLineBomb = false;
        HasPowerBomb = false;
        HasShield = false;
        IsInvincible = false;
        InvincibilityTimer = 0;
        HasSpawnProtection = false;
        SpawnProtectionTimer = 0;
        ActiveCurse = CurseType.None;
        CurseTimer = 0;
        DiarrheaTimer = 0;
        Lives = 3;
        Score = 0;
        ActiveBombs = 0;
        IsDying = false;
        DeathTimer = 0;
        IsActive = true;
        IsMarkedForRemoval = false;
        FacingDirection = Direction.Down;
        MovementDirection = Direction.None;
        AnimationFrame = 0;
        AnimationTimer = 0;
    }

    /// <summary>
    /// Check if player can place a bomb
    /// </summary>
    public bool CanPlaceBomb()
    {
        if (ActiveCurse == CurseType.Constipation) return false;
        return !IsDying && ActiveBombs < MaxBombs;
    }
}

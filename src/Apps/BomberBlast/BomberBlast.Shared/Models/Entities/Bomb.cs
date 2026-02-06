namespace BomberBlast.Models.Entities;

/// <summary>
/// Bomb entity that explodes after a timer
/// </summary>
public class Bomb : Entity
{
    /// <summary>Default fuse time in seconds (original NES)</summary>
    public const float DEFAULT_FUSE_TIME = 3.0f;

    /// <summary>Owner player (for bomb count tracking)</summary>
    public Player Owner { get; }

    /// <summary>Explosion range in cells</summary>
    public int Range { get; }

    /// <summary>Time remaining until explosion</summary>
    public float FuseTimer { get; private set; }

    /// <summary>Whether bomb is about to explode (for visual warning)</summary>
    public bool IsAboutToExplode => FuseTimer <= 0.5f;

    /// <summary>Whether player is still standing on bomb (just placed)</summary>
    public bool PlayerOnTop { get; set; } = true;

    /// <summary>Whether bomb is manual detonation (Detonator power-up)</summary>
    public bool IsManualDetonation { get; }

    /// <summary>Whether bomb should explode on next update</summary>
    public bool ShouldExplode { get; set; }

    /// <summary>Whether bomb has already exploded</summary>
    public bool HasExploded { get; private set; }

    public override float AnimationSpeed => 4f;

    public Bomb(float x, float y, Player owner, int range, bool isManual = false) : base(x, y)
    {
        Owner = owner;
        Range = range;
        FuseTimer = DEFAULT_FUSE_TIME;
        IsManualDetonation = isManual;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        if (HasExploded)
            return;

        // Manual detonation bombs don't count down
        if (!IsManualDetonation)
        {
            FuseTimer -= deltaTime;
            if (FuseTimer <= 0)
            {
                ShouldExplode = true;
            }
        }
    }

    /// <summary>
    /// Trigger explosion (called by game engine)
    /// </summary>
    public void Explode()
    {
        if (HasExploded)
            return;

        HasExploded = true;
        IsActive = false;
        IsMarkedForRemoval = true;

        // Decrease owner's active bomb count
        if (Owner != null)
        {
            Owner.ActiveBombs = Math.Max(0, Owner.ActiveBombs - 1);
        }
    }

    /// <summary>
    /// Trigger explosion from chain reaction
    /// </summary>
    public void TriggerChainReaction()
    {
        ShouldExplode = true;
    }

    protected override int GetAnimationFrameCount() => 4;

    /// <summary>
    /// Create bomb at grid position
    /// </summary>
    public static Bomb CreateAtGrid(int gridX, int gridY, Player owner)
    {
        float x = gridX * 32 + 16; // CELL_SIZE / 2
        float y = gridY * 32 + 16;
        return new Bomb(x, y, owner, owner.FireRange, owner.HasDetonator);
    }
}

namespace BomberBlast.Models.Entities;

/// <summary>
/// Power-up item that can be collected by the player
/// </summary>
public class PowerUp : Entity
{
    /// <summary>Type of this power-up</summary>
    public PowerUpType Type { get; }

    /// <summary>Whether this power-up is visible (revealed after block destruction)</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Blinking animation when about to expire</summary>
    public bool IsBlinking { get; set; }

    /// <summary>Time remaining before disappearing (0 = infinite)</summary>
    public float RemainingTime { get; set; }

    /// <summary>Standard lifetime before disappearing (0 = never)</summary>
    public const float DEFAULT_LIFETIME = 0f; // Infinite in original

    public override float AnimationSpeed => 4f;

    public PowerUp(float x, float y, PowerUpType type) : base(x, y)
    {
        Type = type;
        RemainingTime = DEFAULT_LIFETIME;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Handle expiration timer if set
        if (RemainingTime > 0)
        {
            RemainingTime -= deltaTime;

            // Start blinking when almost expired
            if (RemainingTime <= 3f)
            {
                IsBlinking = true;
            }

            if (RemainingTime <= 0)
            {
                IsMarkedForRemoval = true;
            }
        }
    }

    protected override int GetAnimationFrameCount() => 2;

    /// <summary>
    /// Create power-up at grid position
    /// </summary>
    public static PowerUp CreateAtGrid(int gridX, int gridY, PowerUpType type)
    {
        float x = gridX * 32 + 16; // CELL_SIZE / 2
        float y = gridY * 32 + 16;
        return new PowerUp(x, y, type);
    }
}

using BomberBlast.Models.Grid;

namespace BomberBlast.Models.Entities;

/// <summary>
/// Base class for all game entities (player, enemies, bombs, etc.)
/// </summary>
public abstract class Entity
{
    /// <summary>Unique identifier</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Pixel position X (center of entity)</summary>
    public float X { get; set; }

    /// <summary>Pixel position Y (center of entity)</summary>
    public float Y { get; set; }

    /// <summary>Entity width in pixels</summary>
    public virtual int Width => GameGrid.CELL_SIZE;

    /// <summary>Entity height in pixels</summary>
    public virtual int Height => GameGrid.CELL_SIZE;

    /// <summary>Whether entity is active/alive</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Whether entity should be removed from game</summary>
    public bool IsMarkedForRemoval { get; set; }

    /// <summary>Current animation frame</summary>
    public int AnimationFrame { get; set; }

    /// <summary>Animation timer</summary>
    public float AnimationTimer { get; set; }

    /// <summary>Animation speed (frames per second)</summary>
    public virtual float AnimationSpeed => 8f;

    /// <summary>Grid X position (column)</summary>
    public int GridX => (int)(X / GameGrid.CELL_SIZE);

    /// <summary>Grid Y position (row)</summary>
    public int GridY => (int)(Y / GameGrid.CELL_SIZE);

    /// <summary>Bounding box for collision detection</summary>
    public virtual (float left, float top, float right, float bottom) BoundingBox
    {
        get
        {
            float halfW = Width / 2f;
            float halfH = Height / 2f;
            return (X - halfW, Y - halfH, X + halfW, Y + halfH);
        }
    }

    protected Entity(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Update entity state
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds</param>
    public virtual void Update(float deltaTime)
    {
        UpdateAnimation(deltaTime);
    }

    /// <summary>
    /// Update animation frame
    /// </summary>
    protected virtual void UpdateAnimation(float deltaTime)
    {
        AnimationTimer += deltaTime;
        float frameDuration = 1f / AnimationSpeed;

        while (AnimationTimer >= frameDuration)
        {
            AnimationTimer -= frameDuration;
            AnimationFrame = (AnimationFrame + 1) % GetAnimationFrameCount();
        }
    }

    /// <summary>
    /// Get number of animation frames for current state
    /// </summary>
    protected virtual int GetAnimationFrameCount() => 4;

    /// <summary>
    /// Check collision with another entity
    /// </summary>
    public bool CollidesWith(Entity other)
    {
        if (!IsActive || !other.IsActive)
            return false;

        var a = BoundingBox;
        var b = other.BoundingBox;

        return a.left < b.right &&
               a.right > b.left &&
               a.top < b.bottom &&
               a.bottom > b.top;
    }

    /// <summary>
    /// Check if entity is at specific grid position
    /// </summary>
    public bool IsAtGridPosition(int gridX, int gridY)
    {
        return GridX == gridX && GridY == gridY;
    }

    /// <summary>
    /// Set position from grid coordinates (centered in cell)
    /// </summary>
    public void SetGridPosition(int gridX, int gridY)
    {
        X = gridX * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f;
        Y = gridY * GameGrid.CELL_SIZE + GameGrid.CELL_SIZE / 2f;
    }

    /// <summary>
    /// Calculate distance to another entity
    /// </summary>
    public float DistanceTo(Entity other)
    {
        float dx = other.X - X;
        float dy = other.Y - Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculate Manhattan distance to grid position
    /// </summary>
    public int ManhattanDistanceTo(int gridX, int gridY)
    {
        return Math.Abs(GridX - gridX) + Math.Abs(GridY - gridY);
    }
}

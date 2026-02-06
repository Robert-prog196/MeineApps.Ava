namespace BomberBlast.Models.Grid;

/// <summary>
/// Types of cells in the game grid
/// </summary>
public enum CellType
{
    /// <summary>Empty floor - player and enemies can walk here</summary>
    Empty,

    /// <summary>Indestructible wall - blocks movement and explosions</summary>
    Wall,

    /// <summary>Destructible block - can be destroyed by explosions, may contain power-up</summary>
    Block,

    /// <summary>Exit portal - appears after all enemies are defeated</summary>
    Exit
}

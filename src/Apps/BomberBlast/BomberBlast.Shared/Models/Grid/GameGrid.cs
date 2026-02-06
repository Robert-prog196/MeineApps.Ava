namespace BomberBlast.Models.Grid;

/// <summary>
/// The game grid (11x9 like GBA Bomberman - compact for mobile)
/// </summary>
public class GameGrid
{
    /// <summary>Grid width in cells (GBA style)</summary>
    public const int WIDTH = 11;

    /// <summary>Grid height in cells (GBA style)</summary>
    public const int HEIGHT = 9;

    /// <summary>Cell size in pixels (for rendering)</summary>
    public const int CELL_SIZE = 32;

    private readonly Cell[,] _cells;

    /// <summary>
    /// Get cell at position
    /// </summary>
    public Cell this[int x, int y]
    {
        get
        {
            if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT)
                throw new ArgumentOutOfRangeException($"Cell ({x},{y}) is out of bounds");
            return _cells[x, y];
        }
    }

    /// <summary>
    /// Grid width in cells
    /// </summary>
    public int Width => WIDTH;

    /// <summary>
    /// Grid height in cells
    /// </summary>
    public int Height => HEIGHT;

    /// <summary>
    /// Grid width in pixels
    /// </summary>
    public int PixelWidth => WIDTH * CELL_SIZE;

    /// <summary>
    /// Grid height in pixels
    /// </summary>
    public int PixelHeight => HEIGHT * CELL_SIZE;

    public GameGrid()
    {
        _cells = new Cell[WIDTH, HEIGHT];
        Initialize();
    }

    /// <summary>
    /// Initialize empty grid with border walls
    /// </summary>
    private void Initialize()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                _cells[x, y] = new Cell(x, y, CellType.Empty);
            }
        }
    }

    /// <summary>
    /// Setup classic Bomberman grid pattern with indestructible walls
    /// </summary>
    public void SetupClassicPattern()
    {
        // Border walls
        for (int x = 0; x < WIDTH; x++)
        {
            _cells[x, 0].Type = CellType.Wall;
            _cells[x, HEIGHT - 1].Type = CellType.Wall;
        }
        for (int y = 0; y < HEIGHT; y++)
        {
            _cells[0, y].Type = CellType.Wall;
            _cells[WIDTH - 1, y].Type = CellType.Wall;
        }

        // Interior walls in checkerboard pattern (every 2nd cell)
        for (int x = 2; x < WIDTH - 1; x += 2)
        {
            for (int y = 2; y < HEIGHT - 1; y += 2)
            {
                _cells[x, y].Type = CellType.Wall;
            }
        }
    }

    /// <summary>
    /// Place destructible blocks randomly
    /// </summary>
    /// <param name="density">Percentage of empty cells to fill (0.0-1.0)</param>
    /// <param name="random">Random generator for reproducible levels</param>
    public void PlaceBlocks(float density, Random random)
    {
        // Collect all placeable positions (empty cells)
        var placeableCells = new List<Cell>();

        for (int x = 1; x < WIDTH - 1; x++)
        {
            for (int y = 1; y < HEIGHT - 1; y++)
            {
                var cell = _cells[x, y];

                // Skip walls
                if (cell.Type == CellType.Wall)
                    continue;

                // Skip player spawn area (top-left corner)
                if (IsPlayerSpawnArea(x, y))
                    continue;

                placeableCells.Add(cell);
            }
        }

        // Place blocks based on density
        int blockCount = (int)(placeableCells.Count * density);

        // Shuffle and take first N
        var shuffled = placeableCells.OrderBy(_ => random.Next()).Take(blockCount);

        foreach (var cell in shuffled)
        {
            cell.Type = CellType.Block;
        }
    }

    /// <summary>
    /// Check if position is in player spawn area (must remain clear)
    /// </summary>
    private bool IsPlayerSpawnArea(int x, int y)
    {
        // Player spawns at (1,1), keep (1,1), (1,2), (2,1) clear
        return (x == 1 && y == 1) || (x == 1 && y == 2) || (x == 2 && y == 1);
    }

    /// <summary>
    /// Check if position is valid grid coordinate
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT;
    }

    /// <summary>
    /// Try get cell at position (returns null if out of bounds)
    /// </summary>
    public Cell? TryGetCell(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return null;
        return _cells[x, y];
    }

    /// <summary>
    /// Get all cells of a specific type
    /// </summary>
    public IEnumerable<Cell> GetCellsOfType(CellType type)
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                if (_cells[x, y].Type == type)
                    yield return _cells[x, y];
            }
        }
    }

    /// <summary>
    /// Count cells of a specific type
    /// </summary>
    public int CountCells(CellType type)
    {
        int count = 0;
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                if (_cells[x, y].Type == type)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Get neighbors of a cell (4-directional)
    /// </summary>
    public IEnumerable<Cell> GetNeighbors(int x, int y)
    {
        if (IsValidPosition(x - 1, y)) yield return _cells[x - 1, y];
        if (IsValidPosition(x + 1, y)) yield return _cells[x + 1, y];
        if (IsValidPosition(x, y - 1)) yield return _cells[x, y - 1];
        if (IsValidPosition(x, y + 1)) yield return _cells[x, y + 1];
    }

    /// <summary>
    /// Convert pixel position to grid position
    /// </summary>
    public (int x, int y) PixelToGrid(float pixelX, float pixelY)
    {
        int gridX = (int)(pixelX / CELL_SIZE);
        int gridY = (int)(pixelY / CELL_SIZE);
        return (Math.Clamp(gridX, 0, WIDTH - 1), Math.Clamp(gridY, 0, HEIGHT - 1));
    }

    /// <summary>
    /// Convert grid position to pixel position (center of cell)
    /// </summary>
    public (float x, float y) GridToPixel(int gridX, int gridY)
    {
        return (gridX * CELL_SIZE + CELL_SIZE / 2f, gridY * CELL_SIZE + CELL_SIZE / 2f);
    }

    /// <summary>
    /// Clear all dynamic elements (bombs, explosions, power-ups)
    /// </summary>
    public void ClearDynamicElements()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                var cell = _cells[x, y];
                cell.Bomb = null;
                cell.PowerUp = null;
                cell.IsExploding = false;
                cell.ExplosionProgress = 0;
                cell.IsDestroying = false;
                cell.DestructionProgress = 0;
            }
        }
    }

    /// <summary>
    /// Reset entire grid to empty
    /// </summary>
    public void Reset()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                _cells[x, y].Clear();
            }
        }
    }
}

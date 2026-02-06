using BomberBlast.AI.PathFinding;
using BomberBlast.Models.Entities;
using BomberBlast.Models.Grid;

namespace BomberBlast.AI;

/// <summary>
/// Enemy AI controller using behavior-based decisions
/// </summary>
public class EnemyAI
{
    private readonly GameGrid _grid;
    private readonly AStar _pathfinder;
    private readonly Random _random = new();

    // Detection ranges (in cells)
    private const int LOW_INTEL_DETECTION = 3;
    private const int NORMAL_INTEL_DETECTION = 5;
    private const int HIGH_INTEL_DETECTION = 8;

    public EnemyAI(GameGrid grid)
    {
        _grid = grid;
        _pathfinder = new AStar(grid);
    }

    /// <summary>
    /// Update enemy AI and set movement direction
    /// </summary>
    public void Update(Enemy enemy, Player player, IEnumerable<Bomb> bombs, float deltaTime)
    {
        if (enemy.IsDying || !enemy.IsActive)
            return;

        // Check if it's time for a new decision
        if (enemy.AIDecisionTimer > 0)
        {
            // Continue current movement
            enemy.Move(deltaTime, _grid);
            return;
        }

        // Reset decision timer
        enemy.AIDecisionTimer = enemy.AIDecisionInterval * (0.8f + _random.NextSingle() * 0.4f);

        // Calculate danger zone from bombs
        var dangerZone = CalculateDangerZone(bombs);

        // Check if we're in danger
        bool inDanger = dangerZone.Contains((enemy.GridX, enemy.GridY));

        // Priority 1: Evade if in danger
        if (inDanger)
        {
            if (TryEvade(enemy, dangerZone))
            {
                enemy.Move(deltaTime, _grid);
                return;
            }
        }

        // Priority 2: Behavior based on intelligence
        switch (enemy.Intelligence)
        {
            case EnemyIntelligence.Low:
                UpdateLowIntelligence(enemy, player);
                break;

            case EnemyIntelligence.Normal:
                UpdateNormalIntelligence(enemy, player, dangerZone);
                break;

            case EnemyIntelligence.High:
                UpdateHighIntelligence(enemy, player, dangerZone);
                break;
        }

        // Check for stuck state and force direction change
        if (enemy.StuckTimer > 1.0f)
        {
            enemy.MovementDirection = GetRandomValidDirection(enemy);
            enemy.StuckTimer = 0;
        }

        enemy.Move(deltaTime, _grid);
    }

    private void UpdateLowIntelligence(Enemy enemy, Player player)
    {
        // Low intelligence: Simple back-and-forth movement
        // Only change direction when blocked or randomly

        if (enemy.MovementDirection == Direction.None || _random.NextDouble() < 0.1)
        {
            // Pick random direction, with preference for current direction
            if (enemy.MovementDirection != Direction.None && _random.NextDouble() < 0.7)
            {
                // Keep current direction if valid
                if (CanMoveInDirection(enemy, enemy.MovementDirection))
                    return;
            }

            enemy.MovementDirection = GetRandomValidDirection(enemy);
        }
    }

    private void UpdateNormalIntelligence(Enemy enemy, Player player, HashSet<(int, int)> dangerZone)
    {
        // Normal intelligence: Erratic movement, sometimes chases player

        // 30% chance to chase player if within detection range
        int detectionRange = NORMAL_INTEL_DETECTION;
        int distanceToPlayer = enemy.ManhattanDistanceTo(player.GridX, player.GridY);

        if (distanceToPlayer <= detectionRange && _random.NextDouble() < 0.3)
        {
            // Use cached path if valid
            if (TryFollowCachedPath(enemy, dangerZone))
                return;

            // Calculate new path
            var path = _pathfinder.FindPath(
                enemy.GridX, enemy.GridY,
                player.GridX, player.GridY,
                enemy.CanPassWalls, avoidBombs: true);

            if (path.Count > 0)
            {
                // Cache the path
                enemy.Path = path;
                enemy.TargetPosition = (player.GridX, player.GridY);

                var next = path.Peek();
                enemy.MovementDirection = GetDirectionTo(enemy.GridX, enemy.GridY, next.x, next.y);
                return;
            }
        }

        // Otherwise, random movement
        if (enemy.MovementDirection == Direction.None || _random.NextDouble() < 0.2)
        {
            enemy.MovementDirection = GetRandomValidDirection(enemy);
        }
    }

    private void UpdateHighIntelligence(Enemy enemy, Player player, HashSet<(int, int)> dangerZone)
    {
        // High intelligence: Actively chases player, avoids bombs
        // Uses state machine with hysteresis to prevent rapid switching

        int detectionRange = HIGH_INTEL_DETECTION;
        int distanceToPlayer = enemy.ManhattanDistanceTo(player.GridX, player.GridY);

        // Hysteresis: different thresholds for entering vs leaving chase mode
        bool shouldChase = enemy.AIState == EnemyAIState.Chasing
            ? distanceToPlayer <= (int)(detectionRange * 1.3f)  // Keep chasing until further away
            : distanceToPlayer <= detectionRange;                // Start chasing at normal range

        if (shouldChase)
        {
            // Switch to chasing state
            enemy.AIState = EnemyAIState.Chasing;

            // Use cached path if valid and safe
            if (TryFollowCachedPath(enemy, dangerZone))
                return;

            // Calculate new path
            var path = _pathfinder.FindPath(
                enemy.GridX, enemy.GridY,
                player.GridX, player.GridY,
                enemy.CanPassWalls, avoidBombs: true);

            if (path.Count > 0)
            {
                var next = path.Peek();

                // Don't move into danger zone
                if (!dangerZone.Contains((next.x, next.y)))
                {
                    // Cache the path
                    enemy.Path = path;
                    enemy.TargetPosition = (player.GridX, player.GridY);

                    enemy.MovementDirection = GetDirectionTo(enemy.GridX, enemy.GridY, next.x, next.y);
                    return;
                }
            }
        }
        else
        {
            // Switch to wandering state
            enemy.AIState = EnemyAIState.Wandering;
        }

        // Patrol or wander
        if (enemy.MovementDirection == Direction.None || _random.NextDouble() < 0.15)
        {
            enemy.MovementDirection = GetRandomSafeDirection(enemy, dangerZone);
        }
    }

    /// <summary>
    /// Try to follow cached path. Returns true if valid step taken.
    /// </summary>
    private bool TryFollowCachedPath(Enemy enemy, HashSet<(int, int)> dangerZone)
    {
        if (enemy.Path.Count == 0)
            return false;

        // Check if we reached the next waypoint
        var nextWaypoint = enemy.Path.Peek();
        if (enemy.GridX == nextWaypoint.x && enemy.GridY == nextWaypoint.y)
        {
            enemy.Path.Dequeue();
            if (enemy.Path.Count == 0)
            {
                enemy.TargetPosition = null;
                return false;
            }
            nextWaypoint = enemy.Path.Peek();
        }

        // Check if next waypoint is safe
        if (dangerZone.Contains((nextWaypoint.x, nextWaypoint.y)))
        {
            // Path compromised, clear it
            enemy.Path.Clear();
            enemy.TargetPosition = null;
            return false;
        }

        // Check if path is still valid (no new obstacles)
        if (!CanMoveInDirection(enemy, GetDirectionTo(enemy.GridX, enemy.GridY, nextWaypoint.x, nextWaypoint.y)))
        {
            enemy.Path.Clear();
            enemy.TargetPosition = null;
            return false;
        }

        enemy.MovementDirection = GetDirectionTo(enemy.GridX, enemy.GridY, nextWaypoint.x, nextWaypoint.y);
        return true;
    }

    private bool TryEvade(Enemy enemy, HashSet<(int, int)> dangerZone)
    {
        // Optimized: Only test 4 cardinal directions for escape
        // Early exit when safe direction found
        var bestDirection = Direction.None;

        foreach (var dir in DirectionExtensions.GetCardinalDirections())
        {
            if (!CanMoveInDirection(enemy, dir))
                continue;

            int nx = enemy.GridX + dir.GetDeltaX();
            int ny = enemy.GridY + dir.GetDeltaY();

            // Check if this cell is safe
            if (!dangerZone.Contains((nx, ny)))
            {
                // Found safe direction - use it immediately (early exit)
                enemy.MovementDirection = dir;
                return true;
            }

            // Remember we can at least move somewhere
            if (bestDirection == Direction.None)
            {
                bestDirection = dir;
            }
        }

        // No immediate safe direction found - use pathfinder for complex escape
        var safeCell = _pathfinder.FindSafeCell(
            enemy.GridX, enemy.GridY,
            dangerZone, enemy.CanPassWalls);

        if (safeCell.HasValue)
        {
            var direction = GetDirectionTo(
                enemy.GridX, enemy.GridY,
                safeCell.Value.x, safeCell.Value.y);

            if (direction != Direction.None && CanMoveInDirection(enemy, direction))
            {
                enemy.MovementDirection = direction;
                return true;
            }
        }

        // Last resort: move anywhere we can
        if (bestDirection != Direction.None)
        {
            enemy.MovementDirection = bestDirection;
            return true;
        }

        return false;
    }

    private HashSet<(int, int)> CalculateDangerZone(IEnumerable<Bomb> bombs)
    {
        var dangerZone = new HashSet<(int, int)>();

        foreach (var bomb in bombs)
        {
            if (!bomb.IsActive)
                continue;

            int bx = bomb.GridX;
            int by = bomb.GridY;

            // Bomb cell is dangerous
            dangerZone.Add((bx, by));

            // Calculate explosion range in all directions
            foreach (var dir in DirectionExtensions.GetCardinalDirections())
            {
                for (int i = 1; i <= bomb.Range; i++)
                {
                    int nx = bx + dir.GetDeltaX() * i;
                    int ny = by + dir.GetDeltaY() * i;

                    var cell = _grid.TryGetCell(nx, ny);
                    if (cell == null || cell.Type == CellType.Wall)
                        break;

                    dangerZone.Add((nx, ny));

                    if (cell.Type == CellType.Block)
                        break;
                }
            }
        }

        return dangerZone;
    }

    private Direction GetRandomValidDirection(Enemy enemy)
    {
        var validDirections = DirectionExtensions.GetCardinalDirections()
            .Where(d => CanMoveInDirection(enemy, d))
            .ToList();

        if (validDirections.Count == 0)
            return Direction.None;

        return validDirections[_random.Next(validDirections.Count)];
    }

    private Direction GetRandomSafeDirection(Enemy enemy, HashSet<(int, int)> dangerZone)
    {
        var safeDirections = DirectionExtensions.GetCardinalDirections()
            .Where(d =>
            {
                if (!CanMoveInDirection(enemy, d))
                    return false;

                int nx = enemy.GridX + d.GetDeltaX();
                int ny = enemy.GridY + d.GetDeltaY();
                return !dangerZone.Contains((nx, ny));
            })
            .ToList();

        if (safeDirections.Count == 0)
            return GetRandomValidDirection(enemy);

        return safeDirections[_random.Next(safeDirections.Count)];
    }

    private bool CanMoveInDirection(Enemy enemy, Direction direction)
    {
        int nx = enemy.GridX + direction.GetDeltaX();
        int ny = enemy.GridY + direction.GetDeltaY();

        var cell = _grid.TryGetCell(nx, ny);
        if (cell == null)
            return false;

        if (cell.Type == CellType.Wall)
            return false;

        if (cell.Type == CellType.Block && !enemy.CanPassWalls)
            return false;

        if (cell.Bomb != null)
            return false;

        return true;
    }

    private Direction GetDirectionTo(int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;

        // Prefer horizontal/vertical based on greater difference
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            return dx > 0 ? Direction.Right : Direction.Left;
        }
        else if (dy != 0)
        {
            return dy > 0 ? Direction.Down : Direction.Up;
        }

        return Direction.None;
    }
}

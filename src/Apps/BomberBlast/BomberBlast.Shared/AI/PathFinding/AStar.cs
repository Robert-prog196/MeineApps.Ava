using BomberBlast.Models.Grid;

namespace BomberBlast.AI.PathFinding;

/// <summary>
/// A* pathfinding implementation for enemy AI
/// </summary>
public class AStar
{
    private readonly GameGrid _grid;

    public AStar(GameGrid grid)
    {
        _grid = grid;
    }

    /// <summary>
    /// Find path from start to goal
    /// </summary>
    /// <param name="startX">Start grid X</param>
    /// <param name="startY">Start grid Y</param>
    /// <param name="goalX">Goal grid X</param>
    /// <param name="goalY">Goal grid Y</param>
    /// <param name="canPassWalls">Whether entity can pass through blocks</param>
    /// <param name="avoidBombs">Whether to avoid bomb cells</param>
    /// <returns>Queue of grid positions to follow, or empty if no path</returns>
    public Queue<(int x, int y)> FindPath(int startX, int startY, int goalX, int goalY,
        bool canPassWalls = false, bool avoidBombs = true)
    {
        var openSet = new PriorityQueue<Node, float>();
        var closedSet = new HashSet<(int, int)>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var gScore = new Dictionary<(int, int), float>();

        var startNode = new Node(startX, startY);
        gScore[(startX, startY)] = 0;
        openSet.Enqueue(startNode, Heuristic(startX, startY, goalX, goalY));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current.X == goalX && current.Y == goalY)
            {
                return ReconstructPath(cameFrom, (goalX, goalY));
            }

            closedSet.Add((current.X, current.Y));

            foreach (var neighbor in GetNeighbors(current.X, current.Y, canPassWalls, avoidBombs))
            {
                if (closedSet.Contains((neighbor.X, neighbor.Y)))
                    continue;

                float tentativeG = gScore[(current.X, current.Y)] + 1;

                if (!gScore.ContainsKey((neighbor.X, neighbor.Y)) ||
                    tentativeG < gScore[(neighbor.X, neighbor.Y)])
                {
                    cameFrom[(neighbor.X, neighbor.Y)] = (current.X, current.Y);
                    gScore[(neighbor.X, neighbor.Y)] = tentativeG;

                    float fScore = tentativeG + Heuristic(neighbor.X, neighbor.Y, goalX, goalY);
                    openSet.Enqueue(neighbor, fScore);
                }
            }
        }

        // No path found
        return new Queue<(int x, int y)>();
    }

    /// <summary>
    /// Find nearest safe cell (not threatened by bombs)
    /// </summary>
    public (int x, int y)? FindSafeCell(int startX, int startY, HashSet<(int, int)> dangerZone,
        bool canPassWalls = false)
    {
        var visited = new HashSet<(int, int)>();
        var queue = new Queue<(int x, int y, int dist)>();
        queue.Enqueue((startX, startY, 0));
        visited.Add((startX, startY));

        while (queue.Count > 0)
        {
            var (x, y, dist) = queue.Dequeue();

            // Found safe cell
            if (!dangerZone.Contains((x, y)))
            {
                return (x, y);
            }

            // Don't search too far
            if (dist > 10)
                continue;

            foreach (var neighbor in GetNeighbors(x, y, canPassWalls, avoidBombs: false))
            {
                if (!visited.Contains((neighbor.X, neighbor.Y)))
                {
                    visited.Add((neighbor.X, neighbor.Y));
                    queue.Enqueue((neighbor.X, neighbor.Y, dist + 1));
                }
            }
        }

        return null;
    }

    private IEnumerable<Node> GetNeighbors(int x, int y, bool canPassWalls, bool avoidBombs)
    {
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dx, dy) in directions)
        {
            int nx = x + dx;
            int ny = y + dy;

            var cell = _grid.TryGetCell(nx, ny);
            if (cell == null)
                continue;

            // Check walkability
            if (cell.Type == CellType.Wall)
                continue;

            if (cell.Type == CellType.Block && !canPassWalls)
                continue;

            if (avoidBombs && cell.Bomb != null)
                continue;

            yield return new Node(nx, ny);
        }
    }

    private float Heuristic(int x1, int y1, int x2, int y2)
    {
        // Manhattan distance
        return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
    }

    private Queue<(int x, int y)> ReconstructPath(Dictionary<(int, int), (int, int)> cameFrom, (int x, int y) current)
    {
        var path = new List<(int x, int y)> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        // Skip start position
        if (path.Count > 1)
            path.RemoveAt(0);

        return new Queue<(int x, int y)>(path);
    }

    private class Node
    {
        public int X { get; }
        public int Y { get; }

        public Node(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}

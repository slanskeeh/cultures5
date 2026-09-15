using Cultures.World;

namespace Cultures.Population;

public enum MovementBlock : byte
{
    None = 0,
    OutsideWorld = 1,
    Terrain = 2
}

/// <summary>
/// Wrap-aware 4-direction grid navigation. Replaceable later; not a world-scale pathfinder.
/// </summary>
public sealed class GridNavigator
{
    public GridNavigator(LogicalWorld world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
    }

    public LogicalWorld World { get; }

    public bool IsPassable(LogicalGridCoordinate cell)
    {
        var terrain = World.Grid.GetCell(cell);
        return terrain.Passable;
    }

    public bool TryNeighbor(
        LogicalGridCoordinate from,
        int dx,
        int dy,
        out LogicalGridCoordinate next,
        out MovementBlock block)
    {
        var resolution = World.Topology.Resolve(from.X + dx, from.Y + dy);
        if (!resolution.TryGetCell(out next))
        {
            block = MovementBlock.OutsideWorld;
            return false;
        }

        if (!IsPassable(next))
        {
            block = MovementBlock.Terrain;
            return false;
        }

        block = MovementBlock.None;
        return true;
    }

    public IEnumerable<LogicalGridCoordinate> PassableNeighbors(LogicalGridCoordinate cell)
    {
        if (TryNeighbor(cell, 1, 0, out var east, out _))
            yield return east;
        if (TryNeighbor(cell, -1, 0, out var west, out _))
            yield return west;
        if (TryNeighbor(cell, 0, -1, out var north, out _))
            yield return north;
        if (TryNeighbor(cell, 0, 1, out var south, out _))
            yield return south;
    }

    public List<LogicalGridCoordinate>? FindPath(LogicalGridCoordinate from, LogicalGridCoordinate to)
    {
        if (from.Equals(to))
            return [];

        var cameFrom = new Dictionary<LogicalGridCoordinate, LogicalGridCoordinate>();
        var queue = new Queue<LogicalGridCoordinate>();
        queue.Enqueue(from);
        cameFrom[from] = from;
        var visited = 1;

        while (queue.Count > 0 && visited < CharacterRules.PathSearchLimit)
        {
            var current = queue.Dequeue();
            foreach (var next in PassableNeighbors(current))
            {
                if (!cameFrom.TryAdd(next, current))
                    continue;

                visited++;
                if (next.Equals(to))
                    return Reconstruct(cameFrom, from, to);

                queue.Enqueue(next);
                if (visited >= CharacterRules.PathSearchLimit)
                    break;
            }
        }

        return null;
    }

    private static List<LogicalGridCoordinate> Reconstruct(
        Dictionary<LogicalGridCoordinate, LogicalGridCoordinate> cameFrom,
        LogicalGridCoordinate from,
        LogicalGridCoordinate to)
    {
        var path = new List<LogicalGridCoordinate>();
        var current = to;
        while (!current.Equals(from))
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}

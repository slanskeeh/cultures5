using Cultures.World;

namespace Cultures.Population;

public enum MovementBlock : byte
{
    None = 0,
    OutsideWorld = 1,
    Terrain = 2,
    Occupancy = 3
}

/// <summary>
/// Wrap-aware hex-grid navigation. Replaceable later; not a world-scale pathfinder.
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
        return terrain.Generated.Passable && !terrain.Occupancy.BlocksMovement;
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

        var terrain = World.Grid.GetCell(next);
        if (!terrain.Generated.Passable)
        {
            block = MovementBlock.Terrain;
            return false;
        }

        if (terrain.Occupancy.BlocksMovement)
        {
            block = MovementBlock.Occupancy;
            return false;
        }

        block = MovementBlock.None;
        return true;
    }

    public IEnumerable<LogicalGridCoordinate> PassableNeighbors(LogicalGridCoordinate cell)
    {
        foreach (var (dx, dy) in HexGrid.NeighborOffsets(cell.Y))
        {
            if (TryNeighbor(cell, dx, dy, out var next, out _))
                yield return next;
        }
    }

    public List<LogicalGridCoordinate>? FindPath(LogicalGridCoordinate from, LogicalGridCoordinate to)
    {
        if (from.Equals(to))
            return [];

        var cameFrom = new Dictionary<LogicalGridCoordinate, LogicalGridCoordinate>();
        var cost = new Dictionary<LogicalGridCoordinate, int> { [from] = 0 };
        var open = new PriorityQueue<LogicalGridCoordinate, int>();
        open.Enqueue(from, World.Topology.HexDistance(from, to));
        var closed = new HashSet<LogicalGridCoordinate>();
        var expansions = 0;

        while (open.Count > 0 && expansions < CharacterRules.PathSearchLimit)
        {
            var current = open.Dequeue();
            if (!closed.Add(current))
                continue;

            expansions++;
            if (current.Equals(to))
                return Reconstruct(cameFrom, from, to);

            var g = cost[current];
            foreach (var next in PassableNeighbors(current))
            {
                var tentative = g + 1;
                if (cost.TryGetValue(next, out var existing) && tentative >= existing)
                    continue;

                cameFrom[next] = current;
                cost[next] = tentative;
                open.Enqueue(next, tentative + World.Topology.HexDistance(next, to));
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

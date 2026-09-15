using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Temporary clustered spawn on land. Not a settlement.
/// </summary>
public static class PopulationSpawner
{
    public static PopulationRoster Spawn(
        LogicalWorld world,
        EntityIdFactory ids,
        ulong seed,
        int count = CharacterRules.DefaultPopulation)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(ids);
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var origin = FindLandOrigin(world);
        var roster = new PopulationRoster();
        var used = new HashSet<LogicalGridCoordinate> { origin };
        var navigator = new GridNavigator(world);

        for (var i = 0; i < count; i++)
        {
            var cell = i == 0 ? origin : NextLandCell(world, navigator, origin, i, used);
            var character = new CharacterState(ids.NextCharacter(), cell, seed ^ (ulong)(i + 1) * 0x9E3779B97F4A7C15UL)
            {
                WorkCell = origin
            };
            roster.Add(character);
            used.Add(cell);
        }

        return roster;
    }

    public static LogicalGridCoordinate FindLandOrigin(LogicalWorld world)
    {
        var y = world.Configuration.Height / 2;
        for (var x = 0; x < world.Configuration.Width; x++)
        {
            var cell = new LogicalGridCoordinate(x, y);
            if (world.Grid.GetCell(cell).Passable)
                return cell;
        }

        for (var y2 = 0; y2 < world.Configuration.Height; y2++)
        {
            for (var x = 0; x < world.Configuration.Width; x++)
            {
                var cell = new LogicalGridCoordinate(x, y2);
                if (world.Grid.GetCell(cell).Passable)
                    return cell;
            }
        }

        throw new InvalidOperationException("No passable land cell found for population spawn.");
    }

    private static LogicalGridCoordinate NextLandCell(
        LogicalWorld world,
        GridNavigator navigator,
        LogicalGridCoordinate origin,
        int index,
        HashSet<LogicalGridCoordinate> used)
    {
        var dx = (index % 5) - 2;
        var dy = (index / 5) % 5 - 2;
        var candidate = world.Topology.Resolve(origin.X + dx, origin.Y + dy);
        if (candidate.TryGetCell(out var cell) && world.Grid.GetCell(cell).Passable && used.Add(cell))
            return cell;

        foreach (var neighbor in navigator.PassableNeighbors(origin))
        {
            if (used.Add(neighbor))
                return neighbor;
        }

        return origin;
    }
}

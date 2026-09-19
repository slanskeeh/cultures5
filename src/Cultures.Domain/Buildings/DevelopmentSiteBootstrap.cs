using Cultures.Population;
using Cultures.World;

namespace Cultures.Buildings;

/// <summary>
/// Temporary development layout. Not a settlement.
/// </summary>
public static class DevelopmentSiteBootstrap
{
    public static void Place(BuildingPlacementSystem placement, LogicalWorld world)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(world);

        var origin = PopulationSpawner.FindLandOrigin(world);
        var types = new[]
        {
            BuildingTypeId.Storage,
            BuildingTypeId.Shelter,
            BuildingTypeId.Farm,
            BuildingTypeId.Farm,
            BuildingTypeId.Workshop
        };

        var used = new HashSet<LogicalGridCoordinate>();
        foreach (var type in types)
        {
            if (!TryPlaceNear(placement, world, origin, type, used))
                throw new InvalidOperationException($"Could not place development building '{type.Value}'.");
        }
    }

    private static bool TryPlaceNear(
        BuildingPlacementSystem placement,
        LogicalWorld world,
        LogicalGridCoordinate origin,
        BuildingTypeId type,
        HashSet<LogicalGridCoordinate> used)
    {
        foreach (var candidate in Candidates(world, origin))
        {
            if (!used.Add(candidate))
                continue;
            var result = placement.TryPlace(type, candidate);
            if (result.Success)
                return true;
            used.Remove(candidate);
        }

        return false;
    }

    private static IEnumerable<LogicalGridCoordinate> Candidates(LogicalWorld world, LogicalGridCoordinate origin)
    {
        var preferred = new (int Dx, int Dy)[]
        {
            (5, 4), (5, -4), (8, 5), (8, -5), (9, 0),
            (4, 0), (10, 4), (10, -4), (7, 6)
        };
        foreach (var (dx, dy) in preferred)
        {
            var resolution = world.Topology.Resolve(origin.X + dx, origin.Y + dy);
            if (resolution.TryGetCell(out var cell))
                yield return cell;
        }

        for (var radius = 4; radius <= 24; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    var resolution = world.Topology.Resolve(origin.X + dx, origin.Y + dy);
                    if (resolution.TryGetCell(out var cell))
                        yield return cell;
                }
            }
        }
    }
}

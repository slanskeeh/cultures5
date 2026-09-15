using Cultures.Core.Time;

namespace Cultures.World;

/// <summary>
/// Provisional LOD radii and update cadences (OD-023). Not final game design.
/// </summary>
public static class LodRules
{
    public const int FullRadiusChunks = 2;
    public const int ReducedRadiusChunks = 4;
    public const int AggregateRadiusChunks = 6;
    public const int ClassificationIntervalTicks = 30;
    public const int ReducedBehaviorIntervalTicks = 4;

    public static ulong AggregateIntervalTicks(SimulationCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        return calendar.TicksPerHour;
    }

    public static ulong MacroIntervalTicks(SimulationCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        return calendar.TicksPerDay;
    }
}

/// <summary>
/// Wrap-aware chunk distance and tier classification. Does not scan entities.
/// </summary>
public static class SimulationLodClassifier
{
    public static int ChebyshevDistance(ChunkCoordinate a, ChunkCoordinate b, WorldConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var dx = Math.Abs(a.X - b.X);
        dx = Math.Min(dx, configuration.ChunkCountX - dx);
        var dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy);
    }

    public static SimulationLodTier Classify(
        ChunkCoordinate chunk,
        ChunkCoordinate focus,
        WorldConfiguration configuration,
        int fullRadius = LodRules.FullRadiusChunks,
        int reducedRadius = LodRules.ReducedRadiusChunks,
        int aggregateRadius = LodRules.AggregateRadiusChunks)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var distance = ChebyshevDistance(chunk, focus, configuration);
        if (distance <= fullRadius)
            return SimulationLodTier.Full;
        if (distance <= reducedRadius)
            return SimulationLodTier.Reduced;
        if (distance <= aggregateRadius)
            return SimulationLodTier.Aggregate;
        return SimulationLodTier.Macro;
    }

    public static SimulationLodTier ClassifyNearest(
        ChunkCoordinate chunk,
        IEnumerable<ChunkCoordinate> foci,
        WorldConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(foci);
        ArgumentNullException.ThrowIfNull(configuration);
        var best = SimulationLodTier.Macro;
        var any = false;
        foreach (var focus in foci)
        {
            any = true;
            var tier = Classify(chunk, focus, configuration);
            if (tier < best)
                best = tier;
        }

        return any ? best : SimulationLodTier.Macro;
    }
}

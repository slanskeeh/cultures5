using Cultures.World;

namespace Cultures.Buildings;

/// <summary>
/// Relative cells from a building origin. Materialized through WorldTopology so X can wrap.
/// </summary>
public sealed class BuildingFootprint
{
    public BuildingFootprint(IReadOnlyList<(int Dx, int Dy)> offsets)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        if (offsets.Count == 0)
            throw new ArgumentException("Footprint must contain at least one cell.", nameof(offsets));

        Offsets = offsets;
    }

    public IReadOnlyList<(int Dx, int Dy)> Offsets { get; }

    public static BuildingFootprint Cell1x1 { get; } = Rect(1, 1);

    public static BuildingFootprint Rect(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Footprint size must be positive.");

        var offsets = new List<(int, int)>(width * height);
        for (var dy = 0; dy < height; dy++)
        {
            for (var dx = 0; dx < width; dx++)
                offsets.Add((dx, dy));
        }

        return new BuildingFootprint(offsets);
    }

    public bool TryMaterialize(
        WorldTopology topology,
        LogicalGridCoordinate origin,
        out List<LogicalGridCoordinate> cells)
    {
        ArgumentNullException.ThrowIfNull(topology);
        cells = new List<LogicalGridCoordinate>(Offsets.Count);
        foreach (var (dx, dy) in Offsets)
        {
            var resolution = topology.Resolve(origin.X + dx, origin.Y + dy);
            if (!resolution.TryGetCell(out var cell))
            {
                cells.Clear();
                return false;
            }

            cells.Add(cell);
        }

        return true;
    }
}

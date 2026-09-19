namespace Cultures.World;

/// <summary>
/// Occupancy overlay on generated terrain. Does not allocate the whole planet.
/// Generated geography comes from <see cref="WorldGenerator"/> on demand.
/// </summary>
public sealed class LogicalGrid
{
    private readonly Dictionary<(int X, int Y), Occupancy> _occupancy = new();

    public LogicalGrid(WorldTopology topology, WorldGenerator generator)
    {
        Topology = topology ?? throw new ArgumentNullException(nameof(topology));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    public WorldTopology Topology { get; }
    public WorldGenerator Generator { get; }
    public WorldConfiguration Configuration => Topology.Configuration;

    public bool IsInside(WorldCoordinate position) => Topology.IsInside(position);

    public TerrainCell GetCell(LogicalGridCoordinate cell)
    {
        var generated = Generator.SampleFromChunk(cell);
        _occupancy.TryGetValue((cell.X, cell.Y), out var occupancy);
        return generated.WithOccupancy(occupancy);
    }

    public bool TryGetCell(WorldCoordinate position, out TerrainCell cell)
    {
        if (!Topology.TryGetCell(position, out var grid))
        {
            cell = default;
            return false;
        }

        cell = GetCell(grid);
        return true;
    }

    public bool TryGetOccupancy(WorldCoordinate position, out Occupancy occupancy)
    {
        if (!TryGetCell(position, out var cell))
        {
            occupancy = Occupancy.Empty;
            return false;
        }

        occupancy = cell.Occupancy;
        return true;
    }

    public bool TrySetOccupancy(WorldCoordinate position, Occupancy occupancy)
    {
        if (!Topology.TryGetCell(position, out var grid))
            return false;

        if (occupancy.IsOccupied)
            _occupancy[(grid.X, grid.Y)] = occupancy;
        else
            _occupancy.Remove((grid.X, grid.Y));

        return true;
    }

    public bool TryClearOccupancy(WorldCoordinate position) => TrySetOccupancy(position, Occupancy.Empty);

    public bool TryIsOccupied(WorldCoordinate position, out bool occupied)
    {
        if (!TryGetOccupancy(position, out var occupancy))
        {
            occupied = false;
            return false;
        }

        occupied = occupancy.IsOccupied;
        return true;
    }

    public IEnumerable<(LogicalGridCoordinate Cell, Occupancy Occupancy)> OccupancyEntries
    {
        get
        {
            foreach (var pair in _occupancy)
                yield return (new LogicalGridCoordinate(pair.Key.X, pair.Key.Y), pair.Value);
        }
    }
}

namespace Cultures.World;

/// <summary>
/// Authoritative in-memory logical grid. Independent of Godot TileMap.
/// Phase 1 stores a dense array; chunk streaming is deferred.
/// </summary>
public sealed class LogicalGrid
{
    private readonly TerrainCell[,] _cells;

    public LogicalGrid(WorldTopology topology)
    {
        Topology = topology ?? throw new ArgumentNullException(nameof(topology));
        var cfg = topology.Configuration;
        _cells = new TerrainCell[cfg.Width, cfg.Height];
        for (var x = 0; x < cfg.Width; x++)
        {
            for (var y = 0; y < cfg.Height; y++)
                _cells[x, y] = TerrainCell.Empty;
        }
    }

    public WorldTopology Topology { get; }
    public WorldConfiguration Configuration => Topology.Configuration;

    public bool IsInside(WorldCoordinate position) => Topology.IsInside(position);

    public TerrainCell GetCell(LogicalGridCoordinate cell) => _cells[cell.X, cell.Y];

    public bool TryGetCell(WorldCoordinate position, out TerrainCell cell)
    {
        if (!Topology.TryGetCell(position, out var grid))
        {
            cell = default;
            return false;
        }

        cell = _cells[grid.X, grid.Y];
        return true;
    }

    public void SetCell(LogicalGridCoordinate cell, TerrainCell value) => _cells[cell.X, cell.Y] = value;

    public bool TrySetCell(WorldCoordinate position, TerrainCell value)
    {
        if (!Topology.TryGetCell(position, out var grid))
            return false;

        _cells[grid.X, grid.Y] = value;
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
        if (!TryGetCell(position, out var cell))
            return false;

        if (!Topology.TryGetCell(position, out var grid))
            return false;

        _cells[grid.X, grid.Y] = cell.WithOccupancy(occupancy);
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
}

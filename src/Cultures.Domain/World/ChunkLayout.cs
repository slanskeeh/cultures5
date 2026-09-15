namespace Cultures.World;

/// <summary>
/// Deterministic conversion between logical cells and chunk/local coordinates.
/// Horizontal wrapping is applied before chunk resolution.
/// </summary>
public sealed class ChunkLayout
{
    public ChunkLayout(WorldTopology topology)
    {
        Topology = topology ?? throw new ArgumentNullException(nameof(topology));
    }

    public WorldTopology Topology { get; }
    public WorldConfiguration Configuration => Topology.Configuration;

    public bool TryResolve(WorldCoordinate world, out ChunkAddress address)
    {
        if (!Topology.TryGetCell(world, out var cell))
        {
            address = default;
            return false;
        }

        address = ToAddress(cell);
        return true;
    }

    public ChunkAddress ToAddress(LogicalGridCoordinate cell)
    {
        var cfg = Configuration;
        var chunk = new ChunkCoordinate(cell.X / cfg.ChunkWidth, cell.Y / cfg.ChunkHeight);
        var local = new ChunkLocalCoordinate(cell.X % cfg.ChunkWidth, cell.Y % cfg.ChunkHeight);
        return new ChunkAddress(chunk, local);
    }

    public bool TryToCell(ChunkCoordinate chunk, ChunkLocalCoordinate local, out LogicalGridCoordinate cell)
    {
        var cfg = Configuration;
        if (chunk.X < 0 || chunk.X >= cfg.ChunkCountX
            || chunk.Y < 0 || chunk.Y >= cfg.ChunkCountY
            || local.X < 0 || local.X >= cfg.ChunkWidth
            || local.Y < 0 || local.Y >= cfg.ChunkHeight)
        {
            cell = default;
            return false;
        }

        cell = new LogicalGridCoordinate(
            chunk.X * cfg.ChunkWidth + local.X,
            chunk.Y * cfg.ChunkHeight + local.Y);
        return true;
    }
}

namespace Cultures.World;

/// <summary>
/// Position in world cell units. X may be any integer (including negative);
/// Y may be outside [0, Height). Normalization lives in <see cref="WorldTopology"/>.
/// </summary>
public readonly record struct WorldCoordinate(int X, int Y)
{
    public override string ToString() => $"World({X},{Y})";
}

/// <summary>
/// A cell known to lie on the logical grid after horizontal wrap and vertical bounds checks.
/// Phase 1 uses a 1:1 mapping from a valid world cell to a grid cell (OD-002 still open).
/// </summary>
public readonly record struct LogicalGridCoordinate(int X, int Y)
{
    public WorldCoordinate ToWorld() => new(X, Y);
    public override string ToString() => $"Grid({X},{Y})";
}

/// <summary>
/// Chunk index in the world chunk lattice. Not a persistent <c>ChunkId</c>.
/// </summary>
public readonly record struct ChunkCoordinate(int X, int Y)
{
    public override string ToString() => $"Chunk({X},{Y})";
}

/// <summary>
/// Cell offset inside a chunk. Valid range is [0, ChunkWidth) x [0, ChunkHeight).
/// </summary>
public readonly record struct ChunkLocalCoordinate(int X, int Y)
{
    public override string ToString() => $"Local({X},{Y})";
}

public readonly record struct ChunkAddress(ChunkCoordinate Chunk, ChunkLocalCoordinate Local)
{
    public override string ToString() => $"{Chunk} {Local}";
}

/// <summary>
/// Non-authoritative isometric space. Never used as simulation position.
/// </summary>
public readonly record struct IsometricRenderCoordinate(int X, int Y)
{
    public override string ToString() => $"Iso({X},{Y})";
}

/// <summary>
/// Non-authoritative screen pixel space.
/// </summary>
public readonly record struct ScreenCoordinate(int X, int Y)
{
    public override string ToString() => $"Screen({X},{Y})";
}

/// <summary>
/// Result of wrapping X and checking Y. Invalid Y is not clamped.
/// </summary>
public readonly record struct CoordinateResolution(
    WorldCoordinate Input,
    WorldCoordinate HorizontallyNormalized,
    bool IsInsideWorld)
{
    public bool TryGetCell(out LogicalGridCoordinate cell)
    {
        if (!IsInsideWorld)
        {
            cell = default;
            return false;
        }

        cell = new LogicalGridCoordinate(HorizontallyNormalized.X, HorizontallyNormalized.Y);
        return true;
    }
}

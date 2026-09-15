namespace Cultures.World;

/// <summary>
/// Configurable logical world size. Exact production dimensions are still open (OD-001).
/// Width/height must be divisible by chunk size so every chunk is rectangular and complete.
/// </summary>
public sealed class WorldConfiguration : IEquatable<WorldConfiguration>
{
    public const int DebugWidth = 100;
    public const int DebugHeight = 50;
    public const int DebugChunkWidth = 10;
    public const int DebugChunkHeight = 10;

    /// <summary>
    /// Small deterministic sample used by tests and the Phase 1 debug shell. Not the final game size.
    /// </summary>
    public static WorldConfiguration DebugSample { get; } = Create(
        DebugWidth,
        DebugHeight,
        DebugChunkWidth,
        DebugChunkHeight);

    private WorldConfiguration(int width, int height, int chunkWidth, int chunkHeight)
    {
        Width = width;
        Height = height;
        ChunkWidth = chunkWidth;
        ChunkHeight = chunkHeight;
    }

    public int Width { get; }
    public int Height { get; }
    public int ChunkWidth { get; }
    public int ChunkHeight { get; }
    public int ChunkCountX => Width / ChunkWidth;
    public int ChunkCountY => Height / ChunkHeight;

    public static WorldConfiguration Create(int width, int height, int chunkWidth, int chunkHeight)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "World width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "World height must be positive.");
        if (chunkWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkWidth), "Chunk width must be positive.");
        if (chunkHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkHeight), "Chunk height must be positive.");
        if (width % chunkWidth != 0)
            throw new ArgumentException("World width must be divisible by chunk width.", nameof(chunkWidth));
        if (height % chunkHeight != 0)
            throw new ArgumentException("World height must be divisible by chunk height.", nameof(chunkHeight));

        return new WorldConfiguration(width, height, chunkWidth, chunkHeight);
    }

    public bool Equals(WorldConfiguration? other) =>
        other is not null
        && Width == other.Width
        && Height == other.Height
        && ChunkWidth == other.ChunkWidth
        && ChunkHeight == other.ChunkHeight;

    public override bool Equals(object? obj) => Equals(obj as WorldConfiguration);
    public override int GetHashCode() => HashCode.Combine(Width, Height, ChunkWidth, ChunkHeight);
    public override string ToString() => $"World {Width}x{Height}, chunk {ChunkWidth}x{ChunkHeight}";
}

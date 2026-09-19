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
    public const int PlaytestWidth = 512;
    public const int PlaytestHeight = 256;
    public const int PlaytestChunkWidth = 16;
    public const int PlaytestChunkHeight = 16;

    /// <summary>
    /// Small deterministic sample used by tests. Not the playable world.
    /// </summary>
    public static WorldConfiguration DebugSample { get; } = Create(
        DebugWidth,
        DebugHeight,
        DebugChunkWidth,
        DebugChunkHeight);

    /// <summary>
    /// Current playable world. Noise is cycles-per-world, so biomes scale with these dimensions (AD-127).
    /// Tests keep <see cref="DebugSample"/>. Final production size remains OD-001.
    /// </summary>
    public static WorldConfiguration Playtest { get; } = Create(
        PlaytestWidth,
        PlaytestHeight,
        PlaytestChunkWidth,
        PlaytestChunkHeight);

    private WorldConfiguration(
        int width,
        int height,
        int chunkWidth,
        int chunkHeight,
        int generationVersion,
        float seaLevel,
        float polarBand)
    {
        Width = width;
        Height = height;
        ChunkWidth = chunkWidth;
        ChunkHeight = chunkHeight;
        GenerationVersion = generationVersion;
        SeaLevel = seaLevel;
        PolarBand = polarBand;
    }

    public int Width { get; }
    public int Height { get; }
    public int ChunkWidth { get; }
    public int ChunkHeight { get; }
    public int ChunkCountX => Width / ChunkWidth;
    public int ChunkCountY => Height / ChunkHeight;
    public int GenerationVersion { get; }
    public float SeaLevel { get; }
    public float PolarBand { get; }

    public static WorldConfiguration Create(
        int width,
        int height,
        int chunkWidth,
        int chunkHeight,
        int generationVersion = WorldGeneration.CurrentVersion,
        float seaLevel = WorldGeneration.DefaultSeaLevel,
        float polarBand = WorldGeneration.DefaultPolarBand)
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
        if (generationVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(generationVersion), "Generation version must be positive.");
        if (seaLevel is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(seaLevel), "Sea level must be in [0, 1].");
        if (polarBand is < 0f or > 0.5f)
            throw new ArgumentOutOfRangeException(nameof(polarBand), "Polar band must be in [0, 0.5].");

        return new WorldConfiguration(width, height, chunkWidth, chunkHeight, generationVersion, seaLevel, polarBand);
    }

    public WorldConfiguration WithGenerationVersion(int generationVersion) =>
        Create(Width, Height, ChunkWidth, ChunkHeight, generationVersion, SeaLevel, PolarBand);

    public bool Equals(WorldConfiguration? other) =>
        other is not null
        && Width == other.Width
        && Height == other.Height
        && ChunkWidth == other.ChunkWidth
        && ChunkHeight == other.ChunkHeight
        && GenerationVersion == other.GenerationVersion
        && SeaLevel.Equals(other.SeaLevel)
        && PolarBand.Equals(other.PolarBand);

    public override bool Equals(object? obj) => Equals(obj as WorldConfiguration);
    public override int GetHashCode() =>
        HashCode.Combine(Width, Height, ChunkWidth, ChunkHeight, GenerationVersion, SeaLevel, PolarBand);
    public override string ToString() =>
        $"World {Width}x{Height}, chunk {ChunkWidth}x{ChunkHeight}, gen v{GenerationVersion}";
}

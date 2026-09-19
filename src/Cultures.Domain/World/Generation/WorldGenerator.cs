namespace Cultures.World;

/// <summary>
/// On-demand deterministic generator. Samples are a pure function of
/// seed + generation version + configuration + cell coordinates.
/// </summary>
public sealed class WorldGenerator
{
    private readonly Dictionary<ChunkCoordinate, GeneratedTerrain[]> _chunks = new();

    public WorldGenerator(ulong seed, WorldConfiguration configuration, ChunkLayout chunks)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
        Seed = seed;
        NoiseSeed = LatticeNoise.Mix(seed, (ulong)(uint)configuration.GenerationVersion);
    }

    public ulong Seed { get; }
    public WorldConfiguration Configuration { get; }
    public ChunkLayout Chunks { get; }
    public int CachedChunkCount => _chunks.Count;
    public bool IsCached(ChunkCoordinate chunk) => _chunks.ContainsKey(chunk);
    internal ulong NoiseSeed { get; }

    public GeneratedTerrain Sample(LogicalGridCoordinate cell)
    {
        var x = cell.X;
        var y = cell.Y;
        var latitude = Chunks.Topology.Latitude01(y);

        var macro = SampleWrapped(x, y, 1.15f, 1.35f, 1);
        var hills = SampleWrapped(x, y, 2.55f, 2.70f, 2);
        var detail = SampleWrapped(x, y, 5.10f, 5.40f, 3);
        var moistNoise = SampleWrapped(x, y, 2.05f, 2.15f, 4);

        var landMass = Smoothstep(0.36f, 0.58f, macro);
        var elevation = Clamp01(
            0.20f
            + landMass * 0.50f
            + (hills - 0.5f) * 0.22f
            + (detail - 0.5f) * 0.08f);

        var isWater = elevation < Configuration.SeaLevel;

        var poleDistance = MathF.Min(latitude, 1f - latitude);
        var latTemperature = MathF.Cos((latitude - 0.5f) * MathF.PI) * 0.85f + 0.08f;
        var temperature = Clamp01(latTemperature - elevation * 0.35f);
        if (poleDistance < Configuration.PolarBand)
            temperature = MathF.Min(temperature, 0.22f);

        var equator = 1f - MathF.Abs(latitude - 0.5f) * 2f;
        var moisture = Clamp01(0.22f + equator * 0.48f + (moistNoise - 0.5f) * 0.36f);
        var climate = new ClimateSample(temperature, moisture);
        var biome = Classify(elevation, isWater, climate);
        var riverNoise = SampleWrapped(x, y, 3.40f, 3.55f, 5);
        var hasRiver = !isWater
            && moisture >= 0.50f
            && elevation >= Configuration.SeaLevel
            && elevation < 0.78f
            && riverNoise < 0.18f;
        var fertility = EcologyRules.Fertility(new GeneratedTerrain(elevation, isWater, climate, biome, hasRiver));
        return new GeneratedTerrain(elevation, isWater, climate, biome, hasRiver, fertility);
    }

    public GeneratedTerrain[] GetChunk(ChunkCoordinate chunk)
    {
        if (_chunks.TryGetValue(chunk, out var cached))
            return cached;

        var cfg = Configuration;
        var cells = new GeneratedTerrain[cfg.ChunkWidth * cfg.ChunkHeight];
        for (var ly = 0; ly < cfg.ChunkHeight; ly++)
        {
            for (var lx = 0; lx < cfg.ChunkWidth; lx++)
            {
                if (!Chunks.TryToCell(chunk, new ChunkLocalCoordinate(lx, ly), out var cell))
                    throw new InvalidOperationException($"Invalid chunk local {chunk} ({lx},{ly}).");

                cells[ly * cfg.ChunkWidth + lx] = Sample(cell);
            }
        }

        _chunks[chunk] = cells;
        return cells;
    }

    public GeneratedTerrain SampleFromChunk(LogicalGridCoordinate cell)
    {
        var address = Chunks.ToAddress(cell);
        var data = GetChunk(address.Chunk);
        return data[address.Local.Y * Configuration.ChunkWidth + address.Local.X];
    }

    /// <summary>
    /// Provisional classifier. Thresholds are temporary and must not be treated as final design.
    /// </summary>
    public static BiomeId Classify(float elevation, bool isWater, ClimateSample climate)
    {
        if (isWater)
            return climate.Temperature <= 0.22f ? BiomeId.Ice : BiomeId.Ocean;

        if (climate.Temperature <= 0.22f)
            return elevation > 0.72f ? BiomeId.Ice : BiomeId.Tundra;

        if (elevation >= 0.72f)
            return BiomeId.Highland;

        if (climate.Temperature >= 0.62f && climate.Moisture <= 0.38f)
            return BiomeId.Desert;

        if (climate.Moisture >= 0.55f && climate.Temperature >= 0.32f)
        {
            if (climate.Temperature <= 0.38f)
                return BiomeId.Taiga;
            if (climate.Moisture >= 0.68f && elevation < 0.46f)
                return BiomeId.Swamp;
            return BiomeId.Forest;
        }

        if (climate.Temperature >= 0.52f && climate.Moisture is >= 0.38f and <= 0.54f)
            return BiomeId.Savanna;

        return BiomeId.TemperateLand;
    }

    /// <summary>
    /// Frequency is waves around the world (X) / across latitude (Y), not per cell.
    /// Larger Width/Height therefore produce larger continents and biomes in hexes.
    /// </summary>
    private float SampleWrapped(int cellX, int cellY, float frequencyX, float frequencyY, ulong salt)
    {
        var width = Configuration.Width;
        var height = Math.Max(1, Configuration.Height);
        var theta = (cellX + 0.5f) / width * MathF.Tau;
        var nx = MathF.Cos(theta) * frequencyX;
        var ny = MathF.Sin(theta) * frequencyX;
        var nz = (cellY + 0.5f) / height * frequencyY;
        return LatticeNoise.Value3(nx, ny, nz, LatticeNoise.Mix(NoiseSeed, salt));
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private static float Smoothstep(float edge0, float edge1, float value)
    {
        var t = Clamp01((value - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }
}

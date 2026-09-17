using Cultures.World;

namespace Cultures.Exploration;

public readonly record struct TerrainKnowledge(
    bool Known,
    bool HasLand,
    bool HasWater,
    float? MeanElevation,
    int? LandCells,
    int? WaterCells);

public readonly record struct BiomeKnowledge(bool Known, BiomeId? Dominant, int DistinctCount);

public readonly record struct ClimateKnowledge(bool Known, float? Temperature, float? Moisture);

public readonly record struct DiscoveryNote(DiscoveryKind Kind, ulong Tick);

/// <summary>
/// Read-only summary of one generated chunk. Not player knowledge.
/// </summary>
public readonly record struct ChunkGeographySample(
    bool HasLand,
    bool HasWater,
    float MeanElevation,
    float MeanTemperature,
    float MeanMoisture,
    BiomeId DominantBiome,
    int DistinctBiomes,
    int LandCells,
    int WaterCells);

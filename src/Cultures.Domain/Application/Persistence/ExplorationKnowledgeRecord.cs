using Cultures.Exploration;
using Cultures.World;

namespace Cultures.Application.Persistence;

/// <summary>
/// Serialization seam for chunk knowledge. Not wired into save envelope v2.
/// </summary>
public sealed record ExplorationKnowledgeRecord(
    int ChunkX,
    int ChunkY,
    byte Level,
    byte Source,
    ulong DiscoveredTick,
    ulong UpdatedTick,
    bool TerrainKnown,
    bool HasLand,
    bool HasWater,
    float? MeanElevation,
    bool BiomeKnown,
    byte? DominantBiome,
    bool ClimateKnown,
    float? Temperature,
    float? Moisture);

public static class ExplorationKnowledgeMapper
{
    public static ExplorationKnowledgeRecord ToRecord(ChunkExplorationKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        return new ExplorationKnowledgeRecord(
            knowledge.Coordinate.X,
            knowledge.Coordinate.Y,
            (byte)knowledge.Level,
            (byte)knowledge.Source,
            knowledge.DiscoveredTick,
            knowledge.UpdatedTick,
            knowledge.Terrain.Known,
            knowledge.Terrain.HasLand,
            knowledge.Terrain.HasWater,
            knowledge.Terrain.MeanElevation,
            knowledge.Biome.Known,
            knowledge.Biome.Dominant is { } biome ? (byte)biome : null,
            knowledge.Climate.Known,
            knowledge.Climate.Temperature,
            knowledge.Climate.Moisture);
    }

    public static ChunkExplorationKnowledge FromRecord(ExplorationKnowledgeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var knowledge = new ChunkExplorationKnowledge(new ChunkCoordinate(record.ChunkX, record.ChunkY))
        {
            Level = (ExplorationKnowledgeLevel)record.Level,
            Source = (ExplorationSource)record.Source,
            DiscoveredTick = record.DiscoveredTick,
            UpdatedTick = record.UpdatedTick,
            Terrain = new TerrainKnowledge(
                record.TerrainKnown,
                record.HasLand,
                record.HasWater,
                record.MeanElevation,
                null,
                null),
            Biome = new BiomeKnowledge(
                record.BiomeKnown,
                record.DominantBiome is { } biome ? (BiomeId)biome : null,
                0),
            Climate = new ClimateKnowledge(record.ClimateKnown, record.Temperature, record.Moisture)
        };
        return knowledge;
    }
}

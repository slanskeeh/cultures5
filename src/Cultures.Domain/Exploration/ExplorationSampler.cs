using Cultures.World;

namespace Cultures.Exploration;

/// <summary>
/// Read-only geography sample for one chunk. Generates that chunk on demand only.
/// </summary>
public static class ExplorationSampler
{
    public static ChunkGeographySample Sample(LogicalWorld world, ChunkCoordinate chunk)
    {
        ArgumentNullException.ThrowIfNull(world);
        var cells = world.Generator.GetChunk(chunk);
        var land = 0;
        var water = 0;
        var elevation = 0f;
        var temperature = 0f;
        var moisture = 0f;
        var biomes = new Dictionary<BiomeId, int>();
        foreach (var cell in cells)
        {
            if (cell.IsWater)
                water++;
            else
                land++;
            elevation += cell.Elevation;
            temperature += cell.Climate.Temperature;
            moisture += cell.Climate.Moisture;
            biomes[cell.Biome] = biomes.GetValueOrDefault(cell.Biome) + 1;
        }

        var count = cells.Length;
        var dominant = biomes.Count == 0
            ? BiomeId.Ocean
            : biomes.OrderByDescending(p => p.Value).ThenBy(p => (byte)p.Key).First().Key;
        return new ChunkGeographySample(
            land > 0,
            water > 0,
            count == 0 ? 0f : elevation / count,
            count == 0 ? 0f : temperature / count,
            count == 0 ? 0f : moisture / count,
            dominant,
            biomes.Count,
            land,
            water);
    }
}

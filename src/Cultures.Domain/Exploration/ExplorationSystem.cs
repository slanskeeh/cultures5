using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Exploration;

/// <summary>
/// Player knowledge about the world. Does not mutate terrain, LOD, or presentation.
/// </summary>
public sealed class ExplorationSystem
{
    public ExplorationSystem(LogicalWorld world, SimulationClock clock, EventBus events)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Knowledge = new ExplorationKnowledgeDirectory();
    }

    public LogicalWorld World { get; }
    public SimulationClock Clock { get; }
    public EventBus Events { get; }
    public ExplorationKnowledgeDirectory Knowledge { get; }

    public ExplorationKnowledgeLevel LevelOf(ChunkCoordinate chunk) =>
        Knowledge.TryGet(chunk, out var record) ? record.Level : ExplorationKnowledgeLevel.Unknown;

    public ChunkExplorationSnapshot GetKnowledge(ChunkCoordinate chunk)
    {
        if (Knowledge.TryGet(chunk, out var record))
            return record.Snapshot();
        return new ChunkExplorationSnapshot(
            chunk,
            0,
            ExplorationKnowledgeLevel.Unknown,
            ExplorationSource.None,
            0,
            0,
            default,
            default,
            default,
            0);
    }

    public KnownExplorationFacts GetKnownFacts(ChunkCoordinate chunk)
    {
        var snapshot = GetKnowledge(chunk);
        return new KnownExplorationFacts(snapshot.Level, snapshot.Terrain, snapshot.Biome, snapshot.Climate);
    }

    public bool TryAdvance(ChunkCoordinate chunk, ExplorationKnowledgeLevel target, out string error)
    {
        error = "Exploration failed.";
        if (!IsInside(chunk))
        {
            error = "Chunk is outside the world.";
            return false;
        }

        var current = LevelOf(chunk);
        if (current == target)
        {
            error = $"Chunk is already {target}.";
            return false;
        }

        if (target < current)
        {
            error = "Knowledge cannot decrease.";
            return false;
        }

        if (!current.CanAdvanceTo(target))
        {
            error = $"Cannot advance from {current} to {target}.";
            return false;
        }

        var record = Knowledge.GetOrCreate(chunk);
        var previous = record.Level;
        record.Level = target;
        record.Source = SourceFor(target);
        record.UpdatedTick = Clock.Tick;
        if (previous == ExplorationKnowledgeLevel.Unknown)
            record.DiscoveredTick = Clock.Tick;
        ApplyFacts(record, target);
        Events.Publish(new ChunkKnowledgeChangedEvent(Clock.Tick, chunk, previous, target));
        return true;
    }

    private void ApplyFacts(ChunkExplorationKnowledge record, ExplorationKnowledgeLevel target)
    {
        if (target == ExplorationKnowledgeLevel.Rumored)
            return;

        var sample = ExplorationSampler.Sample(World, record.Coordinate);
        if (target >= ExplorationKnowledgeLevel.Scouted)
        {
            record.Terrain = new TerrainKnowledge(
                true,
                sample.HasLand,
                sample.HasWater,
                target >= ExplorationKnowledgeLevel.Mapped ? sample.MeanElevation : record.Terrain.MeanElevation,
                target >= ExplorationKnowledgeLevel.Analyzed ? sample.LandCells : record.Terrain.LandCells,
                target >= ExplorationKnowledgeLevel.Analyzed ? sample.WaterCells : record.Terrain.WaterCells);
        }

        if (target >= ExplorationKnowledgeLevel.Mapped)
            record.Biome = new BiomeKnowledge(true, sample.DominantBiome, sample.DistinctBiomes);

        if (target >= ExplorationKnowledgeLevel.Confirmed)
            record.Climate = new ClimateKnowledge(true, sample.MeanTemperature, sample.MeanMoisture);
    }

    private bool IsInside(ChunkCoordinate chunk)
    {
        var cfg = World.Configuration;
        return chunk.X >= 0 && chunk.X < cfg.ChunkCountX
            && chunk.Y >= 0 && chunk.Y < cfg.ChunkCountY;
    }

    private static ExplorationSource SourceFor(ExplorationKnowledgeLevel level) => level switch
    {
        ExplorationKnowledgeLevel.Rumored => ExplorationSource.DebugRumor,
        ExplorationKnowledgeLevel.Scouted => ExplorationSource.Scout,
        ExplorationKnowledgeLevel.Mapped => ExplorationSource.Map,
        ExplorationKnowledgeLevel.Confirmed => ExplorationSource.Confirm,
        ExplorationKnowledgeLevel.Analyzed => ExplorationSource.Analyze,
        _ => ExplorationSource.None
    };
}

public readonly record struct KnownExplorationFacts(
    ExplorationKnowledgeLevel Level,
    TerrainKnowledge Terrain,
    BiomeKnowledge Biome,
    ClimateKnowledge Climate);

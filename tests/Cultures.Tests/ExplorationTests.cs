using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Exploration;
using Cultures.World;

namespace Cultures.Tests;

public sealed class ExplorationProgressionTests
{
    [Fact]
    public void Missing_chunk_is_unknown_and_sparse()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = new ChunkCoordinate(1, 2);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(chunk));
        Assert.Equal(0, host.Exploration.Knowledge.Count);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.GetKnownFacts(chunk).Level);
        Assert.False(host.Exploration.GetKnownFacts(chunk).Terrain.Known);
        Assert.Equal(0, host.Exploration.Knowledge.Count);
    }

    [Fact]
    public void Knowledge_advances_monotonically_through_commands()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Cursor.Position).Chunk;
        Assert.True(host.Commands.Execute(new RumorChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Rumored, host.Exploration.LevelOf(chunk));
        Assert.False(host.Exploration.GetKnownFacts(chunk).Terrain.Known);

        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
        Assert.True(host.Exploration.GetKnownFacts(chunk).Terrain.Known);
        Assert.False(host.Exploration.GetKnownFacts(chunk).Biome.Known);

        Assert.True(host.Commands.Execute(new MapChunkCommand(chunk)).Success);
        Assert.True(host.Exploration.GetKnownFacts(chunk).Biome.Known);

        Assert.True(host.Commands.Execute(new ConfirmChunkCommand(chunk)).Success);
        Assert.True(host.Exploration.GetKnownFacts(chunk).Climate.Known);

        Assert.True(host.Commands.Execute(new AnalyzeChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Analyzed, host.Exploration.LevelOf(chunk));
        Assert.True(host.Exploration.GetKnownFacts(chunk).Terrain.LandCells >= 0);
        Assert.Equal(1, host.Exploration.Knowledge.Count);
    }

    [Fact]
    public void Rumor_does_not_generate_the_chunk()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var cfg = host.World.Configuration;
        ChunkCoordinate? uncached = null;
        for (var y = 0; y < cfg.ChunkCountY && uncached is null; y++)
        {
            for (var x = 0; x < cfg.ChunkCountX && uncached is null; x++)
            {
                var candidate = new ChunkCoordinate(x, y);
                if (!host.World.Generator.IsCached(candidate))
                    uncached = candidate;
            }
        }

        Assert.True(uncached.HasValue);
        var chunk = uncached.Value;
        var before = host.World.Generator.CachedChunkCount;
        Assert.True(host.Commands.Execute(new RumorChunkCommand(chunk)).Success);
        Assert.Equal(before, host.World.Generator.CachedChunkCount);
        Assert.False(host.World.Generator.IsCached(chunk));
        Assert.False(host.Exploration.GetKnownFacts(chunk).Terrain.Known);

        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        Assert.True(host.World.Generator.IsCached(chunk));
        Assert.Equal(before + 1, host.World.Generator.CachedChunkCount);
    }

    [Fact]
    public void Scout_can_skip_rumor()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Cursor.Position).Chunk;
        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
    }

    [Fact]
    public void Illegal_transitions_fail_and_do_not_decrease()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Cursor.Position).Chunk;
        Assert.False(host.Commands.Execute(new MapChunkCommand(chunk)).Success);
        Assert.False(host.Commands.Execute(new ConfirmChunkCommand(chunk)).Success);
        Assert.False(host.Commands.Execute(new AnalyzeChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(chunk));

        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        Assert.False(host.Commands.Execute(new ConfirmChunkCommand(chunk)).Success);
        Assert.False(host.Commands.Execute(new RumorChunkCommand(chunk)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
        Assert.False(host.Exploration.TryAdvance(chunk, ExplorationKnowledgeLevel.Unknown, out _));
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
    }
}

public sealed class ExplorationIndependenceTests
{
    [Fact]
    public void Knowledge_survives_ticks_lod_and_presentation_unload()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        host.Step(40);
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        host.Commands.Execute(new SetChunkPresentationCommand(chunk, ChunkPresentationPresence.Unloaded));
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(chunk));
        Assert.True(host.Lod.Chunks.TryGet(chunk, out var state));
        Assert.Equal(ChunkPresentationPresence.Unloaded, state.Presentation);
        Assert.Equal(SimulationLodTier.Aggregate, state.EffectiveTier);
    }

    [Fact]
    public void Exploration_does_not_mutate_settlement_directory()
    {
        var host = new SimulationHost(1);
        host.SettlementDetection.Evaluate();
        var count = host.Settlements.Count;
        var chunk = host.World.Chunks.ToAddress(host.Cursor.Position).Chunk;
        host.Commands.Execute(new ScoutChunkCommand(chunk));
        Assert.Equal(count, host.Settlements.Count);
    }

    [Fact]
    public void Exploration_does_not_mutate_terrain_or_lod_tier()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var cell = host.Cursor.Position;
        var before = host.World.Grid.GetCell(cell);
        var chunk = host.World.Chunks.ToAddress(cell).Chunk;
        var lod = host.Lod.Classify(chunk);
        Assert.True(host.Commands.Execute(new ScoutChunkCommand(chunk)).Success);
        Assert.True(host.Commands.Execute(new MapChunkCommand(chunk)).Success);
        var after = host.World.Grid.GetCell(cell);
        Assert.Equal(before.Biome, after.Biome);
        Assert.Equal(before.Elevation, after.Elevation);
        Assert.Equal(before.IsWater, after.IsWater);
        Assert.Equal(lod, host.Lod.Classify(chunk));
    }

    [Fact]
    public void Wrap_adjacent_chunks_are_independent_knowledge_records()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var west = new ChunkCoordinate(0, host.World.Configuration.Height / 2 / host.World.Configuration.ChunkHeight);
        var east = new ChunkCoordinate(host.World.Configuration.ChunkCountX - 1, west.Y);
        Assert.Equal(1, SimulationLodClassifier.ChebyshevDistance(west, east, host.World.Configuration));
        Assert.True(host.Commands.Execute(new ScoutChunkCommand(west)).Success);
        Assert.True(host.Commands.Execute(new ScoutChunkCommand(east)).Success);
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(west));
        Assert.Equal(ExplorationKnowledgeLevel.Scouted, host.Exploration.LevelOf(east));
        Assert.Equal(2, host.Exploration.Knowledge.Count);
        Assert.True(host.Exploration.Knowledge.Count < host.World.Configuration.ChunkCountX * host.World.Configuration.ChunkCountY);
    }

    [Fact]
    public void Equivalent_hosts_produce_equivalent_knowledge()
    {
        var a = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var b = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = a.World.Chunks.ToAddress(a.Cursor.Position).Chunk;
        a.Commands.Execute(new ScoutChunkCommand(chunk));
        a.Commands.Execute(new MapChunkCommand(chunk));
        b.Commands.Execute(new ScoutChunkCommand(chunk));
        b.Commands.Execute(new MapChunkCommand(chunk));
        Assert.Equal(a.Exploration.GetKnowledge(chunk), b.Exploration.GetKnowledge(chunk));
    }

    [Fact]
    public void Serialization_seam_roundtrips_a_record()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Cursor.Position).Chunk;
        host.Commands.Execute(new ScoutChunkCommand(chunk));
        Assert.True(host.Exploration.Knowledge.TryGet(chunk, out var original));
        var restored = ExplorationKnowledgeMapper.FromRecord(ExplorationKnowledgeMapper.ToRecord(original));
        Assert.Equal(original.Level, restored.Level);
        Assert.Equal(original.Terrain.HasLand, restored.Terrain.HasLand);
        Assert.Equal(original.Coordinate, restored.Coordinate);
    }
}

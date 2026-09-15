using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.Tests;

public sealed class LodClassificationTests
{
    [Fact]
    public void Distance_and_tiers_follow_configured_radii()
    {
        var config = WorldConfiguration.Create(200, 100, 10, 10);
        var focus = new ChunkCoordinate(0, 5);
        Assert.Equal(SimulationLodTier.Full, SimulationLodClassifier.Classify(new ChunkCoordinate(0, 5), focus, config));
        Assert.Equal(SimulationLodTier.Full, SimulationLodClassifier.Classify(new ChunkCoordinate(2, 5), focus, config));
        Assert.Equal(SimulationLodTier.Reduced, SimulationLodClassifier.Classify(new ChunkCoordinate(3, 5), focus, config));
        Assert.Equal(SimulationLodTier.Aggregate, SimulationLodClassifier.Classify(new ChunkCoordinate(5, 5), focus, config));
        Assert.Equal(SimulationLodTier.Macro, SimulationLodClassifier.Classify(new ChunkCoordinate(10, 5), focus, config));
    }

    [Fact]
    public void Classification_respects_horizontal_chunk_wrap()
    {
        var config = WorldConfiguration.DebugSample;
        var focus = new ChunkCoordinate(0, 2);
        var wrapped = new ChunkCoordinate(config.ChunkCountX - 1, 2);
        Assert.Equal(1, SimulationLodClassifier.ChebyshevDistance(focus, wrapped, config));
        Assert.Equal(SimulationLodTier.Full, SimulationLodClassifier.Classify(wrapped, focus, config));
    }
}

public sealed class LodTransitionTests
{
    [Fact]
    public void Aggregation_keeps_people_buildings_resources_and_ids()
    {
        var host = new SimulationHost(1, populationCount: 4);
        var person = host.Population[0];
        person.Inventory.TryAdd(ResourceType.Food, 2);
        person.Skills.SetLevel(SkillType.Farming, 11);
        var before = host.Lod.ResourceTotals();
        var ids = host.Population.All.Select(c => c.Id).ToArray();
        var buildings = host.Buildings.All.Select(b => b.Id).ToArray();
        var chunk = host.World.Chunks.ToAddress(person.Position).Chunk;

        Assert.True(host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate)).Success);
        Assert.True(person.LodTier.IsAggregate());
        Assert.Equal(ids, host.Population.All.Select(c => c.Id));
        Assert.Equal(buildings, host.Buildings.All.Select(b => b.Id));
        Assert.Equal(before, host.Lod.ResourceTotals());
        Assert.Equal(11, person.Skills.GetLevel(SkillType.Farming));
        Assert.Equal(2, person.Inventory.GetQuantity(ResourceType.Food));
        Assert.False(person.AssignedWorkplace.IsAssigned);
    }

    [Fact]
    public void Reconstruction_is_deterministic_and_restores_detailed_simulation()
    {
        var a = new SimulationHost(1, populationCount: 4);
        var b = new SimulationHost(1, populationCount: 4);
        var chunk = a.World.Chunks.ToAddress(a.Population[0].Position).Chunk;
        a.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        b.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        a.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Full));
        b.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Full));
        Assert.Equal(
            a.Population.All.Select(c => c.Snapshot()),
            b.Population.All.Select(c => c.Snapshot()));
        Assert.Equal(SimulationLodTier.Full, a.Population[0].LodTier);
        Assert.True(a.Lod.SimulateBody(a.Population[0]));
    }

    [Fact]
    public void Family_and_settlement_survive_roundtrip()
    {
        var host = new SimulationHost(1, populationCount: 6);
        host.SettlementDetection.Evaluate();
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        var settlement = host.Settlements[0].Id;
        var parents = child!.FamilyLinks.Parents.ToArray();
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Full));
        Assert.Equal(parents, child.FamilyLinks.Parents);
        Assert.True(FamilyQueries.AreParentAndChild(host.Population[0], child));
        Assert.Equal(settlement, host.Settlements[0].Id);
        Assert.Equal(host.Buildings.Count, host.Buildings.All.Count);
    }

    [Fact]
    public void Protected_character_is_not_aggregated()
    {
        var host = new SimulationHost(1, populationCount: 4);
        var person = host.Population[0];
        var other = host.Population[1];
        Assert.True(host.Commands.Execute(new ProtectCharacterCommand(person.Id, true)).Success);
        var chunk = host.World.Chunks.ToAddress(person.Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        Assert.Equal(SimulationLodTier.Full, person.LodTier);
        Assert.True(person.IsPersistentIndividual);
        Assert.True(other.LodTier.IsAggregate());
        Assert.True(host.Lod.SimulateBody(person));
        Assert.False(host.Lod.SimulateBody(other));
    }

    [Fact]
    public void Player_command_rejects_aggregated_target()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        var result = host.Commands.Execute(
            new TeachCharacterCommand(host.Population[0].Id, host.Population[1].Id, SkillType.Farming));
        Assert.False(result.Success);
        Assert.Contains("aggregate", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Aggregated_people_do_not_tick_individually()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var hunger = person.Needs.Hunger;
        var chunk = host.World.Chunks.ToAddress(person.Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        host.Step(8);
        Assert.Equal(hunger, person.Needs.Hunger);
    }

    [Fact]
    public void Aggregate_advance_changes_food_without_dropping_identities()
    {
        var host = new SimulationHost(1, populationCount: 4);
        var storage = host.Buildings.All.First(b => b.Definition.IsStorage);
        storage.Inventory.TryAdd(ResourceType.Food, 20);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        var ids = host.Population.All.Select(c => c.Id).ToArray();
        var age = host.Population[0].AgeYears;
        host.Aggregate.Advance(SimulationLodTier.Aggregate, host.Clock.Calendar.TicksPerHour);
        Assert.Equal(ids, host.Population.All.Select(c => c.Id));
        Assert.True(host.Population[0].AgeYears > age);
    }

    [Fact]
    public void Presentation_presence_is_independent_of_simulation_tier()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        host.Commands.Execute(new SetChunkPresentationCommand(chunk, ChunkPresentationPresence.Unloaded));
        Assert.True(host.Lod.Chunks.TryGet(chunk, out var state));
        Assert.Equal(SimulationLodTier.Aggregate, state.EffectiveTier);
        Assert.Equal(ChunkPresentationPresence.Unloaded, state.Presentation);
        Assert.Equal(1, host.Population.Count);
    }

    [Fact]
    public void Refresh_command_classifies_from_cursor_focus()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.True(host.Commands.Execute(new RefreshLodCommand()).Success);
        var expected = host.Lod.Classify(host.Population[0].Position);
        Assert.Equal(expected, host.Population[0].LodTier);
    }

    [Fact]
    public void Census_counts_demographics_and_skills()
    {
        var host = new SimulationHost(1, populationCount: 4, placeDevelopmentBuildings: false);
        host.Population[0].Skills.SetLevel(SkillType.Farming, 20);
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        child!.Position = host.Population[0].Position;
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Lod.Evaluate();
        Assert.True(host.Lod.Chunks.TryGet(chunk, out var state));
        Assert.Equal(host.Population.Alive.Count(c => host.World.Chunks.ToAddress(c.Position).Chunk.Equals(chunk)), state.Census.Population);
        Assert.True(state.Census.Infants >= 1);
        Assert.True(state.Census.FarmingSkillSum >= 20);
    }

    [Fact]
    public void Migration_group_is_a_seam_not_gameplay()
    {
        var group = new MigrationGroup(
            new MigrationGroupId(1),
            new SettlementId(1),
            SettlementId.None,
            12,
            CultureId.Neutral);
        group.Members.Add(new CharacterId(3));
        Assert.Equal(12, group.Population);
        Assert.Equal(CultureId.Neutral, group.Culture);
        Assert.Empty(new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false).Lod.Groups);
    }
}

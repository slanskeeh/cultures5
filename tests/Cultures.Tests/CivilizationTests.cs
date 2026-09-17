using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.Tests;

public sealed class CultureFoundationTests
{
    [Fact]
    public void Neutral_culture_exists_and_generated_ids_are_stable_and_distinct()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.True(host.Civilization.Cultures.TryGet(CultureId.Neutral, out var neutral));
        Assert.Equal("Unaffiliated", neutral.Name);
        Assert.Equal(1 + CivilizationRules.GeneratedCultureCount, host.Civilization.Cultures.Count);

        var generated = host.Civilization.Cultures.All.Where(c => c.Id != CultureId.Neutral).ToList();
        Assert.Equal(2, generated.Count);
        Assert.NotEqual(generated[0].Id, generated[1].Id);
        Assert.Equal(2UL, generated[0].Id.Value);
        Assert.Equal(3UL, generated[1].Id.Value);
        Assert.NotEqual(generated[0].Name, generated[1].Name);
        Assert.DoesNotContain("Viking", generated[0].Name, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Roman", generated[1].Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Culture_names_and_traits_are_deterministic()
    {
        var a = new SimulationHost(7, populationCount: 1, placeDevelopmentBuildings: false);
        var b = new SimulationHost(7, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(
            a.Civilization.Cultures.All.Select(c => c.Snapshot()).ToArray(),
            b.Civilization.Cultures.All.Select(c => c.Snapshot()).ToArray());
        Assert.NotEqual(
            a.Civilization.Cultures[1].Traits,
            new SimulationHost(8, populationCount: 1, placeDevelopmentBuildings: false).Civilization.Cultures[1].Traits);
    }
}

public sealed class FactionFoundationTests
{
    [Fact]
    public void Factions_reference_existing_cultures_and_are_not_settlements()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(CivilizationRules.GeneratedFactionCount, host.Civilization.Factions.Count);
        Assert.Equal(0, host.Settlements.Count);
        foreach (var faction in host.Civilization.Factions.All)
        {
            Assert.True(host.Civilization.Cultures.TryGet(faction.Culture, out _));
            Assert.NotEqual(typeof(SettlementState), faction.GetType());
            Assert.Equal(SettlementId.None, faction.HomeSettlement);
            Assert.NotEqual(0UL, faction.Id.Value);
        }

        var before = host.Settlements.Count;
        Assert.True(host.Commands.Execute(new CreateFactionCommand(CultureId.Neutral)).Success);
        Assert.Equal(before, host.Settlements.Count);
        Assert.False(host.Commands.Execute(new CreateFactionCommand(new CultureId(99))).Success);
    }

    [Fact]
    public void Faction_generation_is_deterministic_and_independent_of_exploration()
    {
        var a = new SimulationHost(3, populationCount: 1, placeDevelopmentBuildings: false);
        var b = new SimulationHost(3, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(
            a.Civilization.Factions.All.Select(f => f.Snapshot()).ToArray(),
            b.Civilization.Factions.All.Select(f => f.Snapshot()).ToArray());
        Assert.Equal(0, a.Exploration.Knowledge.Count);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, a.Exploration.LevelOf(new ChunkCoordinate(0, 0)));
        var cache = a.World.Generator.CachedChunkCount;
        Assert.True(a.Commands.Execute(new CreateCultureCommand()).Success);
        Assert.Equal(cache, a.World.Generator.CachedChunkCount);
        Assert.Equal(0, a.Exploration.Knowledge.Count);
    }
}

public sealed class FactionMembershipTests
{
    [Fact]
    public void Characters_default_unaffiliated_and_membership_uses_commands()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        Assert.Equal(CultureId.Neutral, person.Culture);
        Assert.Equal(FactionId.None, person.Faction);

        var faction = host.Civilization.Factions[0];
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, faction.Id)).Success);
        Assert.Equal(faction.Id, person.Faction);
        Assert.Equal(1, host.Civilization.CountMembers(faction.Id));
        Assert.True(host.Commands.Execute(new AssignCultureCommand(person.Id, faction.Culture)).Success);
        Assert.Equal(faction.Culture, person.Culture);

        Assert.False(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, new FactionId(99))).Success);
        Assert.Equal(faction.Id, person.Faction);
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, FactionId.None)).Success);
        Assert.Equal(FactionId.None, person.Faction);
        Assert.False(host.Commands.Execute(new AssignCultureCommand(person.Id, new CultureId(99))).Success);
    }

    [Fact]
    public void Child_inherits_culture_not_faction()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var parent = host.Population[0];
        var culture = host.Civilization.Cultures.All.First(c => c.Id != CultureId.Neutral).Id;
        var faction = host.Civilization.Factions[0].Id;
        Assert.True(host.Commands.Execute(new AssignCultureCommand(parent.Id, culture)).Success);
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(parent.Id, faction)).Success);
        parent.AgeYears = 20;
        Assert.True(host.Commands.Execute(new CreateChildCommand(parent.Id, CharacterId.None)).Success);
        var child = host.Population.All.Last();
        Assert.Equal(culture, child.Culture);
        Assert.Equal(FactionId.None, child.Faction);
    }
}

public sealed class FactionRelationTests
{
    [Fact]
    public void Relations_are_symmetric_sparse_and_reject_self()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.Equal(FactionRelationStance.Neutral, host.Civilization.RelationOf(a, b));
        Assert.Equal(0, host.Civilization.Relations.Count);

        Assert.False(host.Commands.Execute(new SetFactionRelationCommand(a, a, FactionRelationStance.Friendly)).Success);
        Assert.True(host.Commands.Execute(new SetFactionRelationCommand(a, b, FactionRelationStance.Friendly)).Success);
        Assert.Equal(FactionRelationStance.Friendly, host.Civilization.RelationOf(b, a));
        Assert.Equal(1, host.Civilization.Relations.Count);

        Assert.True(host.Commands.Execute(new SetFactionRelationCommand(b, a, FactionRelationStance.Hostile)).Success);
        Assert.Equal(FactionRelationStance.Hostile, host.Civilization.RelationOf(a, b));
        Assert.Equal(1, host.Civilization.Relations.Count);

        Assert.True(host.Commands.Execute(new SetFactionRelationCommand(a, b, FactionRelationStance.Neutral)).Success);
        Assert.Equal(0, host.Civilization.Relations.Count);
        Assert.Equal(FactionRelationStance.Neutral, host.Civilization.RelationOf(a, b));
    }

    [Fact]
    public void Relations_do_not_depend_on_geography_or_lod()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.True(host.Commands.Execute(new SetFactionRelationCommand(a, b, FactionRelationStance.Friendly)).Success);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Commands.Execute(new ForceChunkLodCommand(chunk, SimulationLodTier.Aggregate));
        host.Commands.Execute(new SetChunkPresentationCommand(chunk, ChunkPresentationPresence.Unloaded));
        Assert.Equal(FactionRelationStance.Friendly, host.Civilization.RelationOf(a, b));
        Assert.Equal(CivilizationRules.GeneratedFactionCount, host.Civilization.Factions.Count);
    }
}

public sealed class CivilizationSeamTests
{
    [Fact]
    public void Mapper_roundtrips_culture_faction_and_relation()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var culture = host.Civilization.Cultures[1];
        var faction = host.Civilization.Factions[0];
        Assert.True(host.Commands.Execute(new SetFactionRelationCommand(
            host.Civilization.Factions[0].Id,
            host.Civilization.Factions[1].Id,
            FactionRelationStance.Friendly)).Success);
        var relation = host.Civilization.Relations.All[0];

        var restoredCulture = CivilizationMapper.FromRecord(CivilizationMapper.ToRecord(culture));
        var restoredFaction = CivilizationMapper.FromRecord(CivilizationMapper.ToRecord(faction));
        var restoredRelation = CivilizationMapper.FromRecord(CivilizationMapper.ToRecord(relation));
        Assert.Equal(culture.Snapshot(), restoredCulture.Snapshot());
        Assert.Equal(faction.Snapshot(), restoredFaction.Snapshot());
        Assert.Equal(relation.Snapshot(), restoredRelation.Snapshot());
    }
}

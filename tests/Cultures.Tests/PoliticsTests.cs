using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.World;

namespace Cultures.Tests;

public sealed class PoliticalGroupIdentityTests
{
    [Fact]
    public void Groups_are_faction_local_unique_and_deterministic()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var expected = CivilizationRules.GeneratedFactionCount * PoliticsRules.GeneratedGroupsPerFaction;
        Assert.Equal(expected, host.Politics.Groups.Count);
        var ids = host.Politics.Groups.All.Select(g => g.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(1UL, ids[0].Value);
        foreach (var group in host.Politics.Groups.All)
            Assert.True(host.Civilization.Factions.TryGet(group.Faction, out _));

        var copy = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(
            host.Politics.Groups.All.Select(g => g.Snapshot()).ToArray(),
            copy.Politics.Groups.All.Select(g => g.Snapshot()).ToArray());
    }

    [Fact]
    public void Creation_requires_existing_faction_and_rejects_duplicate_ids()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.False(host.Commands.Execute(new CreatePoliticalGroupCommand(new FactionId(99))).Success);
        var faction = host.Civilization.Factions[0].Id;
        var before = host.Politics.Groups.Count;
        Assert.True(host.Commands.Execute(new CreatePoliticalGroupCommand(faction, "Circle")).Success);
        Assert.Equal(before + 1, host.Politics.Groups.Count);
        var existing = host.Politics.Groups[0];
        Assert.Throws<InvalidOperationException>(() => host.Politics.Groups.Add(
            new PoliticalGroupState(existing.Id, existing.Faction, "Copy", default, 0)));
    }
}

public sealed class PoliticalMembershipTests
{
    [Fact]
    public void Affiliation_requires_matching_faction_and_does_not_change_culture()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var group = host.Politics.Groups[0];
        Assert.Equal(PoliticalGroupId.None, person.PoliticalGroup);
        Assert.False(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);

        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, group.Faction)).Success);
        var culture = person.Culture;
        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        Assert.Equal(group.Id, person.PoliticalGroup);
        Assert.Equal(culture, person.Culture);
        Assert.Equal(1, host.Politics.CountMembers(group.Id));

        var otherFaction = host.Civilization.Factions.All.First(f => f.Id != group.Faction).Id;
        var foreign = host.Politics.Groups.ForFaction(otherFaction)[0];
        Assert.False(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, foreign.Id)).Success);
        Assert.Equal(group.Id, person.PoliticalGroup);

        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, PoliticalGroupId.None)).Success);
        Assert.Equal(PoliticalGroupId.None, person.PoliticalGroup);
        Assert.False(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, new PoliticalGroupId(99))).Success);
    }

    [Fact]
    public void Leaving_a_faction_clears_political_group_without_touching_diplomacy_or_exploration()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var group = host.Politics.Groups[0];
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, group.Faction)).Success);
        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Friendly)).Success);
        var knowledge = host.Exploration.Knowledge.Count;
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, FactionId.None)).Success);
        Assert.Equal(PoliticalGroupId.None, person.PoliticalGroup);
        Assert.Equal(FactionRelationStance.Friendly, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(knowledge, host.Exploration.Knowledge.Count);
    }
}

public sealed class PoliticalInfluenceAndStabilityTests
{
    [Fact]
    public void Influence_is_explicit_and_independent_of_population()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var group = host.Politics.Groups[0];
        Assert.Equal(0, host.Politics.CountMembers(group.Id));
        Assert.True(host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 80)).Success);
        Assert.Equal(80, group.Influence);
        Assert.Equal(0, host.Politics.CountMembers(group.Id));
        Assert.False(host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 101)).Success);
        Assert.False(host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(new PoliticalGroupId(99), 10)).Success);
        Assert.Equal(80, group.Influence);
    }

    [Fact]
    public void Stability_is_sparse_bounded_and_has_no_diplomatic_or_economic_side_effects()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var faction = host.Civilization.Factions[0].Id;
        var other = host.Civilization.Factions[1].Id;
        Assert.Equal(PoliticsRules.StabilityDefault, host.Politics.Stability.Of(faction).Value);
        Assert.Equal(0, host.Politics.Stability.Count);
        var food = host.Lod.ResourceTotals();
        var stance = host.Diplomacy.StanceOf(faction, other);
        Assert.True(host.Commands.Execute(new SetInternalStabilityCommand(faction, 10)).Success);
        Assert.Equal(InternalStabilityBand.Unstable, host.Politics.Stability.Of(faction).Band);
        Assert.Equal(1, host.Politics.Stability.Count);
        Assert.Equal(stance, host.Diplomacy.StanceOf(faction, other));
        Assert.Equal(food, host.Lod.ResourceTotals());
        Assert.False(host.Commands.Execute(new SetInternalStabilityCommand(faction, -1)).Success);
        Assert.False(host.Commands.Execute(new SetInternalStabilityCommand(new FactionId(99), 40)).Success);
        Assert.True(host.Commands.Execute(new SetInternalStabilityCommand(faction, PoliticsRules.StabilityDefault)).Success);
        Assert.Equal(0, host.Politics.Stability.Count);
    }
}

public sealed class PoliticsIsolationTests
{
    [Fact]
    public void Politics_does_not_mutate_world_lod_or_civilization_identity()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var person = host.Population[0];
        var group = host.Politics.Groups[0];
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, group.Faction)).Success);
        var culture = person.Culture;
        var position = person.Position;
        var cell = host.World.Grid.GetCell(position);
        var chunk = host.World.Chunks.ToAddress(position).Chunk;
        var lod = host.Lod.Classify(chunk);
        var settlements = host.Settlements.Count;
        var factions = host.Civilization.Factions.Count;
        var cache = host.World.Generator.CachedChunkCount;
        var knowledge = host.Exploration.Knowledge.Count;
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        var stance = host.Diplomacy.StanceOf(a, b);

        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        Assert.True(host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 25)).Success);
        Assert.True(host.Commands.Execute(new SetInternalStabilityCommand(group.Faction, 20)).Success);

        Assert.Equal(culture, person.Culture);
        Assert.Equal(position, person.Position);
        Assert.Equal(cell.Biome, host.World.Grid.GetCell(position).Biome);
        Assert.Equal(cell.Climate.Temperature, host.World.Grid.GetCell(position).Climate.Temperature);
        Assert.Equal(lod, host.Lod.Classify(chunk));
        Assert.Equal(settlements, host.Settlements.Count);
        Assert.Equal(factions, host.Civilization.Factions.Count);
        Assert.Equal(cache, host.World.Generator.CachedChunkCount);
        Assert.Equal(knowledge, host.Exploration.Knowledge.Count);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(chunk));
        Assert.Equal(stance, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(group.Faction, host.Civilization.Factions.All.First(f => f.Id == group.Faction).Id);
    }

    [Fact]
    public void Equivalent_hosts_and_commands_are_deterministic()
    {
        var a = Run();
        var b = Run();
        Assert.Equal(a, b);

        static (int Groups, int Influence, int Stability) Run()
        {
            var host = new SimulationHost(5, populationCount: 1, placeDevelopmentBuildings: false);
            var group = host.Politics.Groups[0];
            host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 40));
            host.Commands.Execute(new SetInternalStabilityCommand(group.Faction, 15));
            return (host.Politics.Groups.Count, host.Politics.Groups[0].Influence, host.Politics.Stability.Of(group.Faction).Value);
        }
    }

    [Fact]
    public void Mapper_roundtrips_group_and_stability()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var group = host.Politics.Groups[0];
        host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 12));
        var restored = CivilizationMapper.FromRecord(CivilizationMapper.ToRecord(group));
        Assert.Equal(group.Snapshot(), restored.Snapshot());
        var record = CivilizationMapper.ToRecord(group.Faction, new InternalStability(12));
        Assert.Equal(12, CivilizationMapper.FromRecord(record).Value);
    }
}

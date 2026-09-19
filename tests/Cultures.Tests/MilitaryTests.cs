using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.Military;
using Cultures.World;

namespace Cultures.Tests;

public sealed class MilitaryIdentityTests
{
    [Fact]
    public void Units_are_faction_owned_unique_and_deterministic()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var expected = CivilizationRules.GeneratedFactionCount * MilitaryRules.GeneratedUnitsPerFaction;
        Assert.Equal(expected, host.Military.Units.Count);
        var ids = host.Military.Units.All.Select(u => u.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(1UL, ids[0].Value);
        foreach (var unit in host.Military.Units.All)
        {
            Assert.True(host.Civilization.Factions.TryGet(unit.Faction, out _));
            Assert.Equal(MilitaryUnitLifecycle.Active, unit.Lifecycle);
        }

        var copy = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(
            host.Military.Units.All.Select(u => u.Snapshot()).ToArray(),
            copy.Military.Units.All.Select(u => u.Snapshot()).ToArray());
    }

    [Fact]
    public void Creation_requires_existing_faction_and_rejects_duplicate_ids()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.False(host.Commands.Execute(new CreateMilitaryUnitCommand(new FactionId(99))).Success);
        var faction = host.Civilization.Factions[0].Id;
        var before = host.Military.Units.Count;
        Assert.True(host.Commands.Execute(new CreateMilitaryUnitCommand(faction, "Watch")).Success);
        Assert.Equal(before + 1, host.Military.Units.Count);
        var existing = host.Military.Units[0];
        Assert.Throws<InvalidOperationException>(() => host.Military.Units.Add(
            new MilitaryUnitState(existing.Id, existing.Faction, "Copy", 0)));
    }
}

public sealed class MilitaryMembershipTests
{
    [Fact]
    public void Affiliation_requires_matching_faction_and_does_not_change_culture_or_politics()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var unit = host.Military.Units[0];
        var group = host.Politics.Groups.ForFaction(unit.Faction)[0];
        Assert.Equal(MilitaryUnitId.None, person.MilitaryUnit);
        Assert.False(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);

        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, unit.Faction)).Success);
        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        var culture = person.Culture;
        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);
        Assert.Equal(unit.Id, person.MilitaryUnit);
        Assert.Equal(culture, person.Culture);
        Assert.Equal(group.Id, person.PoliticalGroup);
        Assert.Equal(1, host.Military.CountMembers(unit.Id));

        var otherFaction = host.Civilization.Factions.All.First(f => f.Id != unit.Faction).Id;
        var foreign = host.Military.Units.ForFaction(otherFaction)[0];
        Assert.False(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, foreign.Id)).Success);
        Assert.Equal(unit.Id, person.MilitaryUnit);
        Assert.Equal(group.Id, person.PoliticalGroup);

        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, MilitaryUnitId.None)).Success);
        Assert.Equal(MilitaryUnitId.None, person.MilitaryUnit);
        Assert.Equal(group.Id, person.PoliticalGroup);
        Assert.False(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, new MilitaryUnitId(99))).Success);
    }

    [Fact]
    public void Character_belongs_to_at_most_one_unit_and_explicit_switch_is_allowed()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var faction = host.Civilization.Factions[0].Id;
        var first = host.Military.Units.ForFaction(faction)[0];
        Assert.True(host.Commands.Execute(new CreateMilitaryUnitCommand(faction, "Second")).Success);
        var second = host.Military.Units.ForFaction(faction)[^1];
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, faction)).Success);
        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, first.Id)).Success);
        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, second.Id)).Success);
        Assert.Equal(second.Id, person.MilitaryUnit);
        Assert.Equal(0, host.Military.CountMembers(first.Id));
        Assert.Equal(1, host.Military.CountMembers(second.Id));
        Assert.False(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, second.Id)).Success);
        Assert.True(host.Commands.Execute(new RemoveCharacterFromMilitaryUnitCommand(person.Id, second.Id)).Success);
        Assert.Equal(MilitaryUnitId.None, person.MilitaryUnit);
        Assert.False(host.Commands.Execute(new RemoveCharacterFromMilitaryUnitCommand(person.Id, second.Id)).Success);
    }

    [Fact]
    public void Leaving_a_faction_clears_military_membership_without_touching_diplomacy_politics_or_exploration()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var unit = host.Military.Units[0];
        var group = host.Politics.Groups.ForFaction(unit.Faction)[0];
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, unit.Faction)).Success);
        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);
        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Hostile)).Success);
        Assert.True(host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, 40)).Success);
        Assert.True(host.Commands.Execute(new SetInternalStabilityCommand(unit.Faction, 20)).Success);
        var knowledge = host.Exploration.Knowledge.Count;
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, FactionId.None)).Success);
        Assert.Equal(MilitaryUnitId.None, person.MilitaryUnit);
        Assert.Equal(PoliticalGroupId.None, person.PoliticalGroup);
        Assert.Equal(FactionRelationStance.Hostile, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(40, group.Influence);
        Assert.Equal(20, host.Politics.Stability.Of(unit.Faction).Value);
        Assert.Equal(knowledge, host.Exploration.Knowledge.Count);
        Assert.Equal(0, host.Military.CountMembers(unit.Id));
    }
}

public sealed class MilitaryLifecycleTests
{
    [Fact]
    public void Disband_clears_members_and_rejects_invalid_transitions()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        var unit = host.Military.Units[0];
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, unit.Faction)).Success);
        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);
        Assert.True(host.Commands.Execute(new DisbandMilitaryUnitCommand(unit.Id)).Success);
        Assert.Equal(MilitaryUnitLifecycle.Disbanded, unit.Lifecycle);
        Assert.Equal(MilitaryUnitId.None, person.MilitaryUnit);
        Assert.Equal(0, host.Military.CountMembers(unit.Id));
        Assert.False(host.Commands.Execute(new DisbandMilitaryUnitCommand(unit.Id)).Success);
        Assert.False(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);
        Assert.False(host.Commands.Execute(new DisbandMilitaryUnitCommand(new MilitaryUnitId(99))).Success);
        Assert.Equal(unit.Faction, host.Civilization.Factions.All.First(f => f.Id == unit.Faction).Id);
    }
}

public sealed class MilitaryIsolationTests
{
    [Fact]
    public void Military_does_not_mutate_world_lod_civilization_politics_or_diplomacy()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var person = host.Population[0];
        var unit = host.Military.Units[0];
        var group = host.Politics.Groups.ForFaction(unit.Faction)[0];
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, unit.Faction)).Success);
        Assert.True(host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, group.Id)).Success);
        var culture = person.Culture;
        var groupId = person.PoliticalGroup;
        var influence = group.Influence;
        var stability = host.Politics.Stability.Of(unit.Faction).Value;
        var position = person.Position;
        var cell = host.World.Grid.GetCell(position);
        var chunk = host.World.Chunks.ToAddress(position).Chunk;
        var lod = host.Lod.Classify(chunk);
        var settlements = host.Settlements.Count;
        var factions = host.Civilization.Factions.Count;
        var cache = host.World.Generator.CachedChunkCount;
        var knowledge = host.Exploration.Knowledge.Count;
        var food = host.Lod.ResourceTotals();
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        var stance = host.Diplomacy.StanceOf(a, b);

        Assert.True(host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id)).Success);
        Assert.True(host.Commands.Execute(new CreateMilitaryUnitCommand(unit.Faction, "Reserve")).Success);

        Assert.Equal(culture, person.Culture);
        Assert.Equal(groupId, person.PoliticalGroup);
        Assert.Equal(influence, group.Influence);
        Assert.Equal(stability, host.Politics.Stability.Of(unit.Faction).Value);
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
        Assert.Equal(food, host.Lod.ResourceTotals());
        Assert.Equal(FactionRelationStance.Neutral, host.Diplomacy.StanceOf(a, b));
    }

    [Fact]
    public void Equivalent_hosts_and_commands_are_deterministic()
    {
        var a = Run();
        var b = Run();
        Assert.Equal(a, b);

        static (int Units, ulong Member, byte Lifecycle) Run()
        {
            var host = new SimulationHost(5, populationCount: 1, placeDevelopmentBuildings: false);
            var person = host.Population[0];
            var unit = host.Military.Units[0];
            host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, unit.Faction));
            host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, unit.Id));
            host.Commands.Execute(new CreateMilitaryUnitCommand(unit.Faction, "Extra"));
            return (host.Military.Units.Count, person.MilitaryUnit.Value, (byte)unit.Lifecycle);
        }
    }

    [Fact]
    public void Mapper_roundtrips_unit_state()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var unit = host.Military.Units[0];
        host.Commands.Execute(new DisbandMilitaryUnitCommand(unit.Id));
        var restored = CivilizationMapper.FromRecord(CivilizationMapper.ToRecord(unit));
        Assert.Equal(unit.Snapshot(), restored.Snapshot());
    }
}

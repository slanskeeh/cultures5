using Cultures.Application;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Exploration;
using Cultures.World;

namespace Cultures.Tests;

public sealed class DiplomacyInvariantTests
{
    [Fact]
    public void Stance_is_symmetric_sparse_and_rejects_self()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.Equal(FactionRelationStance.Neutral, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(host.Diplomacy.StanceOf(a, b), host.Diplomacy.StanceOf(b, a));
        Assert.Equal(0, host.Diplomacy.Relations.Count);

        Assert.False(host.Commands.Execute(new SetDiplomaticStanceCommand(a, a, FactionRelationStance.Friendly)).Success);
        Assert.Equal(0, host.Diplomacy.Relations.Count);

        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Friendly)).Success);
        Assert.Equal(FactionRelationStance.Friendly, host.Diplomacy.StanceOf(b, a));
        Assert.Equal(1, host.Diplomacy.Relations.Count);

        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(b, a, FactionRelationStance.Hostile)).Success);
        Assert.Equal(FactionRelationStance.Hostile, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(1, host.Diplomacy.Relations.Count);
        Assert.Equal(host.Diplomacy.Relations.All[0].Lower, FactionRelationDirectory.Normalize(a, b).Lower);

        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Neutral)).Success);
        Assert.Equal(0, host.Diplomacy.Relations.Count);
        Assert.Equal(FactionRelationStance.Neutral, host.Diplomacy.StanceOf(a, b));
    }

    [Fact]
    public void Invalid_and_missing_factions_fail()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        var missing = new FactionId(99);

        Assert.False(host.Commands.Execute(new SetDiplomaticStanceCommand(missing, b, FactionRelationStance.Friendly)).Success);
        Assert.False(host.Commands.Execute(new SetDiplomaticStanceCommand(a, missing, FactionRelationStance.Hostile)).Success);
        Assert.False(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, (FactionRelationStance)255)).Success);
        Assert.Equal(0, host.Diplomacy.Relations.Count);
        Assert.Equal(FactionRelationStance.Neutral, host.Diplomacy.StanceOf(a, b));
    }
}

public sealed class DiplomacyIsolationTests
{
    [Fact]
    public void Stance_change_does_not_mutate_unrelated_simulation_state()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var person = host.Population[0];
        var faction = host.Civilization.Factions[0].Id;
        Assert.True(host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, faction)).Success);
        var culture = person.Culture;
        var membership = person.Faction;
        var position = person.Position;
        var food = person.Inventory.GetQuantity(ResourceType.Food);
        var settlements = host.Settlements.Count;
        var knowledge = host.Exploration.Knowledge.Count;
        var cache = host.World.Generator.CachedChunkCount;
        var cell = host.World.Grid.GetCell(position);
        var chunk = host.World.Chunks.ToAddress(position).Chunk;
        var lod = host.Lod.Classify(chunk);
        var totals = host.Lod.ResourceTotals();
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;

        DiplomaticStanceChangedEvent? published = null;
        host.Events.Subscribe<DiplomaticStanceChangedEvent>(e => published = e);
        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Friendly)).Success);

        Assert.Equal(culture, person.Culture);
        Assert.Equal(membership, person.Faction);
        Assert.Equal(position, person.Position);
        Assert.Equal(food, person.Inventory.GetQuantity(ResourceType.Food));
        Assert.Equal(settlements, host.Settlements.Count);
        Assert.Equal(knowledge, host.Exploration.Knowledge.Count);
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(chunk));
        Assert.Equal(cache, host.World.Generator.CachedChunkCount);
        Assert.Equal(cell.Biome, host.World.Grid.GetCell(position).Biome);
        Assert.Equal(lod, host.Lod.Classify(chunk));
        Assert.Equal(totals, host.Lod.ResourceTotals());
        Assert.NotNull(published);
        Assert.Equal(FactionRelationStance.Neutral, published!.Previous);
        Assert.Equal(FactionRelationStance.Friendly, published.Current);
    }

    [Fact]
    public void Equivalent_hosts_and_commands_are_deterministic()
    {
        var a = Run();
        var b = Run();
        Assert.Equal(a, b);

        static (FactionRelationStance Stance, int Stored) Run()
        {
            var host = new SimulationHost(4, populationCount: 1, placeDevelopmentBuildings: false);
            var left = host.Civilization.Factions[0].Id;
            var right = host.Civilization.Factions[2].Id;
            host.Commands.Execute(new SetDiplomaticStanceCommand(left, right, FactionRelationStance.Friendly));
            host.Commands.Execute(new SetDiplomaticStanceCommand(right, left, FactionRelationStance.Hostile));
            return (host.Diplomacy.StanceOf(left, right), host.Diplomacy.Relations.Count);
        }
    }
}

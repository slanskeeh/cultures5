using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Application.Presentation;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.History;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class HistoryRecorderTests
{
    [Fact]
    public void Important_events_are_recorded_with_stable_ids_and_queries()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.True(host.History.Directory.Count > 0);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.CultureCreated);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.FactionCreated);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.MilitaryUnitCreated);
        var ids = host.History.Directory.All.Select(e => e.Id.Value).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        var faction = host.Civilization.Factions[0];
        Assert.NotEmpty(host.History.GetEventsForFaction(faction.Id));
    }

    [Fact]
    public void Low_level_ticks_are_not_history_spam()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var before = host.History.Directory.Count;
        host.Step(12);
        Assert.Equal(before, host.History.Directory.Count);
    }

    [Fact]
    public void Chronology_is_monotonic_and_deterministic()
    {
        static int[] Run()
        {
            var host = new SimulationHost(3, populationCount: 1, placeDevelopmentBuildings: false);
            return host.History.Directory.All.Select(e => (int)e.Kind).ToArray();
        }

        Assert.Equal(Run(), Run());
        var host = new SimulationHost(3, populationCount: 1, placeDevelopmentBuildings: false);
        var ticks = host.History.Directory.All.Select(e => e.Tick).ToList();
        Assert.Equal(ticks.OrderBy(t => t), ticks);
    }

    [Fact]
    public void History_does_not_mutate_exploration_or_diplomacy()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        var knowledge = host.Exploration.Knowledge.Count;
        var stance = host.Diplomacy.StanceOf(a, b);
        Assert.True(host.History.Directory.Count > 0);
        Assert.Equal(knowledge, host.Exploration.Knowledge.Count);
        Assert.Equal(stance, host.Diplomacy.StanceOf(a, b));
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(new ChunkCoordinate(0, 0)));
    }

    [Fact]
    public void Aggregate_facts_are_not_individual_deaths()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var personalDeaths = host.History.Directory.All.Count(e => e.Kind == HistoryKind.CharacterDied);
        Assert.Equal(0, personalDeaths);
        Assert.DoesNotContain(host.History.Directory.All, e => e.Kind == HistoryKind.CharacterDied && e.Subject == 0);
    }
}

public sealed class PersistenceRoundtripTests
{
    [Fact]
    public void Full_debug_world_survives_save_load_and_continuation()
    {
        var original = new SimulationHost(11, populationCount: 4);
        original.Step(20);
        var person = original.Population[0];
        original.Commands.Execute(new AssignFactionMembershipCommand(person.Id, original.Civilization.Factions[0].Id));
        original.Commands.Execute(new ScoutChunkCommand(original.World.Chunks.ToAddress(person.Position).Chunk));
        original.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Farmer.Value));
        original.Commands.Execute(new FormHouseholdCommand(person.Id, CharacterId.None));
        var envelope = original.CreateSave();
        Assert.Equal(4, envelope.SaveVersion);

        var restored = SimulationHost.FromSave(envelope);
        Assert.Equal(original.Population.Count, restored.Population.Count);
        Assert.Equal(person.Id, restored.Population[0].Id);
        Assert.Equal(person.Profession, restored.Population[0].Profession);
        Assert.Equal(person.Household, restored.Population[0].Household);
        Assert.Equal(original.History.Directory.Count, restored.History.Directory.Count);
        Assert.Equal(original.History.Directory[0].Id, restored.History.Directory[0].Id);
        Assert.Equal(original.Exploration.Knowledge.Count, restored.Exploration.Knowledge.Count);
        Assert.Equal(original.Ecology.Deposits.Count, restored.Ecology.Deposits.Count);
        Assert.Equal(original.Ids.Snapshot(), restored.Ids.Snapshot());

        original.Step(8);
        restored.Step(8);
        Assert.Equal(original.Clock.Tick, restored.Clock.Tick);
        Assert.Equal(original.Population[0].AgeYears, restored.Population[0].AgeYears, 5);
    }

    [Fact]
    public void Unsupported_version_fails_and_id_generator_does_not_reuse()
    {
        var serializer = new SaveSerializer();
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("""{"saveVersion":1,"worldSeed":1}"""));
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize("""{"saveVersion":99,"worldSeed":1}"""));

        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var maxId = host.Population.All.Max(p => p.Id.Value);
        var restored = SimulationHost.FromSave(host.CreateSave());
        var next = restored.Ids.NextCharacter();
        Assert.True(next.Value > maxId);
    }

    [Fact]
    public void V2_header_save_still_loads_as_contract_only()
    {
        var json = """
            {"saveVersion":2,"worldSeed":9,"simulationTick":40,"generationVersion":1,"worldWidth":100,"worldHeight":50,"chunkWidth":10,"chunkHeight":10}
            """;
        var envelope = new SaveSerializer().Deserialize(json);
        var host = SimulationHost.FromSave(envelope);
        Assert.Equal(9UL, host.WorldSeed);
        Assert.Equal(40UL, host.Clock.Tick);
        Assert.Equal(CharacterRules.DefaultPopulation, host.Population.Count);
    }
}

public sealed class EcologyTests
{
    [Fact]
    public void Fertility_and_rivers_are_deterministic_and_wrap_stable()
    {
        var a = new SimulationHost(4, populationCount: 1, placeDevelopmentBuildings: false);
        var b = new SimulationHost(4, populationCount: 1, placeDevelopmentBuildings: false);
        var cell = new LogicalGridCoordinate(0, 8);
        Assert.Equal(a.World.Grid.GetCell(cell).Generated, b.World.Grid.GetCell(cell).Generated);
        var wrap = new LogicalGridCoordinate(a.World.Configuration.Width - 1, 8);
        Assert.Equal(
            a.World.Grid.GetCell(wrap).Fertility,
            b.World.Grid.GetCell(wrap).Fertility);
    }

    [Fact]
    public void Deposits_deplete_and_regenerate_without_touching_exploration()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var origin = PopulationSpawner.FindLandOrigin(host.World);
        host.Ecology.EnsureCell(origin);
        Assert.True(host.Ecology.Deposits.Count > 0);
        var deposit = host.Ecology.Deposits[0];
        var before = deposit.Stock;
        Assert.True(host.Ecology.TryExtract(deposit.Cell, deposit.Resource, 1));
        Assert.Equal(before - 1, deposit.Stock);
        for (var i = 0; i < EcologyRules.RegenerationIntervalTicks; i++)
            host.Ecology.Tick();
        Assert.True(deposit.Stock >= before - 1);
        Assert.Equal(0, host.Exploration.Knowledge.Count);
    }

    [Fact]
    public void Farm_output_depends_on_biome_table()
    {
        Assert.Equal(RecipeId.FarmBerries, ContextualRecipeTable.Resolve(BuildingTypeId.Farm, BiomeId.Forest));
        Assert.Equal(RecipeId.FarmFood, ContextualRecipeTable.Resolve(BuildingTypeId.Farm, BiomeId.TemperateLand));
        Assert.Equal(RecipeId.WorkshopStone, ContextualRecipeTable.Resolve(BuildingTypeId.Workshop, BiomeId.Highland));
    }

    [Fact]
    public void Wildlife_is_deterministic_per_chunk()
    {
        var host = new SimulationHost(2, populationCount: 1);
        var chunk = host.World.Chunks.ToAddress(host.Population[0].Position).Chunk;
        host.Ecology.EnsureCell(host.Population[0].Position);
        var copy = new SimulationHost(2, populationCount: 1);
        copy.Ecology.EnsureCell(copy.Population[0].Position);
        Assert.Equal(
            host.Ecology.Wildlife.GetOrCreate(chunk).Snapshot(),
            copy.Ecology.Wildlife.GetOrCreate(chunk).Snapshot());
    }
}

public sealed class ProfessionAndHouseholdTests
{
    [Fact]
    public void Profession_is_not_skill_and_workplace_compatibility_is_enforced()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: true);
        var person = host.Population[0];
        var farming = person.Skills.GetLevel(SkillType.Farming);
        Assert.False(person.Profession.IsAssigned);
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Farmer.Value)).Success);
        Assert.Equal(ProfessionId.Farmer, person.Profession);
        Assert.Equal(farming, person.Skills.GetLevel(SkillType.Farming));
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Woodcutter.Value)).Success);
        Assert.Equal(ProfessionId.Woodcutter, person.Profession);
        Assert.False(host.Commands.Execute(new AssignProfessionCommand(person.Id, "pirate")).Success);
    }

    [Fact]
    public void Household_is_not_genealogy_and_home_is_a_shelter()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: true);
        var a = host.Population[0];
        var b = host.Population[1];
        Assert.False(a.Household.IsAssigned);
        Assert.True(host.Commands.Execute(new FormHouseholdCommand(a.Id, b.Id)).Success);
        Assert.Equal(a.Household, b.Household);
        Assert.True(a.Household.IsAssigned);
        Assert.Empty(a.FamilyLinks.Parents);
        var shelter = host.Buildings.All.First(x => x.Definition.IsShelter);
        Assert.True(host.Commands.Execute(new SetHouseholdHomeCommand(a.Household, shelter.Id)).Success);
        Assert.Equal(shelter.Id, host.Households.All.First(h => h.Id == a.Household).Home);
        Assert.Equal(shelter, host.Social.HomeOf(a));
        Assert.True(host.Commands.Execute(new LeaveHouseholdCommand(b.Id)).Success);
        Assert.False(b.Household.IsAssigned);
        Assert.True(a.Household.IsAssigned);
    }

    [Fact]
    public void Partnership_and_birth_join_household_without_teaching()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var a = host.Population[0];
        var b = host.Population[1];
        Assert.True(host.Commands.Execute(new FormPartnershipCommand(a.Id, b.Id)).Success);
        Assert.Equal(b.Id, a.Partner);
        Assert.Equal(a.Household, b.Household);
        var before = host.Population.Count;
        Assert.True(host.Commands.Execute(new CreateChildCommand(a.Id, b.Id)).Success);
        Assert.Equal(before + 1, host.Population.Count);
        var child = host.Population.All.Last();
        Assert.Equal(a.Household, child.Household);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.CharacterBorn);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.PartnershipFormed);
    }
}

public sealed class PresentationIdentityTests
{
    [Fact]
    public void Selection_and_view_map_use_domain_ids_without_mutating_simulation()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: true);
        var selection = new PresentationSelection { Character = host.Population[0].Id, Building = host.Buildings[0].Id };
        var food = host.Population[0].Inventory.GetQuantity(Cultures.Economy.ResourceType.Food);
        var snapshot = PresentationIdentityMap.Capture(
            host.Population.All,
            host.Buildings.All,
            host.Settlements.All,
            host.Cursor.Position,
            host.World.Topology,
            40,
            40);
        Assert.Contains(selection.Character.Value, snapshot.Characters);
        Assert.Contains(selection.Building.Value, snapshot.Buildings);
        Assert.Equal(food, host.Population[0].Inventory.GetQuantity(Cultures.Economy.ResourceType.Food));
        Assert.Equal(host.Population[0].Id, selection.Character);
    }
}

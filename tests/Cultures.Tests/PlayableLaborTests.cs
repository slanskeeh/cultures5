using Cultures.Application;
using Cultures.Buildings;
using Cultures.Economy;
using Cultures.Exploration;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class PlayableLaborTests
{
    [Fact]
    public void Starting_professions_do_not_require_skill()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var person = host.Population[0];
        Assert.False(person.Profession.IsAssigned);
        var woodworking = person.Skills.GetLevel(SkillType.Woodworking);
        Assert.True(host.Professions.TryGet(ProfessionId.WoodGatherer, out var wood));
        Assert.False(wood.RequiresTraining);
        Assert.Contains(host.Professions.Starting, p => p.Id == ProfessionId.Hunter);
        Assert.Contains(host.Professions.Starting, p => p.Id == ProfessionId.Builder);
        Assert.Contains(host.Professions.Starting, p => p.Id == ProfessionId.Porter);
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.WoodGatherer.Value)).Success);
        Assert.Equal(ProfessionId.WoodGatherer, person.Profession);
        Assert.Equal(woodworking, person.Skills.GetLevel(SkillType.Woodworking));
    }

    [Fact]
    public void Playtest_population_receives_starting_jobs_small_groups_do_not()
    {
        var playtest = new SimulationHost(1);
        Assert.Equal(CharacterRules.DefaultPopulation, playtest.Population.Count);
        Assert.Contains(playtest.Population.All, person => person.Profession == ProfessionId.WoodGatherer);
        Assert.Contains(playtest.Population.All, person => person.Profession == ProfessionId.Scout);
        Assert.Contains(playtest.Population.All, person => !person.Profession.IsAssigned);

        var small = new SimulationHost(1, populationCount: 1);
        Assert.False(small.Population[0].Profession.IsAssigned);
    }

    [Fact]
    public void Unemployed_adult_can_still_work_a_farm()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var person = host.Population[0];
        var farm = host.Buildings.All.First(b => b.TypeId == BuildingTypeId.Farm);
        person.Position = farm.AccessCell;
        person.IsPersistentIndividual = true;
        Assert.False(person.Profession.IsAssigned);
        host.Characters.Decisions.AssignNext(person);
        Assert.Equal(ActionKind.Work, person.Activity.Kind);
        Assert.Equal(farm.Id, person.AssignedWorkplace.Building);
    }

    [Fact]
    public void Wood_gatherer_collects_wood_without_a_workplace()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        SeedDeposit(host, person.Position, ResourceType.Wood);
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.WoodGatherer.Value)).Success);
        TickUntil(host, person, () => person.Inventory.GetQuantity(ResourceType.Wood) > 0, 80);
        Assert.True(person.Inventory.GetQuantity(ResourceType.Wood) > 0);
        Assert.NotEqual(ActionKind.Work, person.Activity.Kind);
    }

    [Fact]
    public void Hunter_brings_meat_and_people_eat_edible_forage()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        host.Ecology.EnsureCell(person.Position);
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Hunter.Value)).Success);
        TickUntil(host, person, () => person.Inventory.GetQuantity(ResourceType.Meat) > 0, 80);
        Assert.True(person.Inventory.GetQuantity(ResourceType.Meat) > 0);

        var eater = host.Population[0];
        eater.Activity.Cancel();
        eater.Inventory.TryRemove(ResourceType.Meat, eater.Inventory.GetQuantity(ResourceType.Meat));
        Assert.True(eater.Inventory.TryAdd(ResourceType.Mushrooms, 1));
        eater.Needs.Hunger = 0.9f;
        host.Characters.Decisions.AssignNext(eater);
        Assert.Equal(ActionKind.Eat, eater.Activity.Kind);
        for (var i = 0; i < CharacterRules.EatDurationTicks; i++)
            host.Characters.Actions.Advance(eater);
        Assert.Equal(0, eater.Inventory.GetQuantity(ResourceType.Mushrooms));
        Assert.True(eater.Needs.Hunger < 0.9f);
    }

    [Fact]
    public void Porter_hauls_cargo_into_storage()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        var storage = host.Buildings.All.First(b => b.Definition.IsStorage);
        var before = storage.Inventory.GetQuantity(ResourceType.Wood);
        person.Position = storage.AccessCell;
        Assert.True(person.Inventory.TryAdd(ResourceType.Wood, 2));
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Porter.Value)).Success);
        TickUntil(host, person, () => storage.Inventory.GetQuantity(ResourceType.Wood) > before, 40);
        Assert.True(storage.Inventory.GetQuantity(ResourceType.Wood) >= before + 2);
        Assert.Equal(0, person.Inventory.GetQuantity(ResourceType.Wood));
    }

    [Fact]
    public void Builder_finishes_a_hut_when_materials_are_on_site()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        var site = PlaceConstructing(host, BuildingTypeId.Shelter, person.Position);
        Assert.Equal(BuildingLifecycle.Constructing, site.Lifecycle);
        Assert.True(site.Inventory.TryAdd(ResourceType.Wood, 3));
        site.ConstructionProgress = site.Definition.ConstructionTicks - 1;
        person.Position = site.AccessCell;
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Builder.Value)).Success);
        TickUntil(host, person, () => site.Lifecycle == BuildingLifecycle.Active, 40);
        Assert.Equal(BuildingLifecycle.Active, site.Lifecycle);
        Assert.Equal(0, site.Inventory.GetQuantity(ResourceType.Wood));
    }

    [Fact]
    public void Construction_waits_for_missing_materials()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        var site = PlaceConstructing(host, BuildingTypeId.Shelter, person.Position);
        site.ConstructionProgress = site.Definition.ConstructionTicks - 1;
        person.Position = site.AccessCell;
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Builder.Value)).Success);
        TickUntil(host, person, () => site.ConstructionProgress >= site.Definition.ConstructionTicks, 40);
        Assert.Equal(BuildingLifecycle.Constructing, site.Lifecycle);
        Assert.True(LaborSystem.NeedsMaterials(site));
    }

    [Fact]
    public void Scout_maps_the_current_chunk()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        var chunk = host.World.Chunks.ToAddress(person.Position).Chunk;
        Assert.Equal(ExplorationKnowledgeLevel.Unknown, host.Exploration.LevelOf(chunk));
        Assert.True(host.Commands.Execute(new AssignProfessionCommand(person.Id, ProfessionId.Scout.Value)).Success);
        TickUntil(host, person, () => host.Exploration.LevelOf(chunk) >= ExplorationKnowledgeLevel.Mapped, 80);
        Assert.True(host.Exploration.LevelOf(chunk) >= ExplorationKnowledgeLevel.Mapped);
    }

    [Fact]
    public void Direct_labor_releases_player_hold_and_order_move_walks()
    {
        var host = AliveWorker();
        var person = host.Population[0];
        var destination = host.Characters.Navigator.PassableNeighbors(person.Position).First();
        Assert.True(host.Commands.Execute(new OrderMoveCommand(person.Id, destination)).Success);
        Assert.True(person.IsPlayerCommanded);
        Assert.Equal(ActionKind.Move, person.Activity.Kind);

        TickUntil(host, person, () => person.Position.Equals(destination), 80);
        Assert.Equal(destination, person.Position);

        person.Needs.Hunger = 0.10f;
        person.Needs.Fatigue = 0.10f;
        host.Characters.Decisions.AssignNext(person);
        Assert.Equal(ActionKind.Idle, person.Activity.Kind);

        Assert.True(host.Commands.Execute(new DirectLaborCommand(person.Id)).Success);
        Assert.False(person.IsPlayerCommanded);
        Assert.True(host.Commands.Execute(new StopLaborCommand(person.Id)).Success);
        Assert.False(person.IsPlayerCommanded);
    }

    private static SimulationHost AliveWorker()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var person = host.Population[0];
        person.IsPersistentIndividual = true;
        person.Needs.Hunger = 0.10f;
        person.Needs.Fatigue = 0.10f;
        if (!host.Characters.Navigator.IsPassable(person.Position))
        {
            person.Position = host.Characters.Navigator.PassableNeighbors(person.Position).First();
        }

        return host;
    }

    private static void SeedDeposit(SimulationHost host, LogicalGridCoordinate cell, ResourceType resource)
    {
        if (host.Ecology.Deposits.TryGetAt(cell, resource, out var existing))
        {
            existing.Stock = Math.Max(existing.Stock, 4);
            return;
        }

        host.Ecology.Deposits.Add(
            new ResourceDeposit(host.Ids.NextResourceDeposit(), cell, resource, 6, EcologyRules.DepositCapacity));
    }

    private static BuildingState PlaceConstructing(
        SimulationHost host,
        BuildingTypeId type,
        LogicalGridCoordinate origin)
    {
        foreach (var cell in SettlementTestSupport.Spiral(host.World, origin))
        {
            var result = host.Placement.TryPlace(type, cell, completeImmediately: false);
            if (result.Success && result.Building is not null)
                return result.Building;
        }

        throw new InvalidOperationException($"Could not place constructing {type.Value} near {origin}.");
    }

    private static void TickUntil(SimulationHost host, CharacterState person, Func<bool> done, int limit)
    {
        for (var i = 0; i < limit; i++)
        {
            person.Needs.Hunger = 0.10f;
            person.Needs.Fatigue = 0.10f;
            host.Step(1);
            if (done())
                return;
        }
    }
}

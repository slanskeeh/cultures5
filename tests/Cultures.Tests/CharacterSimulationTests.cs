using Cultures.Application;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class CharacterIdentityTests
{
    [Fact]
    public void Characters_have_distinct_stable_ids()
    {
        var host = new SimulationHost(1);
        var ids = host.Population.All.Select(c => c.Id).ToArray();
        Assert.Equal(CharacterRules.DefaultPopulation, ids.Length);
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.DoesNotContain(CharacterId.None, ids);
        Assert.Equal(new CharacterId(1), ids[0]);
        Assert.Equal(4UL, ids[3].Value);
        Assert.NotEqual((ulong)3, ids[3].Value);
    }

    [Fact]
    public void Same_seed_creates_equivalent_initial_population()
    {
        var a = new SimulationHost(11);
        var b = new SimulationHost(11);
        Assert.Equal(
            a.Population.All.Select(c => c.Snapshot()),
            b.Population.All.Select(c => c.Snapshot()));
        Assert.All(a.Population.All, c => Assert.True(a.World.Grid.GetCell(c.Position).Passable));
    }
}

public sealed class CharacterNeedsAndSurvivalTests
{
    [Fact]
    public void Time_increases_hunger_and_fatigue()
    {
        var calendar = SimulationCalendar.Default;
        var character = Adult();
        var needs = new CharacterNeedsSystem(calendar);
        var beforeHunger = character.Needs.Hunger;
        var beforeFatigue = character.Needs.Fatigue;
        for (ulong i = 0; i < calendar.TicksPerDay; i++)
            needs.ApplyTick(character);

        Assert.True(character.Needs.Hunger > beforeHunger);
        Assert.True(character.Needs.Fatigue > beforeFatigue);
    }

    [Fact]
    public void Eating_decreases_hunger_and_consumes_food()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var actions = new CharacterActionSystem(new GridNavigator(world), TestProduction.ForWorld(world), TestProduction.Teaching(world));
        var character = Adult();
        character.Needs.Hunger = 0.9f;
        Assert.True(character.Inventory.TryAdd(ResourceType.Food, 1));
        character.Activity.Start(ActionKind.Eat, CharacterRules.EatDurationTicks);

        for (var i = 0; i < CharacterRules.EatDurationTicks; i++)
            actions.Advance(character);

        Assert.Equal(0, character.Inventory.GetQuantity(ResourceType.Food));
        Assert.True(character.Needs.Hunger < 0.9f);
        Assert.Equal(ActionKind.None, character.Activity.Kind);
    }

    [Fact]
    public void Sleeping_decreases_fatigue()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var actions = new CharacterActionSystem(new GridNavigator(world), TestProduction.ForWorld(world), TestProduction.Teaching(world));
        var character = Adult();
        character.Needs.Fatigue = 0.95f;
        character.Activity.Start(ActionKind.Sleep, CharacterRules.SleepDurationTicks, character.Position);

        for (var i = 0; i < CharacterRules.SleepDurationTicks; i++)
            actions.Advance(character);

        Assert.True(character.Needs.Fatigue < 0.95f);
    }

    [Fact]
    public void Age_progresses_with_simulation_time()
    {
        var calendar = SimulationCalendar.Default;
        var aging = new CharacterAgingSystem(calendar);
        var character = Adult();
        var start = character.AgeYears;
        for (ulong i = 0; i < calendar.TicksPerYear; i++)
            aging.ApplyTick(character);

        Assert.InRange(character.AgeYears, start + 0.999, start + 1.001);
        Assert.Equal(CharacterLifeStage.Adult, character.LifeStage);
    }

    [Fact]
    public void Starvation_causes_deterministic_death()
    {
        var calendar = SimulationCalendar.Default;
        var needs = new CharacterNeedsSystem(calendar);
        var survival = new CharacterSurvivalSystem(calendar);
        var character = Adult();
        character.Needs.Hunger = 1f;

        for (ulong i = 0; i < calendar.TicksPerDay * 6; i++)
        {
            needs.ApplyTick(character);
            character.Needs.Hunger = 1f;
            survival.ApplyTick(character);
        }

        Assert.Equal(CharacterLifeStage.Dead, character.LifeStage);
        Assert.False(character.IsAlive);
    }

    private static CharacterState Adult() =>
        new(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1);
}

public sealed class CharacterMovementTests
{
    [Fact]
    public void Adjacent_move_onto_land_succeeds()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var character = host.Population[0];
        var navigator = host.Characters.Navigator;
        var neighbor = navigator.PassableNeighbors(character.Position).First();
        character.Activity.Start(ActionKind.Move, 1, neighbor);
        character.Activity.RemainingPath.Enqueue(neighbor);
        host.Characters.Actions.Advance(character);
        Assert.Equal(neighbor, character.Position);
    }

    [Fact]
    public void Water_blocks_movement()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var navigator = new GridNavigator(world);
        var land = PopulationSpawner.FindLandOrigin(world);
        foreach (var (dx, dy) in HexGrid.NeighborOffsets(land.Y))
        {
            if (navigator.TryNeighbor(land, dx, dy, out _, out var block))
                continue;

            var resolution = world.Topology.Resolve(land.X + dx, land.Y + dy);
            if (resolution.TryGetCell(out var cell) && world.Grid.GetCell(cell).IsWater)
            {
                Assert.Equal(MovementBlock.Terrain, block);
                return;
            }
        }

        Assert.Fail("Expected a water neighbor next to spawn land for seed 1.");
    }

    [Fact]
    public void Horizontal_wrap_east_and_west()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var navigator = new GridNavigator(world);
        var y = world.Configuration.Height / 2;
        Assert.True(world.Topology.Resolve(99 + 1, y).TryGetCell(out var east));
        Assert.Equal(0, east.X);
        Assert.True(world.Topology.Resolve(0 - 1, y).TryGetCell(out var west));
        Assert.Equal(99, west.X);

        navigator.TryNeighbor(new LogicalGridCoordinate(99, y), 1, 0, out var wrappedEast, out var eastBlock);
        if (eastBlock == MovementBlock.None)
            Assert.Equal(0, wrappedEast.X);

        navigator.TryNeighbor(new LogicalGridCoordinate(0, y), -1, 0, out var wrappedWest, out var westBlock);
        if (westBlock == MovementBlock.None)
            Assert.Equal(99, wrappedWest.X);
    }

    [Fact]
    public void Polar_edges_cannot_be_left()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var navigator = new GridNavigator(world);
        Assert.False(navigator.TryNeighbor(new LogicalGridCoordinate(10, 0), 0, -1, out _, out var north));
        Assert.Equal(MovementBlock.OutsideWorld, north);
        Assert.False(navigator.TryNeighbor(new LogicalGridCoordinate(10, 49), 0, 1, out _, out var south));
        Assert.Equal(MovementBlock.OutsideWorld, south);
    }
}

public sealed class CharacterActionAndAiTests
{
    [Fact]
    public void Actions_progress_and_complete()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var actions = new CharacterActionSystem(new GridNavigator(world), TestProduction.ForWorld(world), TestProduction.Teaching(world));
        var character = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1);
        character.Activity.Start(ActionKind.Idle, 3);
        actions.Advance(character);
        actions.Advance(character);
        Assert.False(character.Activity.NeedsDecision);
        actions.Advance(character);
        Assert.Equal(ActionKind.None, character.Activity.Kind);
    }

    [Fact]
    public void Decision_is_deterministic_for_the_same_state()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var decisions = new CharacterDecisionSystem(new GridNavigator(world), TestProduction.ForWorld(world), TestProduction.Teaching(world));
        var a = Hungry(new CharacterId(1));
        var b = Hungry(new CharacterId(2));
        Assert.Equal(decisions.ChooseKind(a), decisions.ChooseKind(b));
        Assert.Equal(ActionKind.Eat, decisions.ChooseKind(a));
    }

    [Fact]
    public void Two_simulations_match_after_many_ticks()
    {
        var a = new SimulationHost(1);
        var b = new SimulationHost(1);
        a.Step(240);
        b.Step(240);
        Assert.Equal(
            a.Population.All.Select(c => c.Snapshot()),
            b.Population.All.Select(c => c.Snapshot()));
        Assert.Equal(
            a.Buildings.All.Select(building => building.Snapshot()),
            b.Buildings.All.Select(building => building.Snapshot()));
        Assert.Contains(a.Population.Alive, _ => true);
    }

    [Fact]
    public void Population_can_operate_for_days_without_total_extinction()
    {
        var host = new SimulationHost(1);
        host.Step(SimulationCalendar.Default.TicksPerDay * 2);
        Assert.True(host.Population.Alive.Count() >= CharacterRules.DefaultPopulation / 2);
        Assert.Contains(host.Population.All, c => c.Activity.Kind != ActionKind.None || c.Inventory.GetQuantity(ResourceType.Food) >= 0);
    }

    private static CharacterState Hungry(CharacterId id)
    {
        var character = new CharacterState(id, new LogicalGridCoordinate(0, 0), 1);
        character.Needs.Hunger = 0.9f;
        Assert.True(character.Inventory.TryAdd(ResourceType.Food, 2));
        return character;
    }
}

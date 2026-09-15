using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void Add_remove_and_query_quantities()
    {
        var inventory = new Inventory(capacity: 10);
        Assert.True(inventory.TryAdd(ResourceType.Wood, 4));
        Assert.True(inventory.Has(ResourceType.Wood, 4));
        Assert.Equal(4, inventory.GetQuantity(ResourceType.Wood));
        Assert.True(inventory.TryRemove(ResourceType.Wood, 1));
        Assert.Equal(3, inventory.GetQuantity(ResourceType.Wood));
    }

    [Fact]
    public void Insufficient_and_invalid_operations_fail_deterministically()
    {
        var inventory = new Inventory(capacity: 2);
        Assert.False(inventory.TryAdd(ResourceType.Stone, 0));
        Assert.False(inventory.TryAdd(ResourceType.Stone, -1));
        Assert.False(inventory.TryAdd(ResourceType.Stone, 3));
        Assert.True(inventory.TryAdd(ResourceType.Stone, 2));
        Assert.False(inventory.TryRemove(ResourceType.Stone, 3));
        Assert.False(inventory.TryRemove(ResourceType.Food, 1));
        Assert.Equal(2, inventory.GetQuantity(ResourceType.Stone));
    }
}

public sealed class BuildingIdentityAndPlacementTests
{
    [Fact]
    public void Buildings_have_distinct_stable_ids()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var ids = host.Buildings.All.Select(b => b.Id).ToArray();
        Assert.True(ids.Length >= 4);
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.DoesNotContain(BuildingId.None, ids);
        Assert.Equal(1UL, ids[0].Value);
        Assert.NotEqual((ulong)0, ids[0].Value);
    }

    [Fact]
    public void Valid_placement_succeeds()
    {
        var host = EmptySite();
        var origin = FirstOpenLand(host);
        var result = host.Placement.TryPlace(BuildingTypeId.Farm, origin);
        Assert.True(result.Success);
        Assert.NotNull(result.Building);
        Assert.Equal(BuildingLifecycle.Active, result.Building!.Lifecycle);
        Assert.True(host.World.Grid.GetCell(origin).Occupancy.IsOccupied);
    }

    [Fact]
    public void Invalid_water_placement_fails()
    {
        var host = EmptySite();
        var water = FirstWater(host);
        var result = host.Placement.TryPlace(BuildingTypeId.Storage, water);
        Assert.False(result.Success);
        Assert.Equal(0, host.Buildings.Count);
    }

    [Fact]
    public void Occupied_placement_fails()
    {
        var host = EmptySite();
        var origin = FirstOpenLand(host);
        Assert.True(host.Placement.TryPlace(BuildingTypeId.Shelter, origin).Success);
        var second = host.Placement.TryPlace(BuildingTypeId.Farm, origin);
        Assert.False(second.Success);
    }

    [Fact]
    public void One_by_one_footprint_occupies_origin()
    {
        var host = EmptySite();
        var origin = FirstOpenLand(host);
        var building = host.Placement.TryPlace(BuildingTypeId.Storage, origin).Building!;
        Assert.Single(building.FootprintCells);
        Assert.Equal(origin, building.FootprintCells[0]);
    }

    [Fact]
    public void Multi_cell_footprint_wraps_horizontally()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var y = world.Configuration.Height / 2;
        LogicalGridCoordinate? origin = null;
        for (var x = world.Configuration.Width - 1; x >= 0; x--)
        {
            var cell = new LogicalGridCoordinate(x, y);
            var east = world.Topology.Resolve(x + 1, y);
            if (!east.TryGetCell(out var eastCell))
                continue;
            if (world.Grid.GetCell(cell).Generated.Passable && world.Grid.GetCell(eastCell).Generated.Passable)
            {
                origin = cell;
                break;
            }
        }

        Assert.True(origin.HasValue);
        var catalog = new BuildingCatalog(
        [
            new BuildingDefinition(
                new BuildingTypeId("wide"),
                "wide",
                BuildingFootprint.Rect(2, 1),
                occupiesCells: true,
                blocksMovement: true,
                workplaceCount: 0,
                recipe: null,
                storageCapacity: 8,
                isShelter: false,
                isStorage: false)
        ]);
        var buildings = new BuildingDirectory();
        var placement = new BuildingPlacementSystem(world, buildings, catalog, new EntityIdFactory());
        var result = placement.TryPlace(new BuildingTypeId("wide"), origin.Value);
        Assert.True(result.Success, result.Error);
        Assert.Equal(2, result.Building!.FootprintCells.Count);
        Assert.Contains(result.Building.FootprintCells, c => c.X == 0 || c.X == origin.Value.X);
        Assert.True(world.Grid.GetCell(result.Building.FootprintCells[0]).Occupancy.BlocksMovement);
        Assert.True(world.Grid.GetCell(result.Building.FootprintCells[1]).Occupancy.BlocksMovement);
    }

    [Fact]
    public void Removing_a_building_frees_occupied_cells()
    {
        var host = EmptySite();
        var origin = FirstOpenLand(host);
        var building = host.Placement.TryPlace(BuildingTypeId.Workshop, origin).Building!;
        Assert.True(host.Placement.TryRemove(building.Id).Success);
        Assert.False(host.World.Grid.GetCell(origin).Occupancy.IsOccupied);
        Assert.Equal(0, host.Buildings.Count);
    }

    private static SimulationHost EmptySite() =>
        new(7, populationCount: 1, placeDevelopmentBuildings: false);

    private static LogicalGridCoordinate FirstOpenLand(SimulationHost host)
    {
        for (var y = 0; y < host.World.Configuration.Height; y++)
        {
            for (var x = 0; x < host.World.Configuration.Width; x++)
            {
                var cell = new LogicalGridCoordinate(x, y);
                var terrain = host.World.Grid.GetCell(cell);
                if (terrain.Generated.Passable && !terrain.Occupancy.IsOccupied)
                    return cell;
            }
        }

        throw new InvalidOperationException("No open land.");
    }

    private static LogicalGridCoordinate FirstWater(SimulationHost host)
    {
        for (var y = 0; y < host.World.Configuration.Height; y++)
        {
            for (var x = 0; x < host.World.Configuration.Width; x++)
            {
                var cell = new LogicalGridCoordinate(x, y);
                if (host.World.Grid.GetCell(cell).IsWater)
                    return cell;
            }
        }

        throw new InvalidOperationException("No water.");
    }
}

public sealed class ProductionAndWorkplaceTests
{
    [Fact]
    public void Recipe_consumes_inputs_and_creates_outputs()
    {
        var inventory = new Inventory();
        Assert.True(inventory.TryAdd(ResourceType.Wood, 1));
        var recipe = new ProductionRecipe(
            new RecipeId("test.craft"),
            durationTicks: 3,
            inputs: [new ResourceStack(ResourceType.Wood, 1)],
            outputs: [new ResourceStack(ResourceType.Food, 2)]);
        Assert.True(recipe.TryExecute(inventory, recipe.Outputs));
        Assert.Equal(0, inventory.GetQuantity(ResourceType.Wood));
        Assert.Equal(2, inventory.GetQuantity(ResourceType.Food));
        Assert.False(recipe.TryExecute(inventory, recipe.Outputs));
    }

    [Fact]
    public void Production_does_not_complete_before_duration()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var farm = host.Buildings.All.First(b => b.TypeId == BuildingTypeId.Farm);
        var character = host.Population[0];
        character.Position = farm.AccessCell;
        var workplace = new WorkplaceId(farm.Id, 0);
        host.Production.AssignWorkplace(character, workplace);
        Assert.True(host.Production.TryEvaluate(farm, character, out var evaluation));
        host.Production.BeginWork(farm, character);
        character.Activity.Start(ActionKind.Work, evaluation.DurationTicks, farm.AccessCell);

        for (var i = 0; i < evaluation.DurationTicks - 1; i++)
            host.Characters.Actions.Advance(character);

        Assert.Equal(0, host.Buildings.All.First(b => b.Definition.IsStorage).Inventory.GetQuantity(ResourceType.Food));
        Assert.Equal(ActionKind.Work, character.Activity.Kind);

        host.Characters.Actions.Advance(character);
        Assert.Equal(ActionKind.None, character.Activity.Kind);
        Assert.True(host.Buildings.All.First(b => b.Definition.IsStorage).Inventory.GetQuantity(ResourceType.Food) >= 1);
    }

    [Fact]
    public void Character_finds_workplace_moves_and_works()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var character = host.Population[0];
        var workplace = host.Production.FindFreeWorkplace(character);
        Assert.NotNull(workplace);
        host.Production.AssignWorkplace(character, workplace.Value);
        var access = host.Production.AccessFor(workplace.Value);
        Assert.NotNull(access);
        var path = host.Characters.Navigator.FindPath(character.Position, access.Value);
        Assert.NotNull(path);
        character.Activity.Start(ActionKind.Move, path!.Count, access);
        foreach (var step in path)
            character.Activity.RemainingPath.Enqueue(step);
        while (character.Activity.Kind == ActionKind.Move)
            host.Characters.Actions.Advance(character);
        Assert.Equal(access, character.Position);
        host.Characters.Decisions.AssignNext(character);
        Assert.Equal(ActionKind.Work, character.Activity.Kind);
    }

    [Fact]
    public void Produced_food_reaches_storage_and_can_be_eaten()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var farm = host.Buildings.All.First(b => b.TypeId == BuildingTypeId.Farm);
        var storage = host.Buildings.All.First(b => b.Definition.IsStorage);
        var character = host.Population[0];
        character.Position = farm.AccessCell;
        host.Production.AssignWorkplace(character, new WorkplaceId(farm.Id, 0));
        Assert.True(host.Production.TryEvaluate(farm, character, out var evaluation));
        host.Production.BeginWork(farm, character);
        character.Activity.Start(ActionKind.Work, evaluation.DurationTicks, farm.AccessCell);
        for (var i = 0; i < evaluation.DurationTicks; i++)
            host.Characters.Actions.Advance(character);

        Assert.True(storage.Inventory.Has(ResourceType.Food, 1));
        character.Position = storage.AccessCell;
        character.Needs.Hunger = 0.9f;
        host.Characters.Decisions.AssignNext(character);
        Assert.Equal(ActionKind.Eat, character.Activity.Kind);
        for (var i = 0; i < CharacterRules.EatDurationTicks; i++)
            host.Characters.Actions.Advance(character);
        Assert.True(character.Needs.Hunger < 0.9f);
        Assert.Equal(0, character.Inventory.GetQuantity(ResourceType.Food));
    }

    [Fact]
    public void No_food_means_eating_cannot_occur()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var decisions = new CharacterDecisionSystem(new GridNavigator(world), TestProduction.ForWorld(world), TestProduction.Teaching(world));
        var character = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1);
        character.Needs.Hunger = 0.95f;
        Assert.NotEqual(ActionKind.Eat, decisions.ChooseKind(character));
    }

    [Fact]
    public void Character_uses_shelter_to_sleep()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var shelter = host.Buildings.All.First(b => b.Definition.IsShelter);
        var character = host.Population[0];
        character.Needs.Fatigue = 0.95f;
        character.Position = shelter.AccessCell;
        host.Characters.Decisions.AssignNext(character);
        Assert.Equal(ActionKind.Sleep, character.Activity.Kind);
        var before = character.Needs.Fatigue;
        for (var i = 0; i < CharacterRules.SleepDurationTicks; i++)
            host.Characters.Actions.Advance(character);
        Assert.True(character.Needs.Fatigue < before);
    }

    [Fact]
    public void Buildings_block_movement()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var origin = PopulationSpawner.FindLandOrigin(host.World);
        var building = host.Placement.TryPlace(BuildingTypeId.Workshop, origin).Building!;
        var blocked = building.Origin;
        var access = building.AccessCell;
        Assert.False(host.Characters.Navigator.IsPassable(blocked));
        Assert.True(host.Characters.Navigator.TryNeighbor(access, blocked.X - access.X, blocked.Y - access.Y, out _, out var block)
            || block == MovementBlock.Occupancy);
        host.Characters.Navigator.TryNeighbor(
            access,
            host.World.Topology.SignedHorizontalDelta(access.X, blocked.X),
            blocked.Y - access.Y,
            out _,
            out var occupancyBlock);
        Assert.Equal(MovementBlock.Occupancy, occupancyBlock);
    }

    [Fact]
    public void Two_hosts_match_after_production_ticks()
    {
        var a = new SimulationHost(1);
        var b = new SimulationHost(1);
        a.Step(480);
        b.Step(480);
        Assert.Equal(a.Buildings.All.Select(x => x.Snapshot()), b.Buildings.All.Select(x => x.Snapshot()));
        Assert.Equal(a.Population.All.Select(x => x.Snapshot()), b.Population.All.Select(x => x.Snapshot()));
    }
}

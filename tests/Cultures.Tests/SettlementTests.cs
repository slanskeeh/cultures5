using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

using static Cultures.Tests.SettlementTestSupport;

namespace Cultures.Tests;

public sealed class SettlementEmergenceTests
{
    [Fact]
    public void Clustered_people_and_infrastructure_form_an_emerging_settlement()
    {
        var host = new SimulationHost(1);
        host.SettlementDetection.Evaluate();
        Assert.NotEmpty(host.Settlements.All);
        var settlement = host.Settlements[0];
        Assert.Equal(SettlementLifecycle.Emerging, settlement.Lifecycle);
        Assert.True(settlement.Statistics.Population >= SettlementRules.MinPeople);
        Assert.True(settlement.Statistics.Shelters >= 1);
        Assert.True(settlement.Statistics.StorageBuildings >= 1);
        Assert.Contains(host.Population.Alive, c => c.Settlement == settlement.Id);
        Assert.All(host.Buildings.All, b => Assert.True(b.AssociatedSettlement.IsAssigned));
        Assert.Equal(new SettlementId(1), settlement.Id);
    }

    [Fact]
    public void Isolated_people_without_infrastructure_do_not_form_a_settlement()
    {
        var host = new SimulationHost(1, populationCount: 3, placeDevelopmentBuildings: false);
        host.SettlementDetection.Evaluate();
        Assert.Empty(host.Settlements.All);
        Assert.All(host.Population.All, c => Assert.Equal(SettlementId.None, c.Settlement));
    }

    [Fact]
    public void People_without_shelter_and_storage_do_not_form_a_settlement()
    {
        var host = new SimulationHost(1, populationCount: 8, placeDevelopmentBuildings: false);
        var origin = host.Population[0].Position;
        PlaceNear(host, BuildingTypeId.Farm, origin);
        PlaceNear(host, BuildingTypeId.Workshop, origin);
        host.SettlementDetection.Evaluate();
        Assert.Empty(host.Settlements.All);
    }

    [Fact]
    public void Emergence_is_deterministic()
    {
        var a = new SimulationHost(1);
        var b = new SimulationHost(1);
        a.SettlementDetection.Evaluate();
        b.SettlementDetection.Evaluate();
        Assert.Equal(
            a.Settlements.All.Select(s => s.Snapshot()),
            b.Settlements.All.Select(s => s.Snapshot()));
        Assert.Equal(
            a.Population.All.Select(c => c.Settlement),
            b.Population.All.Select(c => c.Settlement));
    }

    [Fact]
    public void Persistence_is_required_before_established()
    {
        var host = new SimulationHost(1);
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Emerging, host.Settlements[0].Lifecycle);
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Emerging, host.Settlements[0].Lifecycle);
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Established, host.Settlements[0].Lifecycle);
        Assert.Equal(SettlementRules.EstablishedAfterEvals, host.Settlements[0].PresenceEvals);
    }

    [Fact]
    public void Evaluation_interval_uses_a_tick_counter_not_clock_modulo()
    {
        var host = new SimulationHost(1);
        host.Step(SettlementRules.EvaluationIntervalTicks);
        Assert.Equal(1, host.Settlements.Count);
        Assert.Equal(1, host.Settlements[0].PresenceEvals);
    }
}

public sealed class SettlementMembershipTests
{
    [Fact]
    public void Distant_person_is_not_a_member()
    {
        var host = new SimulationHost(1, populationCount: 5, placeDevelopmentBuildings: false);
        var origin = host.Population[0].Position;
        PlaceNear(host, BuildingTypeId.Shelter, origin);
        PlaceNear(host, BuildingTypeId.Storage, origin);
        var outsider = host.Population[4];
        outsider.Position = FarPassable(host, origin);
        host.SettlementDetection.Evaluate();
        Assert.Single(host.Settlements.All);
        Assert.Equal(host.Settlements[0].Id, host.Population[0].Settlement);
        Assert.Equal(SettlementId.None, outsider.Settlement);
        Assert.Equal(4, host.Settlements[0].Statistics.Population);
    }

    [Fact]
    public void Family_links_do_not_force_shared_settlement()
    {
        var host = new SimulationHost(1, populationCount: 6, placeDevelopmentBuildings: false);
        var origin = host.Population[0].Position;
        PlaceNear(host, BuildingTypeId.Shelter, origin);
        PlaceNear(host, BuildingTypeId.Storage, origin);
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        child!.Position = FarPassable(host, origin);
        host.SettlementDetection.Evaluate();
        Assert.True(FamilyQueries.AreParentAndChild(host.Population[0], child));
        Assert.NotEqual(host.Population[0].Settlement, child.Settlement);
        Assert.Equal(HouseholdId.None, child.Household);
    }
}

public sealed class SettlementLifecycleTests
{
    [Fact]
    public void Established_declines_and_abandons_with_hysteresis()
    {
        var host = new SimulationHost(1);
        Establish(host);
        var settlement = host.Settlements[0];
        var id = settlement.Id;
        Assert.Equal(SettlementLifecycle.Established, settlement.Lifecycle);

        ScatterAlive(host);
        host.SettlementDetection.Evaluate();
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Established, settlement.Lifecycle);

        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Declining, settlement.Lifecycle);
        Assert.Contains(host.Buildings.All, b => b.AssociatedSettlement == id);

        host.SettlementDetection.Evaluate();
        host.SettlementDetection.Evaluate();
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Abandoned, settlement.Lifecycle);
        Assert.Equal(id, host.Settlements[0].Id);
        Assert.All(host.Population.Alive, c => Assert.NotEqual(id, c.Settlement));
    }

    [Fact]
    public void Abandoned_area_can_form_a_new_settlement_identity()
    {
        var host = new SimulationHost(1);
        Establish(host);
        var abandoned = host.Settlements[0].Id;
        ScatterAlive(host);
        for (var i = 0; i < SettlementRules.AbandonAfterUnmatchedEvals; i++)
            host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Abandoned, host.Settlements[0].Lifecycle);

        var origin = host.Buildings[0].Origin;
        foreach (var character in host.Population.Alive.Take(6))
            character.Position = origin;
        host.SettlementDetection.Evaluate();
        Assert.True(host.Settlements.Count >= 2);
        var live = host.Settlements.Active.Single();
        Assert.NotEqual(abandoned, live.Id);
        Assert.Equal(SettlementLifecycle.Emerging, live.Lifecycle);
    }

    [Fact]
    public void Recovery_during_decline_restores_established()
    {
        var host = new SimulationHost(1);
        Establish(host);
        var home = host.Population[0].Position;
        ScatterAlive(host);
        for (var i = 0; i < SettlementRules.DeclineAfterUnmatchedEvals; i++)
            host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Declining, host.Settlements[0].Lifecycle);

        foreach (var character in host.Population.Alive)
            character.Position = home;
        host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Established, host.Settlements[0].Lifecycle);
        Assert.Equal(0, host.Settlements[0].UnmatchedEvals);
    }
}

public sealed class SettlementSpatialTests
{
    [Fact]
    public void Seam_spanning_population_forms_one_settlement()
    {
        var host = new SimulationHost(1, populationCount: 4, placeDevelopmentBuildings: false);
        Assert.True(TryFindWrapPair(host, out var west, out var east), "Expected passable cells on both wrap-adjacent chunks.");
        host.Population[0].Position = west;
        host.Population[1].Position = west;
        host.Population[2].Position = east;
        host.Population[3].Position = east;
        PlaceNear(host, BuildingTypeId.Shelter, west);
        PlaceNear(host, BuildingTypeId.Storage, east);
        host.SettlementDetection.Evaluate();
        Assert.Single(host.Settlements.All);
        Assert.Equal(4, host.Settlements[0].Statistics.Population);
        Assert.All(host.Population.Alive, c => Assert.Equal(host.Settlements[0].Id, c.Settlement));
    }

    [Fact]
    public void Distant_clusters_do_not_merge()
    {
        var host = new SimulationHost(1, populationCount: 8, placeDevelopmentBuildings: false);
        var origin = host.Population[0].Position;
        var far = FarPassable(host, origin);
        for (var i = 0; i < 4; i++)
            host.Population[i].Position = origin;
        for (var i = 4; i < 8; i++)
            host.Population[i].Position = far;
        PlaceNear(host, BuildingTypeId.Shelter, origin);
        PlaceNear(host, BuildingTypeId.Storage, origin);
        PlaceNear(host, BuildingTypeId.Shelter, far);
        PlaceNear(host, BuildingTypeId.Storage, far);
        host.SettlementDetection.Evaluate();
        Assert.Equal(2, host.Settlements.Count);
        var first = host.Population[0].Settlement;
        var second = host.Population[4].Settlement;
        Assert.True(first.IsAssigned);
        Assert.True(second.IsAssigned);
        Assert.NotEqual(first, second);
    }
}

public sealed class SettlementStatisticsTests
{
    [Fact]
    public void Derived_stats_count_people_shelters_food_and_workers()
    {
        var host = new SimulationHost(1, populationCount: 6, placeDevelopmentBuildings: false);
        var origin = host.Population[0].Position;
        foreach (var character in host.Population.All)
            character.Position = origin;
        var shelter = PlaceNear(host, BuildingTypeId.Shelter, origin);
        var storage = PlaceNear(host, BuildingTypeId.Storage, origin);
        var farm = PlaceNear(host, BuildingTypeId.Farm, origin);
        Assert.True(storage.Inventory.TryAdd(ResourceType.Food, 7));
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        child!.Position = origin;
        host.Population[0].Position = farm.AccessCell;
        host.Production.AssignWorkplace(host.Population[0], new WorkplaceId(farm.Id, 0));
        host.SettlementDetection.Evaluate();

        var stats = host.Settlements[0].Statistics;
        Assert.Equal(host.Population.Alive.Count(), stats.Population);
        Assert.Equal(1, stats.Infants);
        Assert.Equal(1, stats.Shelters);
        Assert.Equal(1, stats.ShelterCapacity);
        Assert.Equal(7, stats.FoodStored);
        Assert.Equal(1, stats.Workers);
        Assert.True(stats.UnemployedAdults >= 1);
        Assert.Equal(1, stats.EstimatedFoodProduction);
        Assert.Equal(CultureId.Neutral, host.Settlements[0].Culture);
        Assert.Equal(CharacterId.None, host.Settlements[0].Leader);
        Assert.Equal(shelter.AssociatedSettlement, host.Settlements[0].Id);
    }

    [Fact]
    public void Command_forces_detection()
    {
        var host = new SimulationHost(1);
        Assert.Empty(host.Settlements.All);
        Assert.True(host.Commands.Execute(new EvaluateSettlementsCommand()).Success);
        Assert.NotEmpty(host.Settlements.All);
    }
}

internal static class SettlementTestSupport
{
    public static void Establish(SimulationHost host)
    {
        for (var i = 0; i < SettlementRules.EstablishedAfterEvals; i++)
            host.SettlementDetection.Evaluate();
        Assert.Equal(SettlementLifecycle.Established, host.Settlements[0].Lifecycle);
    }

    public static BuildingState PlaceNear(SimulationHost host, BuildingTypeId type, LogicalGridCoordinate origin)
    {
        foreach (var cell in Spiral(host.World, origin))
        {
            var result = host.Placement.TryPlace(type, cell);
            if (result.Success && result.Building is not null)
                return result.Building;
        }

        throw new InvalidOperationException($"Could not place {type.Value} near {origin}.");
    }

    public static LogicalGridCoordinate FarPassable(SimulationHost host, LogicalGridCoordinate origin)
    {
        var navigator = host.Characters.Navigator;
        var width = host.World.Configuration.Width;
        var height = host.World.Configuration.Height;
        for (var radius = 20; radius < Math.Max(width, height); radius += 5)
        {
            foreach (var cell in Spiral(host.World, origin, radius, radius + 4))
            {
                if (!navigator.IsPassable(cell))
                    continue;
                if (host.World.Topology.HorizontalDistance(origin, cell) + Math.Abs(origin.Y - cell.Y) < 25)
                    continue;
                if (SameOrNeighborChunk(host, origin, cell))
                    continue;
                return cell;
            }
        }

        throw new InvalidOperationException("No distant passable cell found.");
    }

    public static void ScatterAlive(SimulationHost host)
    {
        var origin = host.Population[0].Position;
        var far = FarPassable(host, origin);
        foreach (var character in host.Population.Alive)
            character.Position = far;
    }

    public static bool TryFindWrapPair(
        SimulationHost host,
        out LogicalGridCoordinate west,
        out LogicalGridCoordinate east)
    {
        var navigator = host.Characters.Navigator;
        var width = host.World.Configuration.Width;
        var chunkWidth = host.World.Configuration.ChunkWidth;
        for (var y = 0; y < host.World.Configuration.Height; y++)
        {
            west = new LogicalGridCoordinate(0, y);
            east = new LogicalGridCoordinate(width - 1, y);
            if (navigator.IsPassable(west) && navigator.IsPassable(east))
                return true;
        }

        for (var y = 0; y < host.World.Configuration.Height; y++)
        {
            LogicalGridCoordinate? foundWest = null;
            LogicalGridCoordinate? foundEast = null;
            for (var x = 0; x < chunkWidth; x++)
            {
                var cell = new LogicalGridCoordinate(x, y);
                if (navigator.IsPassable(cell))
                {
                    foundWest = cell;
                    break;
                }
            }

            for (var x = width - chunkWidth; x < width; x++)
            {
                var cell = new LogicalGridCoordinate(x, y);
                if (navigator.IsPassable(cell))
                {
                    foundEast = cell;
                    break;
                }
            }

            if (foundWest is { } w && foundEast is { } e)
            {
                west = w;
                east = e;
                return true;
            }
        }

        west = default;
        east = default;
        return false;
    }

    private static bool SameOrNeighborChunk(SimulationHost host, LogicalGridCoordinate a, LogicalGridCoordinate b)
    {
        var ca = host.World.Chunks.ToAddress(a).Chunk;
        var cb = host.World.Chunks.ToAddress(b).Chunk;
        var dx = Math.Min(
            Math.Abs(ca.X - cb.X),
            host.World.Configuration.ChunkCountX - Math.Abs(ca.X - cb.X));
        var dy = Math.Abs(ca.Y - cb.Y);
        return dx <= 1 && dy <= 1;
    }

    private static IEnumerable<LogicalGridCoordinate> Spiral(
        LogicalWorld world,
        LogicalGridCoordinate origin,
        int minRadius = 0,
        int maxRadius = 18)
    {
        if (minRadius <= 0)
            yield return origin;

        for (var radius = Math.Max(1, minRadius); radius <= maxRadius; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    var resolution = world.Topology.Resolve(origin.X + dx, origin.Y + dy);
                    if (resolution.TryGetCell(out var cell))
                        yield return cell;
                }
            }
        }
    }
}

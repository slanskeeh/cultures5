using Cultures.Application;
using Cultures.Buildings;
using Cultures.World;

namespace Cultures.Tests;

public sealed class BuildingFootprintTests
{
    [Fact]
    public void Disk_and_rect_counts_match_hex_geometry()
    {
        Assert.Equal(1, BuildingFootprint.Cell1x1.CellCount);
        Assert.Equal(7, BuildingFootprint.Disk(1).CellCount);
        Assert.Equal(19, HexGrid.Disk(2).Count);
        Assert.Equal(6, BuildingFootprint.Rect(3, 2).CellCount);
        Assert.Equal(6, BuildingCatalog.Development.Get(BuildingTypeId.Workshop).Footprint.CellCount);
    }

    [Fact]
    public void Branched_shape_is_hex_connected_on_even_and_odd_rows()
    {
        var topology = new WorldTopology(WorldConfiguration.DebugSample);
        var footprint = BuildingCatalog.Development.Get(BuildingTypeId.Workshop).Footprint;
        Assert.True(footprint.TryMaterialize(topology, new LogicalGridCoordinate(10, 8), out var even));
        Assert.True(footprint.TryMaterialize(topology, new LogicalGridCoordinate(10, 9), out var odd));
        Assert.Equal(footprint.CellCount, even.Count);
        Assert.Equal(footprint.CellCount, odd.Count);
        Assert.Equal(even.Count, even.Distinct().Count());
        AssertHexConnected(topology, even);
        AssertHexConnected(topology, odd);
    }

    [Fact]
    public void Disconnected_cube_offsets_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => BuildingFootprint.FromCubeOffsets((0, 0), (3, 0)));
    }

    [Fact]
    public void Development_farm_occupies_a_seven_hex_disk()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var farm = SettlementTestSupport.PlaceNear(host, BuildingTypeId.Farm, host.Population[0].Position);
        Assert.Equal(7, farm.FootprintCells.Count);
        Assert.Contains(farm.Origin, farm.FootprintCells);
    }

    private static void AssertHexConnected(WorldTopology topology, List<LogicalGridCoordinate> cells)
    {
        var set = cells.ToHashSet();
        var seen = new HashSet<LogicalGridCoordinate>();
        var queue = new Queue<LogicalGridCoordinate>();
        queue.Enqueue(cells[0]);
        seen.Add(cells[0]);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in topology.HexNeighbors(current))
            {
                if (set.Contains(neighbor) && seen.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        Assert.Equal(set.Count, seen.Count);
    }
}

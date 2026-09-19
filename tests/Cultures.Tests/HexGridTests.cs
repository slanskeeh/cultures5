using Cultures.Application;
using Cultures.Buildings;
using Cultures.World;

namespace Cultures.Tests;

public sealed class HexGridTests
{
    [Fact]
    public void Each_cell_has_six_hex_neighbors()
    {
        var topology = new WorldTopology(WorldConfiguration.DebugSample);
        var even = topology.HexNeighbors(new LogicalGridCoordinate(10, 8)).ToArray();
        var odd = topology.HexNeighbors(new LogicalGridCoordinate(10, 9)).ToArray();
        Assert.Equal(6, even.Length);
        Assert.Equal(6, odd.Length);
        Assert.Equal(6, even.Distinct().Count());
        Assert.Contains(new LogicalGridCoordinate(11, 8), even);
        Assert.Contains(new LogicalGridCoordinate(11, 9), odd);
        Assert.DoesNotContain(new LogicalGridCoordinate(10, 8), even);
    }

    [Fact]
    public void East_west_wrap_is_a_hex_neighbor()
    {
        var topology = new WorldTopology(WorldConfiguration.DebugSample);
        var west = new LogicalGridCoordinate(0, 10);
        var east = new LogicalGridCoordinate(99, 10);
        Assert.Contains(east, topology.HexNeighbors(west));
        Assert.Contains(west, topology.HexNeighbors(east));
        Assert.Equal(1, topology.HexDistance(west, east));
    }

    [Fact]
    public void Hex_distance_is_not_square_chebyshev()
    {
        var topology = new WorldTopology(WorldConfiguration.DebugSample);
        var a = new LogicalGridCoordinate(4, 4);
        var diagonal = new LogicalGridCoordinate(5, 5);
        Assert.Equal(1, topology.HexDistance(a, new LogicalGridCoordinate(5, 4)));
        Assert.True(topology.HexDistance(a, diagonal) <= 2);
        Assert.Equal(0, topology.HexDistance(a, a));
    }

    [Fact]
    public void Navigator_finds_a_hex_path_across_open_land()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var from = host.Population[0].Position;
        var farm = host.Buildings.All.First(b => b.TypeId == BuildingTypeId.Farm);
        var path = host.Characters.Navigator.FindPath(from, farm.AccessCell);
        Assert.NotNull(path);
        Assert.Equal(farm.AccessCell, path![^1]);
        Assert.True(path.Count >= host.World.Topology.HexDistance(from, farm.AccessCell));
    }

    [Fact]
    public void Cube_roundtrip_preserves_offset_cells()
    {
        foreach (var cell in new[]
                 {
                     new LogicalGridCoordinate(0, 0),
                     new LogicalGridCoordinate(7, 3),
                     new LogicalGridCoordinate(12, 9)
                 })
        {
            Assert.Equal(cell, HexGrid.FromCube(HexGrid.ToCube(cell)));
        }
    }
}

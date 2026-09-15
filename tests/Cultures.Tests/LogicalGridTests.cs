using Cultures.World;

namespace Cultures.Tests;

public sealed class LogicalGridTests
{
    [Fact]
    public void Get_and_set_cell_roundtrip()
    {
        var grid = new LogicalGrid(new WorldTopology(WorldConfiguration.DebugSample));
        var cell = new LogicalGridCoordinate(12, 7);
        var terrain = new TerrainCell(TerrainKind.Unspecified, Passable: false, Occupancy.DebugMarker);

        grid.SetCell(cell, terrain);
        Assert.Equal(terrain, grid.GetCell(cell));
        Assert.True(grid.TryGetCell(cell.ToWorld(), out var fetched));
        Assert.Equal(terrain, fetched);
    }

    [Fact]
    public void IsInside_matches_topology()
    {
        var grid = new LogicalGrid(new WorldTopology(WorldConfiguration.DebugSample));
        Assert.True(grid.IsInside(new WorldCoordinate(0, 0)));
        Assert.False(grid.IsInside(new WorldCoordinate(0, -1)));
        Assert.True(grid.IsInside(new WorldCoordinate(100, 0)));
    }

    [Fact]
    public void TrySetCell_rejects_outside_world()
    {
        var grid = new LogicalGrid(new WorldTopology(WorldConfiguration.DebugSample));
        Assert.False(grid.TrySetCell(new WorldCoordinate(1, -1), TerrainCell.Empty));
        Assert.False(grid.TryGetCell(new WorldCoordinate(1, 50), out _));
    }
}

using Cultures.World;

namespace Cultures.Tests;

public sealed class LogicalGridTests
{
    private static LogicalGrid Grid() => new LogicalWorld(WorldConfiguration.DebugSample, seed: 1).Grid;

    [Fact]
    public void Occupancy_overlay_does_not_replace_generated_terrain()
    {
        var grid = Grid();
        var cell = new LogicalGridCoordinate(12, 7);
        var before = grid.GetCell(cell);

        Assert.True(grid.TrySetOccupancy(cell.ToWorld(), Occupancy.DebugMarker));
        var after = grid.GetCell(cell);

        Assert.Equal(before.Generated, after.Generated);
        Assert.Equal(Occupancy.DebugMarker, after.Occupancy);
        Assert.True(grid.TryGetCell(cell.ToWorld(), out var fetched));
        Assert.Equal(after, fetched);
    }

    [Fact]
    public void IsInside_matches_topology()
    {
        var grid = Grid();
        Assert.True(grid.IsInside(new WorldCoordinate(0, 0)));
        Assert.False(grid.IsInside(new WorldCoordinate(0, -1)));
        Assert.True(grid.IsInside(new WorldCoordinate(100, 0)));
    }

    [Fact]
    public void TryGetCell_rejects_outside_world()
    {
        var grid = Grid();
        Assert.False(grid.TrySetOccupancy(new WorldCoordinate(1, -1), Occupancy.DebugMarker));
        Assert.False(grid.TryGetCell(new WorldCoordinate(1, 50), out _));
    }
}

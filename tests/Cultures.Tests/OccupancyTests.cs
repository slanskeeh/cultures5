using Cultures.World;

namespace Cultures.Tests;

public sealed class OccupancyTests
{
    private static LogicalGrid Grid() => new(new WorldTopology(WorldConfiguration.DebugSample));

    [Fact]
    public void Empty_cell_is_not_occupied()
    {
        var grid = Grid();
        Assert.True(grid.TryIsOccupied(new WorldCoordinate(3, 4), out var occupied));
        Assert.False(occupied);
        Assert.True(grid.TryGetOccupancy(new WorldCoordinate(3, 4), out var occupancy));
        Assert.Equal(Occupancy.Empty, occupancy);
    }

    [Fact]
    public void Setting_and_clearing_occupancy()
    {
        var grid = Grid();
        var position = new WorldCoordinate(8, 8);

        Assert.True(grid.TrySetOccupancy(position, Occupancy.DebugMarker));
        Assert.True(grid.TryIsOccupied(position, out var occupied));
        Assert.True(occupied);

        Assert.True(grid.TryClearOccupancy(position));
        Assert.True(grid.TryIsOccupied(position, out occupied));
        Assert.False(occupied);
    }

    [Fact]
    public void Occupancy_wraps_with_x()
    {
        var grid = Grid();
        Assert.True(grid.TrySetOccupancy(new WorldCoordinate(-1, 2), Occupancy.DebugMarker));
        Assert.True(grid.TryIsOccupied(new WorldCoordinate(99, 2), out var occupied));
        Assert.True(occupied);
    }

    [Fact]
    public void Occupancy_queries_on_invalid_positions_fail()
    {
        var grid = Grid();
        Assert.False(grid.TryGetOccupancy(new WorldCoordinate(0, -1), out _));
        Assert.False(grid.TrySetOccupancy(new WorldCoordinate(0, 50), Occupancy.DebugMarker));
        Assert.False(grid.TryClearOccupancy(new WorldCoordinate(4, -8)));
        Assert.False(grid.TryIsOccupied(new WorldCoordinate(0, 50), out var occupied));
        Assert.False(occupied);
    }
}

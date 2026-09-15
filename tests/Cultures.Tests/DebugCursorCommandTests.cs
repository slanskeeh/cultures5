using Cultures.Application;
using Cultures.World;
using Cultures.World.Commands;

namespace Cultures.Tests;

public sealed class DebugCursorCommandTests
{
    [Fact]
    public void Cursor_wraps_across_the_horizontal_seam()
    {
        var host = new SimulationHost(1);
        host.Cursor.TryMove(99, 0);
        Assert.Equal(new LogicalGridCoordinate(99, host.World.Configuration.Height / 2), host.Cursor.Position);

        var seam = false;
        host.Events.Subscribe<DebugCursorMovedEvent>(e => seam = e.CrossedHorizontalSeam);

        var result = host.Commands.Execute(new MoveDebugCursorCommand(1, 0));
        Assert.True(result.Success);
        Assert.Equal(new LogicalGridCoordinate(0, host.World.Configuration.Height / 2), host.Cursor.Position);
        Assert.True(seam);
    }

    [Fact]
    public void Cursor_wraps_west_from_zero()
    {
        var host = new SimulationHost(1);
        var result = host.Commands.Execute(new MoveDebugCursorCommand(-1, 0));
        Assert.True(result.Success);
        Assert.Equal(99, host.Cursor.Position.X);
    }

    [Fact]
    public void Cursor_refuses_north_and_south_edges()
    {
        var host = new SimulationHost(1);
        host.Cursor.TryMove(0, -host.Cursor.Position.Y);
        Assert.Equal(0, host.Cursor.Position.Y);

        var south = host.Commands.Execute(new MoveDebugCursorCommand(0, -1));
        Assert.False(south.Success);
        Assert.Equal(0, host.Cursor.Position.Y);

        host.Cursor.TryMove(0, 49);
        Assert.Equal(49, host.Cursor.Position.Y);
        var north = host.Commands.Execute(new MoveDebugCursorCommand(0, 1));
        Assert.False(north.Success);
        Assert.Equal(49, host.Cursor.Position.Y);
    }

    [Fact]
    public void Occupancy_command_sets_and_rejects_invalid_cells()
    {
        var host = new SimulationHost(1);
        var ok = host.Commands.Execute(new SetOccupancyCommand(new WorldCoordinate(4, 4), Occupancy.DebugMarker));
        Assert.True(ok.Success);
        Assert.True(host.World.Grid.TryIsOccupied(new WorldCoordinate(4, 4), out var occupied));
        Assert.True(occupied);

        var fail = host.Commands.Execute(new SetOccupancyCommand(new WorldCoordinate(4, -1), Occupancy.DebugMarker));
        Assert.False(fail.Success);
    }
}

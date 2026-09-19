using Cultures.World;

namespace Cultures.Tests;

public sealed class WrapDistanceTests
{
    [Fact]
    public void Horizontal_distance_uses_shortest_wrapped_path()
    {
        var topology = new WorldTopology(WorldConfiguration.DebugSample);
        Assert.Equal(1, topology.HorizontalDistance(99, 0));
        Assert.Equal(1, topology.HorizontalDistance(0, 99));
        Assert.Equal(0, topology.HorizontalDistance(7, 7));
        Assert.Equal(1, topology.HorizontalDistance(0, 1));
        Assert.Equal(50, topology.HorizontalDistance(0, 50));
        Assert.Equal(1, topology.HorizontalDistance(-1, 0));
        Assert.Equal(1, topology.HexDistance(new LogicalGridCoordinate(0, 4), new LogicalGridCoordinate(99, 4)));
    }
}

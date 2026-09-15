using System.Text.Json;
using Cultures.World;

namespace Cultures.Tests;

public sealed class WorldCoordinateTests
{
    private static WorldTopology Topology() => new(WorldConfiguration.DebugSample);

    [Fact]
    public void Equal_coordinates_are_equal()
    {
        Assert.Equal(new WorldCoordinate(10, 20), new WorldCoordinate(10, 20));
        Assert.Equal(new LogicalGridCoordinate(10, 20), new LogicalGridCoordinate(10, 20));
        Assert.Equal(new WorldCoordinate(10, 20).GetHashCode(), new WorldCoordinate(10, 20).GetHashCode());
    }

    [Fact]
    public void Different_coordinates_are_not_equal()
    {
        Assert.NotEqual(new WorldCoordinate(10, 20), new WorldCoordinate(11, 20));
        Assert.NotEqual(new WorldCoordinate(10, 20), new WorldCoordinate(10, 21));
    }

    [Theory]
    [InlineData(-1, 99)]
    [InlineData(0, 0)]
    [InlineData(99, 99)]
    [InlineData(100, 0)]
    [InlineData(101, 1)]
    [InlineData(-101, 99)]
    public void Horizontal_wrap_for_width_100(int inputX, int expectedX)
    {
        var topology = Topology();
        Assert.Equal(expectedX, topology.WrapX(inputX));
        Assert.Equal(expectedX, topology.Resolve(inputX, 10).HorizontallyNormalized.X);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(49, true)]
    [InlineData(-1, false)]
    [InlineData(50, false)]
    public void Vertical_bounds_for_height_50(int y, bool inside)
    {
        var topology = Topology();
        Assert.Equal(inside, topology.IsInsideY(y));
        Assert.Equal(inside, topology.IsInside(new WorldCoordinate(10, y)));
    }

    [Fact]
    public void Normalization_wraps_x_and_does_not_clamp_invalid_y()
    {
        var topology = Topology();

        var negativeX = topology.Resolve(-1, 10);
        Assert.True(negativeX.IsInsideWorld);
        Assert.Equal(new WorldCoordinate(99, 10), negativeX.HorizontallyNormalized);

        var overflowX = topology.Resolve(100, 10);
        Assert.Equal(new WorldCoordinate(0, 10), overflowX.HorizontallyNormalized);

        var further = topology.Resolve(101, 10);
        Assert.Equal(new WorldCoordinate(1, 10), further.HorizontallyNormalized);

        var south = topology.Resolve(50, -1);
        Assert.False(south.IsInsideWorld);
        Assert.Equal(50, south.HorizontallyNormalized.X);
        Assert.Equal(-1, south.HorizontallyNormalized.Y);
        Assert.False(south.TryGetCell(out _));

        var north = topology.Resolve(50, 50);
        Assert.False(north.IsInsideWorld);
        Assert.Equal(50, north.HorizontallyNormalized.Y);
    }

    [Fact]
    public void Coordinates_roundtrip_json()
    {
        var original = new WorldCoordinate(-3, 12);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<WorldCoordinate>(json);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void Resolve_is_deterministic()
    {
        var topology = Topology();
        var a = topology.Resolve(-101, 7);
        var b = topology.Resolve(-101, 7);
        Assert.Equal(a, b);
    }
}

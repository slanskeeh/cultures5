using Cultures.World;

namespace Cultures.Tests;

public sealed class RenderProjectionTests
{
    [Fact]
    public void Logical_to_isometric_to_screen_is_deterministic()
    {
        var projection = new RenderProjection(32, 16);
        var cell = new LogicalGridCoordinate(4, 1);
        var iso = projection.ToIsometric(cell);
        var origin = new ScreenCoordinate(100, 200);

        Assert.Equal(new IsometricRenderCoordinate((4 - 1) * 16, (4 + 1) * 8), iso);
        Assert.Equal(new ScreenCoordinate(100 + iso.X, 200 + iso.Y), projection.ToScreen(iso, origin));
        Assert.Equal(projection.ToScreen(cell, origin), projection.ToScreen(iso, origin));
        Assert.Equal(iso, projection.ToIsometric(cell));
    }
}

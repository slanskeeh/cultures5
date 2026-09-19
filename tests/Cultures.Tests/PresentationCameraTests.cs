using Cultures.Application.Presentation;
using Cultures.World;

namespace Cultures.Tests;

public sealed class PresentationCameraTests
{
    [Fact]
    public void Pan_is_continuous_and_does_not_snap_to_hexes()
    {
        var projection = RenderProjection.Playtest;
        var cell = new LogicalGridCoordinate(8, 6);
        var camera = PresentationCamera.LookingAt(cell, projection);
        var origin = projection.ToIso(cell);
        Assert.Equal(origin.X, camera.IsoX);
        Assert.Equal(origin.Y, camera.IsoY);

        camera.Pan(5.5f, -2.25f);
        Assert.Equal(origin.X + 5.5f, camera.IsoX, 3);
        Assert.Equal(origin.Y - 2.25f, camera.IsoY, 3);
        var snapped = projection.ToIso(projection.ApproximateCell(camera.IsoX, camera.IsoY));
        Assert.NotEqual(snapped.X, camera.IsoX);
        Assert.NotEqual(snapped.Y, camera.IsoY);
    }

    [Fact]
    public void Confine_wraps_horizontal_iso_and_clamps_latitude()
    {
        var world = WorldConfiguration.DebugSample;
        var projection = RenderProjection.Playtest;
        var camera = PresentationCamera.LookingAt(new LogicalGridCoordinate(0, 0), projection);
        var width = projection.HexWidth * world.Width;

        camera.Pan(-20f, -50f);
        camera.Confine(world, projection);
        Assert.True(camera.IsoX >= 0f && camera.IsoX < width);
        Assert.Equal(0f, camera.IsoY);

        camera.Pan(0f, 1_000_000f);
        camera.Confine(world, projection);
        var maxY = projection.HexDepth * projection.VerticalScale * (world.Height - 1);
        Assert.Equal(maxY, camera.IsoY, 3);
    }
}

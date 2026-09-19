using Cultures.World;

namespace Cultures.Tests;

public sealed class RenderProjectionTests
{
    [Fact]
    public void Hex_isometric_projection_is_deterministic_and_staggered()
    {
        var projection = new RenderProjection(22, 60);
        var origin = new ScreenCoordinate(100, 200);
        var a = projection.ToIsometric(new LogicalGridCoordinate(0, 0));
        var east = projection.ToIsometric(new LogicalGridCoordinate(1, 0));
        var south = projection.ToIsometric(new LogicalGridCoordinate(0, 1));

        Assert.True(east.X > a.X);
        Assert.Equal(a.Y, east.Y);
        Assert.True(south.X > a.X);
        Assert.True(south.Y > a.Y);
        Assert.Equal(60f, projection.CameraElevationDegrees);
        Assert.Equal(
            projection.ToScreen(new LogicalGridCoordinate(4, 1), origin),
            projection.ToScreen(projection.ToIsometric(new LogicalGridCoordinate(4, 1)), origin));
        Assert.Equal(projection.ToIsometric(new LogicalGridCoordinate(4, 1)), projection.ToIsometric(new LogicalGridCoordinate(4, 1)));
    }

    [Fact]
    public void Free_iso_roundtrips_without_snapping_to_a_hex()
    {
        var projection = RenderProjection.Playtest;
        var cell = new LogicalGridCoordinate(6, 9);
        var iso = projection.ToIso(cell);
        Assert.Equal(cell, projection.ApproximateCell(iso.X, iso.Y));

        var odd = new LogicalGridCoordinate(3, 1);
        Assert.Equal(odd, projection.ApproximateCell(projection.ToIso(odd).X, projection.ToIso(odd).Y));

        var shifted = new IsoPoint(iso.X + 4.25f, iso.Y + 1.75f);
        Assert.Equal(iso.X + 4.25f, shifted.X);
        Assert.NotEqual(iso, shifted);
    }

    [Fact]
    public void Relative_projection_uses_wrapped_horizontal_delta()
    {
        var projection = RenderProjection.Playtest;
        var focus = new LogicalGridCoordinate(0, 8);
        var west = new LogicalGridCoordinate(99, 8);
        var rel = projection.RelativeTo(focus, west, -1);
        Assert.True(rel.X < 0);
        Assert.Equal(0, rel.Y);
    }
}

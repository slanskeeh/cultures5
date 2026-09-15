using Cultures.World;

namespace Cultures.Tests;

public sealed class ChunkConversionTests
{
    private static ChunkLayout Layout() => new(new WorldTopology(WorldConfiguration.DebugSample));

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(9, 9, 0, 0, 9, 9)]
    [InlineData(10, 0, 1, 0, 0, 0)]
    [InlineData(99, 49, 9, 4, 9, 9)]
    public void Cell_maps_to_chunk_and_local(
        int x,
        int y,
        int chunkX,
        int chunkY,
        int localX,
        int localY)
    {
        var layout = Layout();
        var address = layout.ToAddress(new LogicalGridCoordinate(x, y));
        Assert.Equal(new ChunkCoordinate(chunkX, chunkY), address.Chunk);
        Assert.Equal(new ChunkLocalCoordinate(localX, localY), address.Local);
    }

    [Fact]
    public void Seam_x99_plus_one_becomes_chunk_zero()
    {
        var layout = Layout();
        Assert.True(layout.TryResolve(new WorldCoordinate(99, 12), out var atSeam));
        Assert.Equal(new ChunkCoordinate(9, 1), atSeam.Chunk);
        Assert.Equal(new ChunkLocalCoordinate(9, 2), atSeam.Local);

        Assert.True(layout.TryResolve(new WorldCoordinate(99 + 1, 12), out var wrapped));
        Assert.Equal(new LogicalGridCoordinate(0, 12), layout.TryToCell(wrapped.Chunk, wrapped.Local, out var cell) ? cell : default);
        Assert.Equal(new ChunkCoordinate(0, 1), wrapped.Chunk);
        Assert.Equal(new ChunkLocalCoordinate(0, 2), wrapped.Local);
    }

    [Fact]
    public void Negative_x_wraps_before_chunk_resolution()
    {
        var layout = Layout();
        Assert.True(layout.TryResolve(new WorldCoordinate(-1, 0), out var address));
        Assert.Equal(new ChunkCoordinate(9, 0), address.Chunk);
        Assert.Equal(new ChunkLocalCoordinate(9, 0), address.Local);
    }

    [Fact]
    public void Roundtrip_world_chunk_local_world_for_valid_cells()
    {
        var layout = Layout();
        var samples = new[]
        {
            new LogicalGridCoordinate(0, 0),
            new LogicalGridCoordinate(9, 9),
            new LogicalGridCoordinate(10, 0),
            new LogicalGridCoordinate(99, 49),
            new LogicalGridCoordinate(50, 25)
        };

        foreach (var original in samples)
        {
            var address = layout.ToAddress(original);
            Assert.True(layout.TryToCell(address.Chunk, address.Local, out var restored));
            Assert.Equal(original, restored);
        }
    }

    [Fact]
    public void Invalid_y_does_not_resolve_to_a_chunk()
    {
        var layout = Layout();
        Assert.False(layout.TryResolve(new WorldCoordinate(10, -1), out _));
        Assert.False(layout.TryResolve(new WorldCoordinate(10, 50), out _));
    }

    [Fact]
    public void Out_of_range_chunk_or_local_does_not_convert()
    {
        var layout = Layout();
        Assert.False(layout.TryToCell(new ChunkCoordinate(10, 0), new ChunkLocalCoordinate(0, 0), out _));
        Assert.False(layout.TryToCell(new ChunkCoordinate(0, 0), new ChunkLocalCoordinate(10, 0), out _));
        Assert.False(layout.TryToCell(new ChunkCoordinate(0, -1), new ChunkLocalCoordinate(0, 0), out _));
    }
}

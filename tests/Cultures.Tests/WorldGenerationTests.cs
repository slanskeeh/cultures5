using Cultures.World;

namespace Cultures.Tests;

public sealed class WorldGenerationTests
{
    private static LogicalWorld World(ulong seed, int generationVersion = WorldGeneration.CurrentVersion) =>
        new(WorldConfiguration.DebugSample.WithGenerationVersion(generationVersion), seed);

    [Fact]
    public void Same_seed_and_contract_produce_identical_terrain()
    {
        var a = World(42);
        var b = World(42);
        foreach (var cell in SampleCells())
            Assert.Equal(a.Grid.GetCell(cell).Generated, b.Grid.GetCell(cell).Generated);
    }

    [Fact]
    public void Different_seeds_produce_different_terrain()
    {
        var a = World(1);
        var b = World(2);
        Assert.Contains(SampleCells(), cell => a.Grid.GetCell(cell).Generated != b.Grid.GetCell(cell).Generated);
    }

    [Fact]
    public void Generation_version_changes_the_contract()
    {
        var v1 = World(99, 1);
        var v2 = World(99, 2);
        Assert.Contains(SampleCells(), cell => v1.Grid.GetCell(cell).Generated != v2.Grid.GetCell(cell).Generated);
    }

    [Fact]
    public void Coordinate_sample_is_stable()
    {
        var world = World(11);
        var cell = new LogicalGridCoordinate(17, 9);
        Assert.Equal(world.Generator.Sample(cell), world.Generator.Sample(cell));
        Assert.Equal(world.Generator.Sample(cell), world.Grid.GetCell(cell).Generated);
    }

    [Fact]
    public void Horizontal_wrap_returns_the_same_terrain()
    {
        var world = World(5);
        var cfg = world.Configuration;
        for (var y = 0; y < cfg.Height; y += 7)
        {
            Assert.True(world.Grid.TryGetCell(new WorldCoordinate(-1, y), out var wrappedNegative));
            Assert.True(world.Grid.TryGetCell(new WorldCoordinate(cfg.Width - 1, y), out var west));
            Assert.Equal(west.Generated, wrappedNegative.Generated);

            Assert.True(world.Grid.TryGetCell(new WorldCoordinate(cfg.Width, y), out var wrappedWidth));
            Assert.True(world.Grid.TryGetCell(new WorldCoordinate(0, y), out var east));
            Assert.Equal(east.Generated, wrappedWidth.Generated);
        }
    }

    [Fact]
    public void Vertical_bounds_remain_invalid()
    {
        var world = World(5);
        Assert.False(world.Grid.TryGetCell(new WorldCoordinate(3, -1), out _));
        Assert.False(world.Grid.TryGetCell(new WorldCoordinate(3, world.Configuration.Height), out _));
    }

    [Fact]
    public void Chunk_generation_is_deterministic_and_matches_cell_samples()
    {
        var a = World(8);
        var b = World(8);
        var chunk = new ChunkCoordinate(3, 1);
        var first = a.Generator.GetChunk(chunk);
        var second = a.Generator.GetChunk(chunk);
        var other = b.Generator.GetChunk(chunk);
        Assert.Equal(first, second);
        Assert.Equal(first, other);

        Assert.True(a.Chunks.TryToCell(chunk, new ChunkLocalCoordinate(4, 2), out var cell));
        Assert.Equal(a.Generator.Sample(cell), first[2 * a.Configuration.ChunkWidth + 4]);
    }

    [Fact]
    public void Adjacent_chunks_agree_with_world_samples_on_their_boundary_cells()
    {
        var world = World(8);
        var left = new ChunkCoordinate(0, 2);
        var right = new ChunkCoordinate(1, 2);
        var leftData = world.Generator.GetChunk(left);
        var rightData = world.Generator.GetChunk(right);
        var width = world.Configuration.ChunkWidth;

        for (var ly = 0; ly < world.Configuration.ChunkHeight; ly++)
        {
            Assert.True(world.Chunks.TryToCell(left, new ChunkLocalCoordinate(width - 1, ly), out var leftCell));
            Assert.True(world.Chunks.TryToCell(right, new ChunkLocalCoordinate(0, ly), out var rightCell));
            Assert.Equal(world.Generator.Sample(leftCell), leftData[ly * width + width - 1]);
            Assert.Equal(world.Generator.Sample(rightCell), rightData[ly * width]);
        }
    }

    [Fact]
    public void Wrapped_chunk_edges_match_world_samples()
    {
        var world = World(8);
        var last = new ChunkCoordinate(world.Configuration.ChunkCountX - 1, 0);
        var first = new ChunkCoordinate(0, 0);
        var lastData = world.Generator.GetChunk(last);
        var firstData = world.Generator.GetChunk(first);
        var width = world.Configuration.ChunkWidth;

        Assert.True(world.Chunks.TryToCell(last, new ChunkLocalCoordinate(width - 1, 0), out var west));
        Assert.True(world.Chunks.TryToCell(first, new ChunkLocalCoordinate(0, 0), out var east));
        Assert.Equal(world.Generator.Sample(west), lastData[width - 1]);
        Assert.Equal(world.Generator.Sample(east), firstData[0]);
        Assert.Equal(world.Grid.GetCell(new LogicalGridCoordinate(world.Configuration.Width - 1, 0)).Generated, lastData[width - 1]);
    }

    [Fact]
    public void Climate_and_biome_are_deterministic()
    {
        var a = World(21);
        var b = World(21);
        var cell = new LogicalGridCoordinate(40, 10);
        Assert.Equal(a.Grid.GetCell(cell).Climate, b.Grid.GetCell(cell).Climate);
        Assert.Equal(a.Grid.GetCell(cell).Biome, b.Grid.GetCell(cell).Biome);
    }

    [Fact]
    public void Water_follows_sea_level_and_polar_rows_are_colder_than_equator()
    {
        var world = World(13);
        var cfg = world.Configuration;
        var equatorY = cfg.Height / 2;
        var compared = 0;
        var waterChecked = 0;

        for (var x = 0; x < cfg.Width; x += 5)
        {
            for (var y = 0; y < cfg.Height; y += 5)
            {
                var cell = world.Grid.GetCell(new LogicalGridCoordinate(x, y));
                Assert.Equal(cell.Elevation < cfg.SeaLevel, cell.IsWater);
                Assert.Equal(cell.IsWater ? TerrainKind.Water : TerrainKind.Land, cell.Kind);
                waterChecked++;
            }

            var pole = world.Grid.GetCell(new LogicalGridCoordinate(x, 0));
            var south = world.Grid.GetCell(new LogicalGridCoordinate(x, cfg.Height - 1));
            var mid = world.Grid.GetCell(new LogicalGridCoordinate(x, equatorY));
            Assert.True(pole.Climate.Temperature < mid.Climate.Temperature);
            Assert.True(south.Climate.Temperature < mid.Climate.Temperature);
            compared++;
        }

        Assert.True(waterChecked > 0);
        Assert.True(compared > 0);
    }

    [Fact]
    public void Debug_sample_contains_both_land_and_water()
    {
        var world = World(1);
        var land = 0;
        var water = 0;
        for (var x = 0; x < world.Configuration.Width; x += 2)
        {
            for (var y = 0; y < world.Configuration.Height; y += 2)
            {
                if (world.Grid.GetCell(new LogicalGridCoordinate(x, y)).IsWater)
                    water++;
                else
                    land++;
            }
        }

        Assert.True(land > 0);
        Assert.True(water > 0);
    }

    [Fact]
    public void Constructor_does_not_pregenerate_the_whole_world()
    {
        var world = World(3);
        Assert.Equal(0, world.Generator.CachedChunkCount);
        _ = world.Grid.GetCell(new LogicalGridCoordinate(0, 0));
        Assert.Equal(1, world.Generator.CachedChunkCount);
    }

    [Fact]
    public void Horizontal_seam_is_more_continuous_than_a_far_cut()
    {
        var world = World(4);
        var cfg = world.Configuration;
        var seam = 0.0;
        var far = 0.0;
        for (var y = 0; y < cfg.Height; y++)
        {
            var west = world.Generator.Sample(new LogicalGridCoordinate(cfg.Width - 1, y)).Elevation;
            var east = world.Generator.Sample(new LogicalGridCoordinate(0, y)).Elevation;
            var opposite = world.Generator.Sample(new LogicalGridCoordinate(cfg.Width / 2, y)).Elevation;
            seam += Math.Abs(west - east);
            far += Math.Abs(west - opposite);
        }

        Assert.True(seam < far);
    }

    [Fact]
    public void Larger_world_stretches_biome_regions()
    {
        var small = new LogicalWorld(WorldConfiguration.DebugSample, 3);
        var large = new LogicalWorld(WorldConfiguration.Playtest, 3);
        Assert.True(
            LongestBiomeRun(large, large.Configuration.Height / 2)
            > LongestBiomeRun(small, small.Configuration.Height / 2));
    }

    private static int LongestBiomeRun(LogicalWorld world, int y)
    {
        var longest = 1;
        var run = 1;
        var previous = world.Grid.GetCell(new LogicalGridCoordinate(0, y)).Biome;
        for (var x = 1; x < world.Configuration.Width; x++)
        {
            var biome = world.Grid.GetCell(new LogicalGridCoordinate(x, y)).Biome;
            if (biome == previous)
            {
                run++;
                if (run > longest)
                    longest = run;
                continue;
            }

            previous = biome;
            run = 1;
        }

        return longest;
    }

    private static IEnumerable<LogicalGridCoordinate> SampleCells()
    {
        yield return new LogicalGridCoordinate(0, 0);
        yield return new LogicalGridCoordinate(9, 9);
        yield return new LogicalGridCoordinate(10, 0);
        yield return new LogicalGridCoordinate(99, 49);
        yield return new LogicalGridCoordinate(50, 25);
        yield return new LogicalGridCoordinate(1, 24);
    }
}

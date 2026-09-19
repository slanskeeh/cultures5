using Cultures.World;

namespace Cultures.Tests;

public sealed class WorldConfigurationTests
{
    [Fact]
    public void Rejects_non_divisible_chunk_size()
    {
        Assert.Throws<ArgumentException>(() => WorldConfiguration.Create(100, 50, 8, 10));
        Assert.Throws<ArgumentException>(() => WorldConfiguration.Create(100, 50, 10, 7));
    }

    [Fact]
    public void Debug_sample_matches_phase1_test_world()
    {
        var cfg = WorldConfiguration.DebugSample;
        Assert.Equal(100, cfg.Width);
        Assert.Equal(50, cfg.Height);
        Assert.Equal(10, cfg.ChunkWidth);
        Assert.Equal(10, cfg.ChunkHeight);
        Assert.Equal(10, cfg.ChunkCountX);
        Assert.Equal(5, cfg.ChunkCountY);
        Assert.Equal(WorldGeneration.CurrentVersion, cfg.GenerationVersion);
    }

    [Fact]
    public void Playtest_world_is_much_larger_than_the_debug_sample()
    {
        var play = WorldConfiguration.Playtest;
        var debug = WorldConfiguration.DebugSample;
        Assert.Equal(512, play.Width);
        Assert.Equal(256, play.Height);
        Assert.Equal(16, play.ChunkWidth);
        Assert.Equal(32, play.ChunkCountX);
        Assert.Equal(16, play.ChunkCountY);
        Assert.True(play.Width * play.Height >= debug.Width * debug.Height * 20);
    }
}

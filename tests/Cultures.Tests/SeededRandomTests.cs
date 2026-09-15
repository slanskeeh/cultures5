using Cultures.Core.Randomness;

namespace Cultures.Tests;

public sealed class SeededRandomTests
{
    [Fact]
    public void Same_seed_produces_the_same_sequence()
    {
        var a = new SeededRandom(42);
        var b = new SeededRandom(42);

        var sequenceA = Enumerable.Range(0, 32).Select(_ => a.NextUInt32()).ToArray();
        var sequenceB = Enumerable.Range(0, 32).Select(_ => b.NextUInt32()).ToArray();

        Assert.Equal(sequenceA, sequenceB);
    }

    [Fact]
    public void Different_seeds_diverge()
    {
        var a = new SeededRandom(1);
        var b = new SeededRandom(2);

        var sequenceA = Enumerable.Range(0, 16).Select(_ => a.NextInt(0, 1000)).ToArray();
        var sequenceB = Enumerable.Range(0, 16).Select(_ => b.NextInt(0, 1000)).ToArray();

        Assert.NotEqual(sequenceA, sequenceB);
    }

    [Fact]
    public void NextInt_stays_within_range()
    {
        var random = new SeededRandom(99);
        for (var i = 0; i < 256; i++)
        {
            var value = random.NextInt(3, 8);
            Assert.InRange(value, 3, 7);
        }
    }

    [Fact]
    public void NextDouble_is_in_unit_interval()
    {
        var random = new SeededRandom(7);
        for (var i = 0; i < 64; i++)
        {
            var value = random.NextDouble();
            Assert.True(value is >= 0.0 and < 1.0);
        }
    }

    [Fact]
    public void Domain_rng_does_not_use_shared_global_state()
    {
        var first = new SeededRandom(123).NextUInt32();
        System.Random.Shared.Next();
        var second = new SeededRandom(123).NextUInt32();
        Assert.Equal(first, second);
    }
}

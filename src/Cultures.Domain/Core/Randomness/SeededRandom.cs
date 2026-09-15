namespace Cultures.Core.Randomness;

/// <summary>
/// Seeded random source. Domain code must not use uncontrolled global RNG.
/// </summary>
public interface IDeterministicRandom
{
    ulong Seed { get; }
    uint NextUInt32();
    int NextInt(int minInclusive, int maxExclusive);
    double NextDouble();
}

/// <summary>
/// PCG-XSH-RR 32-bit generator. Same seed produces the same sequence
/// independent of .NET Random implementation changes.
/// </summary>
public sealed class SeededRandom : IDeterministicRandom
{
    private const ulong Multiplier = 6364136223846793005UL;
    private ulong _state;
    private readonly ulong _increment;

    public SeededRandom(ulong seed)
    {
        Seed = seed;
        _increment = ((seed ^ 0xDA3E39CB94B95BDBUL) << 1) | 1UL;
        _state = 0;
        NextUInt32();
        _state += seed;
        NextUInt32();
    }

    public ulong Seed { get; }

    public uint NextUInt32()
    {
        var oldState = _state;
        _state = unchecked(oldState * Multiplier + _increment);
        var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rot = (int)(oldState >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Range must contain at least one value.");

        var range = (uint)(maxExclusive - minInclusive);
        var threshold = (uint)(0u - range) % range;
        uint value;
        do
        {
            value = NextUInt32();
        } while (value < threshold);

        return minInclusive + (int)(value % range);
    }

    public double NextDouble() => NextUInt32() * (1.0 / 4294967296.0);
}

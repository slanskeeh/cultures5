namespace Cultures.World;

/// <summary>
/// Hash-based value noise. Periodic in X when sampled on a cylinder (cos/sin of longitude).
/// </summary>
internal static class LatticeNoise
{
    public static float Value3(float x, float y, float z, ulong seed)
    {
        var x0 = Floor(x);
        var y0 = Floor(y);
        var z0 = Floor(z);
        var tx = Fade(x - x0);
        var ty = Fade(y - y0);
        var tz = Fade(z - z0);

        var n000 = Hash01(x0, y0, z0, seed);
        var n100 = Hash01(x0 + 1, y0, z0, seed);
        var n010 = Hash01(x0, y0 + 1, z0, seed);
        var n110 = Hash01(x0 + 1, y0 + 1, z0, seed);
        var n001 = Hash01(x0, y0, z0 + 1, seed);
        var n101 = Hash01(x0 + 1, y0, z0 + 1, seed);
        var n011 = Hash01(x0, y0 + 1, z0 + 1, seed);
        var n111 = Hash01(x0 + 1, y0 + 1, z0 + 1, seed);

        var nx00 = Lerp(n000, n100, tx);
        var nx10 = Lerp(n010, n110, tx);
        var nx01 = Lerp(n001, n101, tx);
        var nx11 = Lerp(n011, n111, tx);
        var nxy0 = Lerp(nx00, nx10, ty);
        var nxy1 = Lerp(nx01, nx11, ty);
        return Lerp(nxy0, nxy1, tz);
    }

    public static float Hash01(int x, int y, int z, ulong seed)
    {
        unchecked
        {
            var h = seed
                    ^ ((ulong)(uint)x * 0x9E3779B97F4A7C15UL)
                    ^ ((ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL)
                    ^ ((ulong)(uint)z * 0x165667B19E3779F9UL);
            h ^= h >> 33;
            h *= 0xFF51AFD7ED558CCDUL;
            h ^= h >> 33;
            h *= 0xC4CEB9FE1A85EC53UL;
            h ^= h >> 33;
            return (uint)h * (1f / 4294967296f);
        }
    }

    public static ulong Mix(ulong seed, ulong salt)
    {
        unchecked
        {
            var h = seed ^ (salt * 0x9E3779B97F4A7C15UL);
            h ^= h >> 32;
            h *= 0xBF58476D1CE4E5B9UL;
            h ^= h >> 32;
            return h;
        }
    }

    private static int Floor(float value) => (int)MathF.Floor(value);

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}

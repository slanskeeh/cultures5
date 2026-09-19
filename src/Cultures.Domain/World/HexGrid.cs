namespace Cultures.World;

/// <summary>
/// Pointy-top odd-r offset hexes. Storage stays (X column, Y row).
/// Adjacency and distance are hex, not square.
/// </summary>
public static class HexGrid
{
    public const int NeighborCount = 6;

    private static readonly (int Dx, int Dy)[] EvenRow =
    [
        (1, 0), (0, -1), (-1, -1),
        (-1, 0), (-1, 1), (0, 1)
    ];

    private static readonly (int Dx, int Dy)[] OddRow =
    [
        (1, 0), (1, -1), (0, -1),
        (-1, 0), (0, 1), (1, 1)
    ];

    public static IReadOnlyList<(int Dx, int Dy)> NeighborOffsets(int row) =>
        (row & 1) == 0 ? EvenRow : OddRow;

    public readonly record struct Cube(int X, int Y, int Z);

    /// <summary>Axial (dq, dr) steps from a cube. Six hex directions.</summary>
    public static readonly (int Dq, int Dr)[] CubeNeighborDeltas =
    [
        (1, 0), (1, -1), (0, -1),
        (-1, 0), (-1, 1), (0, 1)
    ];

    public static Cube ToCube(LogicalGridCoordinate cell)
    {
        var q = cell.X - (cell.Y - (cell.Y & 1)) / 2;
        var r = cell.Y;
        return new Cube(q, -q - r, r);
    }

    public static LogicalGridCoordinate FromCube(Cube cube)
    {
        var col = cube.X + (cube.Z - (cube.Z & 1)) / 2;
        return new LogicalGridCoordinate(col, cube.Z);
    }

    public static Cube Add(Cube origin, int dq, int dr)
    {
        var q = origin.X + dq;
        var r = origin.Z + dr;
        return new Cube(q, -q - r, r);
    }

    public static int CubeDistance(Cube a, Cube b) =>
        (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z)) / 2;

    /// <summary>
    /// Cubes within <paramref name="radius"/> of the origin, including the origin.
    /// Count is 1 + 3 * r * (r + 1).
    /// </summary>
    public static IReadOnlyList<Cube> Disk(int radius)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        var cubes = new List<Cube>(1 + 3 * radius * (radius + 1));
        for (var r = -radius; r <= radius; r++)
        {
            var qMin = Math.Max(-radius, -r - radius);
            var qMax = Math.Min(radius, -r + radius);
            for (var q = qMin; q <= qMax; q++)
                cubes.Add(new Cube(q, -q - r, r));
        }

        return cubes;
    }

    /// <summary>
    /// Wrap-aware hex distance. X wraps; Y does not.
    /// </summary>
    public static int Distance(LogicalGridCoordinate a, LogicalGridCoordinate b, int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        var origin = ToCube(a);
        var best = int.MaxValue;
        for (var wrap = -1; wrap <= 1; wrap++)
        {
            var other = ToCube(new LogicalGridCoordinate(b.X + wrap * width, b.Y));
            var distance = CubeDistance(origin, other);
            if (distance < best)
                best = distance;
        }

        return best;
    }
}

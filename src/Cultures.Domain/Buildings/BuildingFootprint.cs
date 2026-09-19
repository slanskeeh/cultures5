using Cultures.World;

namespace Cultures.Buildings;

/// <summary>
/// Hex-connected cells relative to a building origin, stored as cube deltas.
/// Shape does not depend on odd-r row parity of the origin (AD-126).
/// </summary>
public sealed class BuildingFootprint
{
    private BuildingFootprint(IReadOnlyList<HexGrid.Cube> relativeCubes)
    {
        RelativeCubes = relativeCubes;
    }

    public IReadOnlyList<HexGrid.Cube> RelativeCubes { get; }

    public int CellCount => RelativeCubes.Count;

    public static BuildingFootprint Cell1x1 { get; } = FromCubeOffsets((0, 0));

    /// <summary>Offset-space rectangle authored at even-row (0,0), then stored as cubes.</summary>
    public static BuildingFootprint Rect(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Footprint size must be positive.");

        var offsets = new List<(int Dq, int Dr)>(width * height);
        for (var dy = 0; dy < height; dy++)
        {
            for (var dx = 0; dx < width; dx++)
            {
                var cube = HexGrid.ToCube(new LogicalGridCoordinate(dx, dy));
                offsets.Add((cube.X, cube.Z));
            }
        }

        return FromCubeOffsets(offsets);
    }

    /// <summary>Origin plus every hex within cube distance <paramref name="radius"/>.</summary>
    public static BuildingFootprint Disk(int radius) =>
        FromCubes(HexGrid.Disk(radius));

    /// <summary>
    /// Arbitrary hex polyomino. Axial (q, r) deltas from the origin cube.
    /// Must include (0, 0) and be hex-connected.
    /// </summary>
    public static BuildingFootprint FromCubeOffsets(params (int Dq, int Dr)[] offsets) =>
        FromCubeOffsets((IReadOnlyList<(int Dq, int Dr)>)offsets);

    public static BuildingFootprint FromCubeOffsets(IReadOnlyList<(int Dq, int Dr)> offsets)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        var cubes = new HexGrid.Cube[offsets.Count];
        for (var i = 0; i < offsets.Count; i++)
        {
            var (dq, dr) = offsets[i];
            cubes[i] = new HexGrid.Cube(dq, -dq - dr, dr);
        }

        return FromCubes(cubes);
    }

    public bool TryMaterialize(
        WorldTopology topology,
        LogicalGridCoordinate origin,
        out List<LogicalGridCoordinate> cells)
    {
        ArgumentNullException.ThrowIfNull(topology);
        cells = new List<LogicalGridCoordinate>(RelativeCubes.Count);
        var originCube = HexGrid.ToCube(origin);
        var seen = new HashSet<LogicalGridCoordinate>();
        foreach (var relative in RelativeCubes)
        {
            var cube = HexGrid.Add(originCube, relative.X, relative.Z);
            var unwrapped = HexGrid.FromCube(cube);
            var resolution = topology.Resolve(unwrapped.X, unwrapped.Y);
            if (!resolution.TryGetCell(out var cell) || !seen.Add(cell))
            {
                cells.Clear();
                return false;
            }

            cells.Add(cell);
        }

        return true;
    }

    private static BuildingFootprint FromCubes(IReadOnlyList<HexGrid.Cube> cubes)
    {
        ArgumentNullException.ThrowIfNull(cubes);
        if (cubes.Count == 0)
            throw new ArgumentException("Footprint must contain at least one cell.", nameof(cubes));

        var unique = new HashSet<HexGrid.Cube>(cubes);
        if (unique.Count != cubes.Count)
            throw new ArgumentException("Footprint cells must be unique.", nameof(cubes));
        if (!unique.Contains(new HexGrid.Cube(0, 0, 0)))
            throw new ArgumentException("Footprint must include the origin cube (0,0,0).", nameof(cubes));
        if (!IsCubeConnected(unique))
            throw new ArgumentException("Footprint must be a single hex-connected shape.", nameof(cubes));

        return new BuildingFootprint(
            unique
                .OrderBy(c => HexGrid.CubeDistance(new HexGrid.Cube(0, 0, 0), c))
                .ThenBy(c => c.Z)
                .ThenBy(c => c.X)
                .ToArray());
    }

    private static bool IsCubeConnected(HashSet<HexGrid.Cube> cubes)
    {
        var seen = new HashSet<HexGrid.Cube>();
        var queue = new Queue<HexGrid.Cube>();
        var start = new HexGrid.Cube(0, 0, 0);
        queue.Enqueue(start);
        seen.Add(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var (dq, dr) in HexGrid.CubeNeighborDeltas)
            {
                var next = HexGrid.Add(current, dq, dr);
                if (cubes.Contains(next) && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        return seen.Count == cubes.Count;
    }
}

namespace Cultures.World;

/// <summary>
/// Single source of wrapping and bounds. All other world systems must use this type
/// instead of duplicating modulo formulas.
/// </summary>
public sealed class WorldTopology
{
    public WorldTopology(WorldConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public WorldConfiguration Configuration { get; }

    /// <summary>
    /// Mathematical Euclidean modulo. C# remainder of negative values is not used as-is.
    /// </summary>
    public static int EuclideanMod(int value, int modulus)
    {
        if (modulus <= 0)
            throw new ArgumentOutOfRangeException(nameof(modulus), "Modulus must be positive.");

        var remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }

    public int WrapX(int x) => EuclideanMod(x, Configuration.Width);

    public bool IsInsideY(int y) => y >= 0 && y < Configuration.Height;

    public CoordinateResolution Resolve(WorldCoordinate coordinate)
    {
        var normalized = new WorldCoordinate(WrapX(coordinate.X), coordinate.Y);
        return new CoordinateResolution(coordinate, normalized, IsInsideY(coordinate.Y));
    }

    public CoordinateResolution Resolve(int x, int y) => Resolve(new WorldCoordinate(x, y));

    public bool IsInside(WorldCoordinate coordinate) => Resolve(coordinate).IsInsideWorld;

    public bool TryGetCell(WorldCoordinate coordinate, out LogicalGridCoordinate cell) =>
        Resolve(coordinate).TryGetCell(out cell);

    /// <summary>
    /// Temporary latitude mapping: 0 at y = 0, 1 at y = Height - 1.
    /// Poles are both ends; which end is geographic north is not a final design decision.
    /// </summary>
    public float Latitude01(int y)
    {
        if (!IsInsideY(y))
            throw new ArgumentOutOfRangeException(nameof(y), "Latitude is only defined inside the world.");

        if (Configuration.Height <= 1)
            return 0.5f;

        return y / (float)(Configuration.Height - 1);
    }

    /// <summary>
    /// Shortest horizontal distance on the wrapped X axis, in cells.
    /// </summary>
    public int HorizontalDistance(int x1, int x2)
    {
        var a = WrapX(x1);
        var b = WrapX(x2);
        var delta = Math.Abs(a - b);
        return Math.Min(delta, Configuration.Width - delta);
    }

    public int HorizontalDistance(WorldCoordinate a, WorldCoordinate b) => HorizontalDistance(a.X, b.X);

    public int HorizontalDistance(LogicalGridCoordinate a, LogicalGridCoordinate b) => HorizontalDistance(a.X, b.X);

    /// <summary>
    /// Signed shortest horizontal delta after wrap. Positive is east.
    /// </summary>
    public int SignedHorizontalDelta(int fromX, int toX)
    {
        var width = Configuration.Width;
        var delta = WrapX(toX) - WrapX(fromX);
        if (delta > width / 2)
            delta -= width;
        if (delta < -width / 2)
            delta += width;
        return delta;
    }
}

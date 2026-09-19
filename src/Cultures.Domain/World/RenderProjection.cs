namespace Cultures.World;

/// <summary>
/// Deterministic logical → isometric hex → screen mapping.
/// Camera elevation is from the ground plane; 60° is the playtest default.
/// Never used as simulation position.
/// </summary>
public sealed class RenderProjection
{
    public const float DefaultCameraElevationDegrees = 60f;

    public RenderProjection(int hexSize, float cameraElevationDegrees = DefaultCameraElevationDegrees)
    {
        if (hexSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(hexSize));

        HexSize = hexSize;
        CameraElevationDegrees = cameraElevationDegrees;
        VerticalScale = MathF.Sin(cameraElevationDegrees * MathF.PI / 180f);
        HexWidth = hexSize * MathF.Sqrt(3f);
        HexDepth = hexSize * 1.5f;
        TileWidth = Math.Max(1, (int)MathF.Round(HexWidth));
        TileHeight = Math.Max(1, (int)MathF.Round(HexDepth * VerticalScale));
    }

    public static RenderProjection Playtest { get; } = new(22);

    public int HexSize { get; }
    public float CameraElevationDegrees { get; }
    public float VerticalScale { get; }
    public float HexWidth { get; }
    public float HexDepth { get; }
    public int TileWidth { get; }
    public int TileHeight { get; }

    public IsometricRenderCoordinate ToIsometric(LogicalGridCoordinate cell)
    {
        var stagger = (cell.Y & 1) * 0.5f;
        var x = (int)MathF.Round(HexWidth * (cell.X + stagger));
        var y = (int)MathF.Round(HexDepth * VerticalScale * cell.Y);
        return new IsometricRenderCoordinate(x, y);
    }

    public ScreenCoordinate ToScreen(IsometricRenderCoordinate isometric, ScreenCoordinate origin) =>
        new(origin.X + isometric.X, origin.Y + isometric.Y);

    public ScreenCoordinate ToScreen(LogicalGridCoordinate cell, ScreenCoordinate origin) =>
        ToScreen(ToIsometric(cell), origin);

    public IsoPoint ToIso(LogicalGridCoordinate cell) => ToIsoUnwrapped(cell.X, cell.Y);

    public IsoPoint ToIsoUnwrapped(int x, int y)
    {
        var stagger = (y & 1) * 0.5f;
        return new IsoPoint(HexWidth * (x + stagger), HexDepth * VerticalScale * y);
    }

    public LogicalGridCoordinate ApproximateCell(float isoX, float isoY)
    {
        var row = (int)MathF.Round(isoY / (HexDepth * VerticalScale));
        var stagger = (row & 1) * 0.5f;
        var col = (int)MathF.Round(isoX / HexWidth - stagger);
        return new LogicalGridCoordinate(col, row);
    }

    public IsometricRenderCoordinate RelativeTo(LogicalGridCoordinate focus, LogicalGridCoordinate cell, int signedDx)
    {
        var unwrapped = new LogicalGridCoordinate(focus.X + signedDx, cell.Y);
        var focusIso = ToIsometric(focus);
        var cellIso = ToIsometric(unwrapped);
        return new IsometricRenderCoordinate(cellIso.X - focusIso.X, cellIso.Y - focusIso.Y);
    }
}

namespace Cultures.World;

/// <summary>
/// Deterministic logical → isometric → screen mapping. Presentation-only math;
/// tile pixel sizes are parameters because OD-002 is still open.
/// </summary>
public sealed class RenderProjection
{
    public RenderProjection(int tileWidth, int tileHeight)
    {
        if (tileWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileWidth));
        if (tileHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileHeight));

        TileWidth = tileWidth;
        TileHeight = tileHeight;
    }

    public int TileWidth { get; }
    public int TileHeight { get; }

    public IsometricRenderCoordinate ToIsometric(LogicalGridCoordinate cell)
    {
        var x = (cell.X - cell.Y) * (TileWidth / 2);
        var y = (cell.X + cell.Y) * (TileHeight / 2);
        return new IsometricRenderCoordinate(x, y);
    }

    public ScreenCoordinate ToScreen(IsometricRenderCoordinate isometric, ScreenCoordinate origin) =>
        new(origin.X + isometric.X, origin.Y + isometric.Y);

    public ScreenCoordinate ToScreen(LogicalGridCoordinate cell, ScreenCoordinate origin) =>
        ToScreen(ToIsometric(cell), origin);
}

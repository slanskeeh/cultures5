using Cultures.Application;
using Cultures.World;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Orthographic debug map. Logical coordinates stay in the domain; this node only reads them.
/// </summary>
public partial class WorldDebugMap : Control
{
    public const int CellSize = 22;
    public const int RadiusX = 16;
    public const int RadiusY = 10;

    public SimulationHost? Host { get; set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        if (Host is null)
            return;

        var cursor = Host.Cursor.Position;
        var topology = Host.World.Topology;
        var width = Host.World.Configuration.Width;
        var center = Size / 2f;

        for (var dy = -RadiusY; dy <= RadiusY; dy++)
        {
            for (var dx = -RadiusX; dx <= RadiusX; dx++)
            {
                var rect = new Rect2(
                    center.X + dx * CellSize - CellSize * 0.5f,
                    center.Y + dy * CellSize - CellSize * 0.5f,
                    CellSize - 1,
                    CellSize - 1);

                var resolution = topology.Resolve(cursor.X + dx, cursor.Y + dy);
                if (!resolution.IsInsideWorld)
                {
                    DrawRect(rect, new Color(0.12f, 0.07f, 0.07f));
                    continue;
                }

                Host.World.Grid.TryGetCell(resolution.HorizontallyNormalized, out var terrain);
                var color = terrain.IsOccupied
                    ? new Color(0.62f, 0.42f, 0.22f)
                    : new Color(0.22f, 0.28f, 0.18f);

                var x = resolution.HorizontallyNormalized.X;
                if (x == 0 || x == width - 1)
                    color = color.Lightened(0.18f);

                DrawRect(rect, color);

                if (dx == 0 && dy == 0)
                    DrawRect(rect, new Color(0.85f, 0.74f, 0.42f), filled: false, width: 2);
            }
        }
    }
}

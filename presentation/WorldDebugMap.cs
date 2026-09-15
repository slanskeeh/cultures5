using Cultures.Application;
using Cultures.Core.Ids;
using Cultures.Population;
using Cultures.World;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Orthographic debug map of generated terrain. Domain remains authoritative.
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
                var color = ColorFor(terrain);
                var x = resolution.HorizontallyNormalized.X;
                if (x == 0 || x == width - 1)
                    color = color.Lightened(0.16f);

                DrawRect(rect, color);

                if (dx == 0 && dy == 0)
                    DrawRect(rect, new Color(0.95f, 0.86f, 0.45f), filled: false, width: 2);
            }
        }

        DrawCharacters(center, cursor);
    }

    public CharacterId? SelectedId { get; set; }

    private void DrawCharacters(Vector2 center, LogicalGridCoordinate cursor)
    {
        if (Host is null)
            return;

        foreach (var character in Host.Population.All)
        {
            var dx = Host.World.Topology.SignedHorizontalDelta(cursor.X, character.Position.X);
            var dy = character.Position.Y - cursor.Y;
            if (Math.Abs(dx) > RadiusX || Math.Abs(dy) > RadiusY)
                continue;

            var pos = new Vector2(
                center.X + dx * CellSize,
                center.Y + dy * CellSize);
            var radius = character.IsAlive ? 6f : 4f;
            DrawCircle(pos, radius, ColorForAction(character));
            if (SelectedId == character.Id)
                DrawArc(pos, radius + 3f, 0, MathF.Tau, 16, new Color(1f, 1f, 1f));
        }
    }

    private static Color ColorForAction(CharacterState character)
    {
        if (!character.IsAlive)
            return new Color(0.25f, 0.25f, 0.25f);

        return character.Activity.Kind switch
        {
            ActionKind.Eat => new Color(0.95f, 0.55f, 0.15f),
            ActionKind.Sleep => new Color(0.45f, 0.65f, 0.95f),
            ActionKind.Work => new Color(0.72f, 0.50f, 0.22f),
            ActionKind.Move => new Color(0.95f, 0.95f, 0.95f),
            ActionKind.Idle => new Color(0.70f, 0.70f, 0.55f),
            _ => new Color(0.85f, 0.35f, 0.45f)
        };
    }

    private static Color ColorFor(TerrainCell terrain)
    {
        var color = terrain.Biome switch
        {
            BiomeId.Ocean => new Color(0.12f, 0.28f, 0.52f),
            BiomeId.Ice => new Color(0.82f, 0.90f, 0.95f),
            BiomeId.Tundra => new Color(0.45f, 0.52f, 0.48f),
            BiomeId.TemperateLand => new Color(0.32f, 0.52f, 0.24f),
            BiomeId.Forest => new Color(0.12f, 0.32f, 0.16f),
            BiomeId.Desert => new Color(0.72f, 0.62f, 0.32f),
            BiomeId.Highland => new Color(0.42f, 0.36f, 0.30f),
            _ => new Color(0.22f, 0.22f, 0.22f)
        };

        if (!terrain.IsWater)
            color = color.Lerp(new Color(0.12f, 0.10f, 0.08f), terrain.Elevation * 0.28f);

        if (terrain.IsOccupied)
            color = color.Lerp(new Color(0.85f, 0.55f, 0.18f), 0.45f);

        return color;
    }
}

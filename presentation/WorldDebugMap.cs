using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Population;
using Cultures.Settlement;
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

        var host = Host;
        var cursor = host.Cursor.Position;
        var topology = host.World.Topology;
        var width = host.World.Configuration.Width;
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

                host.World.Grid.TryGetCell(resolution.HorizontallyNormalized, out var terrain);
                var color = ColorFor(terrain);
                var x = resolution.HorizontallyNormalized.X;
                if (x == 0 || x == width - 1)
                    color = color.Lightened(0.16f);

                if (ShowLod)
                {
                    var lodChunk = host.World.Chunks.ToAddress(
                        new LogicalGridCoordinate(resolution.HorizontallyNormalized.X, resolution.HorizontallyNormalized.Y));
                    color = color.Lerp(ColorForLod(host.Lod.Classify(lodChunk.Chunk)), 0.35f);
                }

                DrawRect(rect, color);

                if (dx == 0 && dy == 0)
                    DrawRect(rect, new Color(0.95f, 0.86f, 0.45f), filled: false, width: 2);
            }
        }

        DrawBuildings(center, cursor);
        DrawSettlements(center, cursor);
        DrawCharacters(center, cursor);
    }

    public CharacterId? SelectedId { get; set; }
    public BuildingId? SelectedBuildingId { get; set; }
    public SettlementId? SelectedSettlementId { get; set; }
    public bool ShowLod { get; set; }

    private void DrawSettlements(Vector2 center, LogicalGridCoordinate cursor)
    {
        if (Host is null)
            return;

        foreach (var settlement in Host.Settlements.All)
        {
            var dx = Host.World.Topology.SignedHorizontalDelta(cursor.X, settlement.Core.X);
            var dy = settlement.Core.Y - cursor.Y;
            if (Math.Abs(dx) > RadiusX || Math.Abs(dy) > RadiusY)
                continue;

            var pos = new Vector2(
                center.X + dx * CellSize,
                center.Y + dy * CellSize);
            var color = ColorForSettlement(settlement.Id, settlement.Lifecycle);
            DrawArc(pos, CellSize * 0.7f, 0, MathF.Tau, 20, color, 2);
            if (SelectedSettlementId == settlement.Id)
                DrawArc(pos, CellSize * 0.95f, 0, MathF.Tau, 24, new Color(1f, 1f, 0.55f), 2);
        }
    }

    private static Color ColorForSettlement(SettlementId id, SettlementLifecycle lifecycle)
    {
        var hue = (id.Value % 8) / 8f;
        var color = Color.FromHsv(hue, 0.55f, 0.95f);
        return lifecycle switch
        {
            SettlementLifecycle.Emerging => color.Lightened(0.15f),
            SettlementLifecycle.Declining => color.Darkened(0.25f),
            SettlementLifecycle.Abandoned => new Color(0.35f, 0.35f, 0.35f),
            _ => color
        };
    }

    private void DrawBuildings(Vector2 center, LogicalGridCoordinate cursor)
    {
        if (Host is null)
            return;

        var font = ThemeDB.FallbackFont;
        foreach (var building in Host.Buildings.All)
        {
            foreach (var cell in building.FootprintCells)
            {
                var dx = Host.World.Topology.SignedHorizontalDelta(cursor.X, cell.X);
                var dy = cell.Y - cursor.Y;
                if (Math.Abs(dx) > RadiusX || Math.Abs(dy) > RadiusY)
                    continue;

                var pos = new Vector2(
                    center.X + dx * CellSize,
                    center.Y + dy * CellSize);
                var letter = LetterFor(building.TypeId);
                DrawRect(
                    new Rect2(pos.X - CellSize * 0.4f, pos.Y - CellSize * 0.4f, CellSize * 0.8f, CellSize * 0.8f),
                    ColorForBuilding(building),
                    filled: true);
                DrawString(
                    font,
                    pos + new Vector2(-5, 5),
                    letter,
                    HorizontalAlignment.Left,
                    -1,
                    12,
                    new Color(0.08f, 0.08f, 0.08f));
                if (SelectedBuildingId == building.Id)
                    DrawRect(
                        new Rect2(pos.X - CellSize * 0.5f, pos.Y - CellSize * 0.5f, CellSize, CellSize),
                        new Color(1f, 1f, 1f),
                        filled: false,
                        width: 2);
            }
        }
    }

    private static string LetterFor(BuildingTypeId type)
    {
        if (type == BuildingTypeId.Farm)
            return "F";
        if (type == BuildingTypeId.Storage)
            return "S";
        if (type == BuildingTypeId.Shelter)
            return "H";
        if (type == BuildingTypeId.Workshop)
            return "W";
        return "?";
    }

    private static Color ColorForBuilding(BuildingState building)
    {
        if (building.TypeId == BuildingTypeId.Farm)
            return new Color(0.55f, 0.72f, 0.28f);
        if (building.TypeId == BuildingTypeId.Storage)
            return new Color(0.72f, 0.58f, 0.28f);
        if (building.TypeId == BuildingTypeId.Shelter)
            return new Color(0.62f, 0.48f, 0.38f);
        return new Color(0.50f, 0.50f, 0.58f);
    }

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
            var radius = !character.IsAlive
                ? 4f
                : character.LifeStage == CharacterLifeStage.Infant ? 4.5f : 6f;
            if (character.LodTier.IsAggregate())
                radius -= 1.5f;
            DrawCircle(pos, radius, ColorForAction(character));
            if (character.Settlement.IsAssigned)
                DrawArc(pos, radius + 5f, 0, MathF.Tau, 12, ColorForSettlement(character.Settlement, SettlementLifecycle.Established));
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

    private static Color ColorForLod(SimulationLodTier tier) => tier switch
    {
        SimulationLodTier.Full => new Color(0.20f, 0.85f, 0.35f),
        SimulationLodTier.Reduced => new Color(0.85f, 0.75f, 0.20f),
        SimulationLodTier.Aggregate => new Color(0.85f, 0.45f, 0.18f),
        _ => new Color(0.45f, 0.20f, 0.55f)
    };
}

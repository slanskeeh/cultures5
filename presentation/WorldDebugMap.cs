using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Exploration;
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
    public bool HighContrast { get; set; }

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

                if (!resolution.TryGetCell(out var cell))
                {
                    DrawRect(rect, new Color(0.12f, 0.07f, 0.07f));
                    continue;
                }

                var paintChunk = host.World.Chunks.ToAddress(cell).Chunk;
                if (ShowExploration)
                {
                    var color = ColorFromKnowledge(host, paintChunk);
                    if (ShowLod && host.Exploration.LevelOf(paintChunk) != ExplorationKnowledgeLevel.Unknown)
                        color = color.Lerp(ColorForLod(host.Lod.Classify(paintChunk)), 0.35f);
                    DrawRect(rect, color);
                }
                else
                {
                    DrawTextureRect(SimpleTextures.Biome(host.World.Grid.GetCell(cell).Biome), rect, false);
                    if (HighContrast)
                        DrawRect(rect, new Color(1f, 1f, 1f, 0.45f), filled: false, width: 1);
                    var terrain = host.World.Grid.GetCell(cell);
                    if (terrain.HasRiver)
                        DrawRect(rect, new Color(0.20f, 0.45f, 0.72f, 0.35f));
                    if (terrain.IsOccupied)
                        DrawRect(rect, new Color(0.85f, 0.55f, 0.18f, 0.25f));
                    if (cell.X == 0 || cell.X == width - 1)
                        DrawRect(rect, new Color(1, 1, 1, 0.08f));
                    if (ShowLod)
                        DrawRect(rect, ColorForLod(host.Lod.Classify(paintChunk)) with { A = 0.28f });
                }

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
    public bool ShowExploration { get; set; }

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
                var sprite = new Rect2(pos.X - CellSize * 0.4f, pos.Y - CellSize * 0.4f, CellSize * 0.8f, CellSize * 0.8f);
                DrawTextureRect(SimpleTextures.Building(building.TypeId), sprite, false);
                if (SelectedBuildingId == building.Id)
                    DrawRect(
                        new Rect2(pos.X - CellSize * 0.5f, pos.Y - CellSize * 0.5f, CellSize, CellSize),
                        new Color(1f, 1f, 1f),
                        filled: false,
                        width: 2);
            }
        }
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
            var sprite = new Rect2(pos.X - CellSize * 0.45f, pos.Y - CellSize * 0.45f, CellSize * 0.9f, CellSize * 0.9f);
            DrawTextureRect(SimpleTextures.Character(character.LifeStage, SelectedId == character.Id), sprite, false);
            if (character.Settlement.IsAssigned)
                DrawArc(pos, CellSize * 0.42f, 0, MathF.Tau, 12, ColorForSettlement(character.Settlement, SettlementLifecycle.Established));
        }
    }

    private static Color ColorFromKnowledge(SimulationHost host, ChunkCoordinate chunk)
    {
        var facts = host.Exploration.GetKnownFacts(chunk);
        return facts.Level switch
        {
            ExplorationKnowledgeLevel.Unknown => ColorForExploration(ExplorationKnowledgeLevel.Unknown),
            ExplorationKnowledgeLevel.Rumored => ColorForExploration(ExplorationKnowledgeLevel.Rumored),
            ExplorationKnowledgeLevel.Scouted => ColorForScouted(facts.Terrain)
                .Lerp(ColorForExploration(ExplorationKnowledgeLevel.Scouted), 0.35f),
            _ => (facts.Biome.Known && facts.Biome.Dominant is { } biome
                    ? ColorForBiome(biome)
                    : ColorForExploration(facts.Level))
                .Lerp(ColorForExploration(facts.Level), 0.40f)
        };
    }

    private static Color ColorForScouted(TerrainKnowledge terrain)
    {
        var land = new Color(0.28f, 0.32f, 0.26f);
        var water = new Color(0.14f, 0.24f, 0.42f);
        if (terrain.HasWater && !terrain.HasLand)
            return water;
        if (terrain.HasLand && !terrain.HasWater)
            return land;
        return land.Lerp(water, 0.50f);
    }

    private static Color ColorForBiome(BiomeId biome) => biome switch
    {
        BiomeId.Ocean => new Color(0.12f, 0.28f, 0.52f),
        BiomeId.Ice => new Color(0.82f, 0.90f, 0.95f),
        BiomeId.Tundra => new Color(0.45f, 0.52f, 0.48f),
        BiomeId.TemperateLand => new Color(0.32f, 0.52f, 0.24f),
        BiomeId.Forest => new Color(0.12f, 0.32f, 0.16f),
        BiomeId.Desert => new Color(0.72f, 0.62f, 0.32f),
        BiomeId.Highland => new Color(0.42f, 0.36f, 0.30f),
        BiomeId.Swamp => new Color(0.18f, 0.34f, 0.26f),
        BiomeId.Savanna => new Color(0.58f, 0.54f, 0.24f),
        BiomeId.Taiga => new Color(0.16f, 0.30f, 0.24f),
        _ => new Color(0.22f, 0.22f, 0.22f)
    };

    private static Color ColorForLod(SimulationLodTier tier) => tier switch
    {
        SimulationLodTier.Full => new Color(0.20f, 0.85f, 0.35f),
        SimulationLodTier.Reduced => new Color(0.85f, 0.75f, 0.20f),
        SimulationLodTier.Aggregate => new Color(0.85f, 0.45f, 0.18f),
        _ => new Color(0.45f, 0.20f, 0.55f)
    };

    private static Color ColorForExploration(ExplorationKnowledgeLevel level) => level switch
    {
        ExplorationKnowledgeLevel.Rumored => new Color(0.45f, 0.28f, 0.55f),
        ExplorationKnowledgeLevel.Scouted => new Color(0.35f, 0.45f, 0.55f),
        ExplorationKnowledgeLevel.Mapped => new Color(0.28f, 0.55f, 0.42f),
        ExplorationKnowledgeLevel.Confirmed => new Color(0.55f, 0.62f, 0.28f),
        ExplorationKnowledgeLevel.Analyzed => new Color(0.75f, 0.72f, 0.35f),
        _ => new Color(0.05f, 0.05f, 0.07f)
    };
}

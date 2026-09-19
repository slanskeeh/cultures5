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
/// Isometric 2.5D hex debug map. Domain remains authoritative.
/// </summary>
public partial class WorldDebugMap : Control
{
    public const int CellSize = 22;
    public const int DefaultRadiusX = 16;
    public const int DefaultRadiusY = 10;

    private readonly RenderProjection _projection = RenderProjection.Playtest;

    public SimulationHost? Host { get; set; }
    public bool HighContrast { get; set; }
    public bool Interactive { get; set; } = true;
    public LogicalGridCoordinate? FocusOverride { get; set; }
    public int ViewRadiusX { get; set; } = DefaultRadiusX;
    public int ViewRadiusY { get; set; } = DefaultRadiusY;
    public event Action<Vector2>? Picked;

    public override void _Ready()
    {
        MouseFilter = Interactive ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Interactive || Host is null)
            return;
        if (@event is not InputEventMouseButton mouse || !mouse.Pressed)
            return;
        if (mouse.ButtonIndex is not (MouseButton.Left or MouseButton.Right))
            return;

        Picked?.Invoke(GetLocalMousePosition());
        AcceptEvent();
    }

    public override void _Draw()
    {
        if (Host is null)
            return;

        var host = Host;
        var anchor = ViewAnchor;
        var topology = host.World.Topology;
        var width = host.World.Configuration.Width;
        var center = Size / 2f;

        for (var dy = -ViewRadiusY; dy <= ViewRadiusY; dy++)
        {
            for (var dx = -ViewRadiusX; dx <= ViewRadiusX; dx++)
            {
                var resolution = topology.Resolve(anchor.X + dx, anchor.Y + dy);
                var paintCell = new LogicalGridCoordinate(anchor.X + dx, anchor.Y + dy);
                var screen = Project(center, resolution.TryGetCell(out var resolved) ? resolved : paintCell);
                if (!resolution.IsInsideWorld || !resolution.TryGetCell(out var cell))
                {
                    DrawColoredPolygon(HexAt(screen), new Color(0.12f, 0.07f, 0.07f));
                    continue;
                }

                var paintChunk = host.World.Chunks.ToAddress(cell).Chunk;
                if (ShowExploration)
                {
                    var color = ColorFromKnowledge(host, paintChunk);
                    if (ShowLod && host.Exploration.LevelOf(paintChunk) != ExplorationKnowledgeLevel.Unknown)
                        color = color.Lerp(ColorForLod(host.Lod.Classify(paintChunk)), 0.35f);
                    DrawColoredPolygon(HexAt(screen), color);
                }
                else
                {
                    var textureSize = new Vector2(SimpleTextures.Tile, SimpleTextures.Tile);
                    DrawTextureRect(
                        SimpleTextures.Biome(host.World.Grid.GetCell(cell).Biome),
                        new Rect2(screen - textureSize * 0.5f, textureSize),
                        false);
                    if (HighContrast)
                        DrawPolyline(HexAt(screen, 0.92f, closed: true), new Color(1f, 1f, 1f, 0.55f), 1.2f, true);
                    var terrain = host.World.Grid.GetCell(cell);
                    if (terrain.HasRiver)
                        DrawColoredPolygon(HexAt(screen, 0.42f), new Color(0.20f, 0.45f, 0.72f, 0.40f));
                    if (terrain.IsOccupied)
                        DrawColoredPolygon(HexAt(screen, 0.55f), new Color(0.85f, 0.55f, 0.18f, 0.22f));
                    if (cell.X == 0 || cell.X == width - 1)
                        DrawPolyline(HexAt(screen, 0.98f, closed: true), new Color(1, 1, 1, 0.10f), 1, true);
                    if (ShowLod)
                        DrawColoredPolygon(HexAt(screen, 0.88f), new Color(ColorForLod(host.Lod.Classify(paintChunk)), 0.22f));
                }

                if (cell.Equals(host.Cursor.Position))
                    DrawPolyline(HexAt(screen, 1.05f, closed: true), new Color(0.95f, 0.86f, 0.45f), 2.2f, true);
            }
        }

        DrawBuildings(center, anchor);
        DrawSettlements(center, anchor);
        DrawCharacters(center, anchor);
    }

    public float CameraIsoX { get; set; }
    public float CameraIsoY { get; set; }
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
            if (Math.Abs(dx) > ViewRadiusX || Math.Abs(dy) > ViewRadiusY)
                continue;

            var pos = Project(center, settlement.Core);
            var color = ColorForSettlement(settlement.Id, settlement.Lifecycle);
            DrawArc(pos, CellSize * 0.55f, 0, MathF.Tau, 20, color, 2);
            if (SelectedSettlementId == settlement.Id)
                DrawArc(pos, CellSize * 0.78f, 0, MathF.Tau, 24, new Color(1f, 1f, 0.55f), 2);
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
                if (Math.Abs(dx) > ViewRadiusX || Math.Abs(dy) > ViewRadiusY)
                    continue;

                var pos = Project(center, cell);
                var sprite = new Rect2(pos.X - 28, pos.Y - 32, SimpleTextures.Tile * 0.85f, SimpleTextures.Tile * 0.85f);
                DrawTextureRect(SimpleTextures.Building(building.TypeId), sprite, false);
                if (SelectedBuildingId == building.Id)
                    DrawPolyline(HexAt(pos, 0.72f, closed: true), new Color(1f, 1f, 1f), 2, true);
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
            if (Math.Abs(dx) > ViewRadiusX || Math.Abs(dy) > ViewRadiusY)
                continue;

            var pos = Project(center, character.Position);
            var selected = SelectedId == character.Id;
            var sprite = new Rect2(pos.X - 26, pos.Y - 34, SimpleTextures.Tile * 0.82f, SimpleTextures.Tile * 0.82f);
            DrawTextureRect(SimpleTextures.Character(character.LifeStage, selected), sprite, false);
            if (selected)
                DrawDashedHex(pos, 1.12f, new Color(1f, 0.92f, 0.35f), 2.4f);
            if (character.Settlement.IsAssigned)
                DrawArc(pos, CellSize * 0.38f, 0, MathF.Tau, 12, ColorForSettlement(character.Settlement, SettlementLifecycle.Established));
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

    private Vector2 Project(Vector2 center, LogicalGridCoordinate cell)
    {
        var anchor = ViewAnchor;
        var dx = Host!.World.Topology.SignedHorizontalDelta(anchor.X, cell.X);
        var iso = _projection.ToIsoUnwrapped(anchor.X + dx, cell.Y);
        var cam = ViewIso;
        return new Vector2(center.X + iso.X - cam.X, center.Y + iso.Y - cam.Y);
    }

    private Vector2[] HexAt(Vector2 origin, float scale = 1f, bool closed = false)
    {
        var size = _projection.HexSize * scale;
        var count = closed ? 7 : 6;
        var verts = new Vector2[count];
        for (var i = 0; i < 6; i++)
        {
            var angle = (60 * i - 30) * MathF.PI / 180f;
            verts[i] = origin + new Vector2(
                size * MathF.Cos(angle),
                size * MathF.Sin(angle) * _projection.VerticalScale);
        }

        if (closed)
            verts[6] = verts[0];

        return verts;
    }

    private void DrawDashedHex(Vector2 origin, float scale, Color color, float width)
    {
        var verts = HexAt(origin, scale);
        for (var i = 0; i < 6; i++)
            DrawDashedLine(verts[i], verts[(i + 1) % 6], color, width, 5.5f, false, true);
    }

    private IsoPoint ViewIso =>
        FocusOverride is { } cell ? _projection.ToIso(cell) : new IsoPoint(CameraIsoX, CameraIsoY);

    private LogicalGridCoordinate ViewAnchor
    {
        get
        {
            if (FocusOverride is { } overrideCell)
                return overrideCell;
            if (Host is null)
                return default;
            var raw = _projection.ApproximateCell(CameraIsoX, CameraIsoY);
            var resolution = Host.World.Topology.Resolve(raw.X, raw.Y);
            if (resolution.TryGetCell(out var cell))
                return cell;
            return new LogicalGridCoordinate(
                Host.World.Topology.WrapX(raw.X),
                Math.Clamp(raw.Y, 0, Host.World.Configuration.Height - 1));
        }
    }

    public bool TryPick(Vector2 local, out LogicalGridCoordinate cell, out CharacterId? person)
    {
        cell = default;
        person = null;
        if (Host is null)
            return false;

        var cursor = ViewAnchor;
        var center = Size / 2f;
        var topology = Host.World.Topology;
        CharacterState? nearestPerson = null;
        var nearestPersonDistance = float.MaxValue;
        foreach (var character in Host.Population.All)
        {
            if (!character.IsAlive)
                continue;
            var dx = topology.SignedHorizontalDelta(cursor.X, character.Position.X);
            var dy = character.Position.Y - cursor.Y;
            if (Math.Abs(dx) > ViewRadiusX || Math.Abs(dy) > ViewRadiusY)
                continue;
            var screen = Project(center, character.Position);
            var distance = local.DistanceSquaredTo(screen);
            if (distance >= nearestPersonDistance || distance > CellSize * CellSize * 2.4f)
                continue;
            nearestPersonDistance = distance;
            nearestPerson = character;
        }

        if (nearestPerson is not null)
        {
            cell = nearestPerson.Position;
            person = nearestPerson.Id;
            return true;
        }

        var best = float.MaxValue;
        LogicalGridCoordinate? found = null;
        for (var dy = -ViewRadiusY; dy <= ViewRadiusY; dy++)
        {
            for (var dx = -ViewRadiusX; dx <= ViewRadiusX; dx++)
            {
                var resolution = topology.Resolve(cursor.X + dx, cursor.Y + dy);
                if (!resolution.TryGetCell(out var candidate))
                    continue;
                var screen = Project(center, candidate);
                var distance = local.DistanceSquaredTo(screen);
                if (distance >= best || distance > CellSize * CellSize * 1.6f)
                    continue;
                best = distance;
                found = candidate;
            }
        }

        if (found is null)
            return false;

        cell = found.Value;
        return true;
    }
}

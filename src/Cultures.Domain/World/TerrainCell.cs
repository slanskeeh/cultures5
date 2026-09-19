using Cultures.Core.Ids;

namespace Cultures.World;

/// <summary>
/// Dynamic overlay token. Terrain stays separate; this is not an entity manager.
/// </summary>
public enum OccupantKind : byte
{
    None = 0,
    DebugMarker = 1,
    Building = 2
}

public readonly record struct Occupancy(OccupantKind Kind, ulong EntityValue = 0, bool BlocksMovement = false)
{
    public static Occupancy Empty { get; } = new(OccupantKind.None);
    public static Occupancy DebugMarker { get; } = new(OccupantKind.DebugMarker, 0, true);

    public static Occupancy ForBuilding(BuildingId id, bool blocksMovement) =>
        new(OccupantKind.Building, id.Value, blocksMovement);

    public BuildingId BuildingId => Kind == OccupantKind.Building ? new BuildingId(EntityValue) : BuildingId.None;

    public bool IsOccupied => Kind != OccupantKind.None;
    public override string ToString() => Kind == OccupantKind.Building ? $"Building:{EntityValue}" : Kind.ToString();
}

/// <summary>
/// Climate is independent of biome. Values are 0..1 and provisional.
/// </summary>
public readonly record struct ClimateSample(float Temperature, float Moisture)
{
    public override string ToString() => $"Climate(t={Temperature:0.00}, m={Moisture:0.00})";
}

/// <summary>
/// Placeholder biomes for classification, not the final content list.
/// </summary>
public enum BiomeId : byte
{
    Ocean = 0,
    Ice = 1,
    Tundra = 2,
    TemperateLand = 3,
    Forest = 4,
    Desert = 5,
    Highland = 6,
    Swamp = 7,
    Savanna = 8,
    Taiga = 9
}

/// <summary>
/// Coarse land/water kind derived from elevation vs sea level.
/// </summary>
public enum TerrainKind : byte
{
    Land = 0,
    Water = 1
}

/// <summary>
/// Static generated fields for one cell. Occupancy is applied by the logical grid overlay.
/// </summary>
public readonly record struct GeneratedTerrain(
    float Elevation,
    bool IsWater,
    ClimateSample Climate,
    BiomeId Biome,
    bool HasRiver = false,
    float Fertility = 0f)
{
    public TerrainKind Kind => IsWater ? TerrainKind.Water : TerrainKind.Land;
    public bool Passable => !IsWater;

    public TerrainCell WithOccupancy(Occupancy occupancy) => new(this, occupancy);
}

/// <summary>
/// Authoritative terrain cell: generated geography plus occupancy overlay.
/// </summary>
public readonly record struct TerrainCell(GeneratedTerrain Generated, Occupancy Occupancy)
{
    public static TerrainCell Unoccupied(GeneratedTerrain generated) => new(generated, Occupancy.Empty);

    public float Elevation => Generated.Elevation;
    public bool IsWater => Generated.IsWater;
    public ClimateSample Climate => Generated.Climate;
    public BiomeId Biome => Generated.Biome;
    public bool HasRiver => Generated.HasRiver;
    public float Fertility => Generated.Fertility;
    public TerrainKind Kind => Generated.Kind;
    public bool Passable => Generated.Passable;
    public bool IsOccupied => Occupancy.IsOccupied;

    public TerrainCell WithOccupancy(Occupancy occupancy) => this with { Occupancy = occupancy };

    public override string ToString() =>
        $"Terrain(e={Elevation:0.00}, water={IsWater}, {Biome}, {Climate}, occ={Occupancy})";
}

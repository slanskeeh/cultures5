namespace Cultures.World;

/// <summary>
/// Generic occupancy token. Future building/character systems should replace this
/// with typed IDs rather than storing occupancy on sprites.
/// </summary>
public enum OccupantKind : byte
{
    None = 0,
    DebugMarker = 1
}

public readonly record struct Occupancy(OccupantKind Kind)
{
    public static Occupancy Empty { get; } = new(OccupantKind.None);
    public static Occupancy DebugMarker { get; } = new(OccupantKind.DebugMarker);

    public bool IsOccupied => Kind != OccupantKind.None;
    public override string ToString() => Kind.ToString();
}

/// <summary>
/// Minimal terrain cell. Biomes, resources and elevation are intentionally absent.
/// </summary>
public enum TerrainKind : byte
{
    Unspecified = 0
}

public readonly record struct TerrainCell(TerrainKind Kind, bool Passable, Occupancy Occupancy)
{
    public static TerrainCell Empty { get; } = new(TerrainKind.Unspecified, Passable: true, Occupancy.Empty);

    public bool IsOccupied => Occupancy.IsOccupied;

    public TerrainCell WithOccupancy(Occupancy occupancy) => this with { Occupancy = occupancy };

    public override string ToString() =>
        $"Terrain({Kind}, passable={Passable}, occupancy={Occupancy})";
}

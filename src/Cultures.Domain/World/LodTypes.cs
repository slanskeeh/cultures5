namespace Cultures.World;

/// <summary>
/// Authoritative simulation detail. Independent from whether Godot has loaded a chunk.
/// </summary>
public enum SimulationLodTier : byte
{
    Full = 0,
    Reduced = 1,
    Aggregate = 2,
    Macro = 3
}

/// <summary>
/// Presentation presence. A chunk may be simulated without any Node.
/// </summary>
public enum ChunkPresentationPresence : byte
{
    Unloaded = 0,
    Loaded = 1
}

public static class SimulationLodTiers
{
    public static bool IsDetailed(this SimulationLodTier tier) =>
        tier is SimulationLodTier.Full or SimulationLodTier.Reduced;

    public static bool IsAggregate(this SimulationLodTier tier) =>
        tier is SimulationLodTier.Aggregate or SimulationLodTier.Macro;
}

/// <summary>
/// Who may keep full individual simulation inside a coarser chunk.
/// </summary>
public static class LodProtection
{
    public const byte None = 0;
    public const byte Selected = 1;
    public const byte Commanded = 2;
    public const byte Notable = 4;
}

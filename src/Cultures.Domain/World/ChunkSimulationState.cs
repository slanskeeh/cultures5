using Cultures.Core.Ids;

namespace Cultures.World;

/// <summary>
/// Derived census for a simulation chunk. Not a second inventory and not terrain.
/// </summary>
public sealed class ChunkCensus
{
    public int Population { get; set; }
    public int Infants { get; set; }
    public int Children { get; set; }
    public int Adolescents { get; set; }
    public int Adults { get; set; }
    public int Elders { get; set; }
    public int Buildings { get; set; }
    public int Shelters { get; set; }
    public int Food { get; set; }
    public int Wood { get; set; }
    public int Stone { get; set; }
    public int FarmingSkillSum { get; set; }
    public int WoodworkingSkillSum { get; set; }
    public int StoneworkingSkillSum { get; set; }
    public int CraftingSkillSum { get; set; }
    public int MigrationPressure { get; set; }
    public SettlementId Settlement { get; set; }

    public void Clear()
    {
        Population = 0;
        Infants = 0;
        Children = 0;
        Adolescents = 0;
        Adults = 0;
        Elders = 0;
        Buildings = 0;
        Shelters = 0;
        Food = 0;
        Wood = 0;
        Stone = 0;
        FarmingSkillSum = 0;
        WoodworkingSkillSum = 0;
        StoneworkingSkillSum = 0;
        CraftingSkillSum = 0;
        MigrationPressure = 0;
        Settlement = SettlementId.None;
    }

    public ChunkCensusSnapshot Snapshot() => new(
        Population,
        Infants,
        Children,
        Adolescents,
        Adults,
        Elders,
        Buildings,
        Shelters,
        Food,
        Wood,
        Stone,
        FarmingSkillSum,
        WoodworkingSkillSum,
        StoneworkingSkillSum,
        CraftingSkillSum,
        MigrationPressure,
        Settlement.Value);
}

public readonly record struct ChunkCensusSnapshot(
    int Population,
    int Infants,
    int Children,
    int Adolescents,
    int Adults,
    int Elders,
    int Buildings,
    int Shelters,
    int Food,
    int Wood,
    int Stone,
    int FarmingSkillSum,
    int WoodworkingSkillSum,
    int StoneworkingSkillSum,
    int CraftingSkillSum,
    int MigrationPressure,
    ulong Settlement);

/// <summary>
/// Per-chunk simulation record. Created on demand; not a planet-sized array.
/// </summary>
public sealed class ChunkSimulationState
{
    public ChunkSimulationState(ChunkCoordinate coordinate)
    {
        Coordinate = coordinate;
        SimulationTier = SimulationLodTier.Macro;
        Presentation = ChunkPresentationPresence.Unloaded;
        Census = new ChunkCensus();
    }

    public ChunkCoordinate Coordinate { get; }
    public SimulationLodTier SimulationTier { get; set; }
    public ChunkPresentationPresence Presentation { get; set; }
    public SimulationLodTier? ForcedTier { get; set; }
    public ChunkCensus Census { get; }
    public ulong LastClassifiedTick { get; set; }

    public SimulationLodTier EffectiveTier => ForcedTier ?? SimulationTier;

    public ChunkSimulationSnapshot Snapshot() => new(
        Coordinate,
        EffectiveTier,
        Presentation,
        Census.Snapshot());
}

public readonly record struct ChunkSimulationSnapshot(
    ChunkCoordinate Coordinate,
    SimulationLodTier Tier,
    ChunkPresentationPresence Presentation,
    ChunkCensusSnapshot Census);

/// <summary>
/// Sparse chunk simulation index. Terrain cache stays on <see cref="WorldGenerator"/>.
/// </summary>
public sealed class ChunkSimulationDirectory
{
    private readonly Dictionary<(int X, int Y), ChunkSimulationState> _byChunk = new();
    private readonly List<ChunkSimulationState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<ChunkSimulationState> All => _order;

    public ChunkSimulationState GetOrCreate(ChunkCoordinate coordinate)
    {
        if (_byChunk.TryGetValue((coordinate.X, coordinate.Y), out var existing))
            return existing;

        var state = new ChunkSimulationState(coordinate);
        _byChunk[(coordinate.X, coordinate.Y)] = state;
        _order.Add(state);
        _order.Sort(static (a, b) =>
        {
            var y = a.Coordinate.Y.CompareTo(b.Coordinate.Y);
            return y != 0 ? y : a.Coordinate.X.CompareTo(b.Coordinate.X);
        });
        return state;
    }

    public bool TryGet(ChunkCoordinate coordinate, out ChunkSimulationState state) =>
        _byChunk.TryGetValue((coordinate.X, coordinate.Y), out state!);
}

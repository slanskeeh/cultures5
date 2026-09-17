using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Exploration;

/// <summary>
/// Player knowledge of one spatial chunk. Missing directory records are Unknown.
/// </summary>
public sealed class ChunkExplorationKnowledge
{
    public ChunkExplorationKnowledge(ChunkCoordinate coordinate)
    {
        Coordinate = coordinate;
        Level = ExplorationKnowledgeLevel.Unknown;
        Source = ExplorationSource.None;
        Terrain = default;
        Biome = default;
        Climate = default;
        Notes = new List<DiscoveryNote>();
    }

    public ChunkCoordinate Coordinate { get; }
    public ChunkId PersistentChunk { get; set; }
    public ExplorationKnowledgeLevel Level { get; set; }
    public ExplorationSource Source { get; set; }
    public ulong DiscoveredTick { get; set; }
    public ulong UpdatedTick { get; set; }
    public TerrainKnowledge Terrain { get; set; }
    public BiomeKnowledge Biome { get; set; }
    public ClimateKnowledge Climate { get; set; }
    public List<DiscoveryNote> Notes { get; }

    public ChunkExplorationSnapshot Snapshot() => new(
        Coordinate,
        PersistentChunk.Value,
        Level,
        Source,
        DiscoveredTick,
        UpdatedTick,
        Terrain,
        Biome,
        Climate,
        Notes.Count);
}

public readonly record struct ChunkExplorationSnapshot(
    ChunkCoordinate Coordinate,
    ulong PersistentChunk,
    ExplorationKnowledgeLevel Level,
    ExplorationSource Source,
    ulong DiscoveredTick,
    ulong UpdatedTick,
    TerrainKnowledge Terrain,
    BiomeKnowledge Biome,
    ClimateKnowledge Climate,
    int NoteCount);

/// <summary>
/// Sparse player-knowledge index. Unknown chunks have no entry.
/// </summary>
public sealed class ExplorationKnowledgeDirectory
{
    private readonly Dictionary<(int X, int Y), ChunkExplorationKnowledge> _byChunk = new();
    private readonly List<ChunkExplorationKnowledge> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<ChunkExplorationKnowledge> All => _order;

    public ChunkExplorationKnowledge GetOrCreate(ChunkCoordinate coordinate)
    {
        if (_byChunk.TryGetValue((coordinate.X, coordinate.Y), out var existing))
            return existing;

        var record = new ChunkExplorationKnowledge(coordinate);
        _byChunk[(coordinate.X, coordinate.Y)] = record;
        _order.Add(record);
        _order.Sort(static (a, b) =>
        {
            var y = a.Coordinate.Y.CompareTo(b.Coordinate.Y);
            return y != 0 ? y : a.Coordinate.X.CompareTo(b.Coordinate.X);
        });
        return record;
    }

    public bool TryGet(ChunkCoordinate coordinate, out ChunkExplorationKnowledge knowledge) =>
        _byChunk.TryGetValue((coordinate.X, coordinate.Y), out knowledge!);
}

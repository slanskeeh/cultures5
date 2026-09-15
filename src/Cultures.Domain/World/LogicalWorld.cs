namespace Cultures.World;

/// <summary>
/// Composes spatial services and on-demand terrain generation. Not a gameplay god-object.
/// </summary>
public sealed class LogicalWorld
{
    public LogicalWorld(WorldConfiguration configuration, ulong seed)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Seed = seed;
        Topology = new WorldTopology(Configuration);
        Chunks = new ChunkLayout(Topology);
        Generator = new WorldGenerator(seed, Configuration, Chunks);
        Grid = new LogicalGrid(Topology, Generator);
    }

    public ulong Seed { get; }
    public WorldConfiguration Configuration { get; }
    public WorldTopology Topology { get; }
    public ChunkLayout Chunks { get; }
    public WorldGenerator Generator { get; }
    public LogicalGrid Grid { get; }
}

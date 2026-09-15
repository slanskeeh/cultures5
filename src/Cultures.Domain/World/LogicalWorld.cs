namespace Cultures.World;

/// <summary>
/// Composes world spatial services. Not a gameplay god-object: no economy, characters, or generation.
/// </summary>
public sealed class LogicalWorld
{
    public LogicalWorld(WorldConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Topology = new WorldTopology(Configuration);
        Chunks = new ChunkLayout(Topology);
        Grid = new LogicalGrid(Topology);
    }

    public WorldConfiguration Configuration { get; }
    public WorldTopology Topology { get; }
    public ChunkLayout Chunks { get; }
    public LogicalGrid Grid { get; }
}

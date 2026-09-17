using Cultures.Core.Events;
using Cultures.World;

namespace Cultures.Exploration;

public sealed record ChunkKnowledgeChangedEvent(
    ulong Tick,
    ChunkCoordinate Chunk,
    ExplorationKnowledgeLevel Previous,
    ExplorationKnowledgeLevel Current) : ISimulationEvent;

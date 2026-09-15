using Cultures.Core.Events;
using Cultures.World;

namespace Cultures.World;

public sealed record ChunkLodChangedEvent(
    ulong Tick,
    ChunkCoordinate Chunk,
    SimulationLodTier Previous,
    SimulationLodTier Current) : ISimulationEvent;

public sealed record CharactersAggregatedEvent(ulong Tick, ChunkCoordinate Chunk, int Count) : ISimulationEvent;

public sealed record CharactersReconstructedEvent(ulong Tick, ChunkCoordinate Chunk, int Count) : ISimulationEvent;

public sealed record AggregateBirthsOccurredEvent(ulong Tick, ChunkCoordinate Chunk, int Count) : ISimulationEvent;

public sealed record AggregateDeathsOccurredEvent(ulong Tick, ChunkCoordinate Chunk, int Count) : ISimulationEvent;

public sealed record AggregateFoodShortageEvent(ulong Tick, ChunkCoordinate Chunk, int Population, int Food)
    : ISimulationEvent;

public sealed record MigrationPressureChangedEvent(ulong Tick, ChunkCoordinate Chunk, int Pressure) : ISimulationEvent;

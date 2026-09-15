using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Buildings;

public sealed record BuildingPlacedEvent(ulong Tick, BuildingId Building, string TypeId) : ISimulationEvent;

public sealed record BuildingCompletedEvent(ulong Tick, BuildingId Building) : ISimulationEvent;

public sealed record BuildingRemovedEvent(ulong Tick, BuildingId Building) : ISimulationEvent;

public sealed record ResourceProducedEvent(ulong Tick, BuildingId Building, RecipeId Recipe) : ISimulationEvent;

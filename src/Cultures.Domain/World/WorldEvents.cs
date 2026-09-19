using Cultures.Core.Events;

namespace Cultures.World;

public sealed record SeasonChangedEvent(ulong Tick, int SeasonIndex, ulong Year) : ISimulationEvent;

public sealed record WildlifeHuntedEvent(
    ulong Tick,
    ulong Character,
    WildlifeSpecies Species,
    int FoodGained) : ISimulationEvent;

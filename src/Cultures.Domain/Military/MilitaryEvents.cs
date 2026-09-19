using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Military;

public sealed record MilitaryUnitCreatedEvent(ulong Tick, MilitaryUnitId Unit, FactionId Faction) : ISimulationEvent;

public sealed record MilitaryMembershipChangedEvent(
    ulong Tick,
    CharacterId Character,
    MilitaryUnitId Previous,
    MilitaryUnitId Current) : ISimulationEvent;

public sealed record MilitaryUnitDisbandedEvent(ulong Tick, MilitaryUnitId Unit, FactionId Faction) : ISimulationEvent;

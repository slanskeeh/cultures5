using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Civilization;

public sealed record CultureCreatedEvent(ulong Tick, CultureId Culture) : ISimulationEvent;

public sealed record FactionCreatedEvent(ulong Tick, FactionId Faction, CultureId Culture) : ISimulationEvent;

public sealed record FactionMembershipChangedEvent(
    ulong Tick,
    CharacterId Character,
    FactionId Previous,
    FactionId Current) : ISimulationEvent;

public sealed record CharacterCultureChangedEvent(
    ulong Tick,
    CharacterId Character,
    CultureId Previous,
    CultureId Current) : ISimulationEvent;

public sealed record FactionRelationChangedEvent(
    ulong Tick,
    FactionId Lower,
    FactionId Higher,
    FactionRelationStance Previous,
    FactionRelationStance Current) : ISimulationEvent;

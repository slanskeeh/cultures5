using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Civilization;

public sealed record PoliticalGroupCreatedEvent(ulong Tick, PoliticalGroupId Group, FactionId Faction) : ISimulationEvent;

public sealed record PoliticalGroupMembershipChangedEvent(
    ulong Tick,
    CharacterId Character,
    PoliticalGroupId Previous,
    PoliticalGroupId Current) : ISimulationEvent;

public sealed record PoliticalGroupInfluenceChangedEvent(
    ulong Tick,
    PoliticalGroupId Group,
    int Previous,
    int Current) : ISimulationEvent;

public sealed record InternalStabilityChangedEvent(
    ulong Tick,
    FactionId Faction,
    int Previous,
    int Current) : ISimulationEvent;

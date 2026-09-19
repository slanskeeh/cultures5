using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Ids;

namespace Cultures.Population;

public sealed record ProfessionChangedEvent(
    ulong Tick,
    CharacterId Character,
    ProfessionId Previous,
    ProfessionId Current) : ISimulationEvent;

public sealed record HouseholdFormedEvent(ulong Tick, HouseholdId Household, CharacterId Founder) : ISimulationEvent;

public sealed record PartnershipFormedEvent(ulong Tick, CharacterId Left, CharacterId Right) : ISimulationEvent;

public sealed record HouseholdHomeChangedEvent(ulong Tick, HouseholdId Household, BuildingId Home) : ISimulationEvent;

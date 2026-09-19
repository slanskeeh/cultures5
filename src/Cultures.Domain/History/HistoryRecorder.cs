using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Military;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.History;

/// <summary>
/// Observes domain events and stores historical facts. Does not mutate simulation.
/// </summary>
public sealed class HistoryRecorder
{
    public HistoryRecorder(EntityIdFactory ids, EventBus events)
    {
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Directory = new HistoryDirectory();
        Bind();
    }

    public EntityIdFactory Ids { get; }
    public EventBus Events { get; }
    public HistoryDirectory Directory { get; }
    public bool IsEnabled { get; set; } = true;

    public HistoryRecord? GetEvent(HistoryEventId id) =>
        Directory.TryGet(id, out var record) ? record : null;

    public IReadOnlyList<HistoryRecord> GetRecentEvents(int count) => Directory.GetRecent(count);

    public IReadOnlyList<HistoryRecord> GetEventsForCharacter(CharacterId id) => Directory.ForCharacter(id);

    public IReadOnlyList<HistoryRecord> GetEventsForSettlement(SettlementId id) => Directory.ForSettlement(id);

    public IReadOnlyList<HistoryRecord> GetEventsForFaction(FactionId id) => Directory.ForFaction(id);

    public IReadOnlyList<HistoryRecord> GetEventsForMilitaryUnit(MilitaryUnitId id) => Directory.ForMilitaryUnit(id);

    public void Record(
        ulong tick,
        HistoryKind kind,
        HistoryImportance importance,
        ulong subject,
        ulong secondary = 0,
        int? x = null,
        int? y = null,
        string? summary = null)
    {
        if (!IsEnabled)
            return;
        Directory.Add(new HistoryRecord(
            Ids.NextHistoryEvent(),
            tick,
            kind,
            importance,
            subject,
            secondary,
            x,
            y,
            summary));
    }

    private void Bind()
    {
        Events.Subscribe<ChildBornEvent>(e => Record(
            e.Tick, HistoryKind.CharacterBorn, HistoryImportance.Major, e.Child.Value, e.ParentA.Value,
            summary: "Character born"));
        Events.Subscribe<CharacterDiedEvent>(e => Record(
            e.Tick, HistoryKind.CharacterDied, HistoryImportance.Major, e.Character.Value, 0,
            e.Position.X, e.Position.Y, "Character died"));
        Events.Subscribe<BuildingPlacedEvent>(e => Record(
            e.Tick, HistoryKind.BuildingPlaced, HistoryImportance.Ordinary, e.Building.Value, 0,
            summary: $"Building placed {e.TypeId}"));
        Events.Subscribe<BuildingCompletedEvent>(e => Record(
            e.Tick, HistoryKind.BuildingCompleted, HistoryImportance.Ordinary, e.Building.Value,
            summary: "Building completed"));
        Events.Subscribe<BuildingRemovedEvent>(e => Record(
            e.Tick, HistoryKind.BuildingRemoved, HistoryImportance.Ordinary, e.Building.Value,
            summary: "Building removed"));
        Events.Subscribe<SettlementEmergedEvent>(e => Record(
            e.Tick, HistoryKind.SettlementEmerged, HistoryImportance.Major, e.Settlement.Value, 0,
            e.Core.X, e.Core.Y, "Settlement emerged"));
        Events.Subscribe<SettlementStageChangedEvent>(e => Record(
            e.Tick, HistoryKind.SettlementStageChanged, HistoryImportance.Major, e.Settlement.Value,
            summary: $"Settlement {e.Previous} → {e.Current}"));
        Events.Subscribe<SettlementAbandonedEvent>(e => Record(
            e.Tick, HistoryKind.SettlementAbandoned, HistoryImportance.Major, e.Settlement.Value,
            summary: "Settlement abandoned"));
        Events.Subscribe<CultureCreatedEvent>(e => Record(
            e.Tick, HistoryKind.CultureCreated, HistoryImportance.Major, e.Culture.Value,
            summary: "Culture created"));
        Events.Subscribe<FactionCreatedEvent>(e => Record(
            e.Tick, HistoryKind.FactionCreated, HistoryImportance.Major, e.Faction.Value, e.Culture.Value,
            summary: "Faction created"));
        Events.Subscribe<FactionMembershipChangedEvent>(e => Record(
            e.Tick, HistoryKind.FactionMembershipChanged, HistoryImportance.Ordinary, e.Character.Value,
            e.Current.Value, summary: "Faction membership changed"));
        Events.Subscribe<DiplomaticStanceChangedEvent>(e => Record(
            e.Tick, HistoryKind.DiplomaticStanceChanged, HistoryImportance.Major, e.Lower.Value, e.Higher.Value,
            summary: $"Diplomacy {e.Previous} → {e.Current}"));
        Events.Subscribe<PoliticalGroupCreatedEvent>(e => Record(
            e.Tick, HistoryKind.PoliticalGroupCreated, HistoryImportance.Ordinary, e.Group.Value, e.Faction.Value,
            summary: "Political group created"));
        Events.Subscribe<MilitaryUnitCreatedEvent>(e => Record(
            e.Tick, HistoryKind.MilitaryUnitCreated, HistoryImportance.Ordinary, e.Unit.Value, e.Faction.Value,
            summary: "Military unit created"));
        Events.Subscribe<MilitaryUnitDisbandedEvent>(e => Record(
            e.Tick, HistoryKind.MilitaryUnitDisbanded, HistoryImportance.Ordinary, e.Unit.Value, e.Faction.Value,
            summary: "Military unit disbanded"));
        Events.Subscribe<AggregateBirthsOccurredEvent>(e => Record(
            e.Tick, HistoryKind.AggregateBirths, HistoryImportance.Ordinary, 0, 0,
            e.Chunk.X, e.Chunk.Y, $"Aggregate births {e.Count}"));
        Events.Subscribe<AggregateDeathsOccurredEvent>(e => Record(
            e.Tick, HistoryKind.AggregateDeaths, HistoryImportance.Ordinary, 0, 0,
            e.Chunk.X, e.Chunk.Y, $"Aggregate deaths {e.Count}"));
        Events.Subscribe<ProfessionChangedEvent>(e => Record(
            e.Tick, HistoryKind.ProfessionChanged, HistoryImportance.Ordinary, e.Character.Value, 0,
            summary: $"Profession {e.Previous.Value} → {e.Current.Value}"));
        Events.Subscribe<HouseholdFormedEvent>(e => Record(
            e.Tick, HistoryKind.HouseholdFormed, HistoryImportance.Major, e.Household.Value, e.Founder.Value,
            summary: "Household formed"));
        Events.Subscribe<PartnershipFormedEvent>(e => Record(
            e.Tick, HistoryKind.PartnershipFormed, HistoryImportance.Major, e.Left.Value, e.Right.Value,
            summary: "Partnership formed"));
        Events.Subscribe<HouseholdHomeChangedEvent>(e => Record(
            e.Tick, HistoryKind.HouseholdHomeChanged, HistoryImportance.Ordinary, e.Household.Value,
            e.Home.Value, summary: "Household home changed"));
        Events.Subscribe<SeasonChangedEvent>(e => Record(
            e.Tick, HistoryKind.SeasonChanged, HistoryImportance.Ordinary, (ulong)e.SeasonIndex, e.Year,
            summary: $"Season {e.SeasonIndex} year {e.Year}"));
        Events.Subscribe<WildlifeHuntedEvent>(e => Record(
            e.Tick, HistoryKind.WildlifeHunted, HistoryImportance.Ordinary, e.Character, (ulong)e.Species,
            summary: $"Hunted {e.Species}"));
        Events.Subscribe<DiplomaticPactFormedEvent>(e => Record(
            e.Tick, HistoryKind.PactFormed, HistoryImportance.Major, e.Lower.Value, e.Higher.Value,
            summary: $"Pact {e.Kind}"));
        Events.Subscribe<DiplomaticPactBrokenEvent>(e => Record(
            e.Tick, HistoryKind.PactBroken, HistoryImportance.Major, e.Lower.Value, e.Higher.Value,
            summary: $"Pact broken {e.Kind}"));
    }
}

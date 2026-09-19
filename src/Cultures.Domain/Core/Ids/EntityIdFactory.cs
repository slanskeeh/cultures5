namespace Cultures.Core.Ids;

/// <summary>
/// Issues monotonically increasing typed IDs. Values are not array indexes.
/// </summary>
public sealed class EntityIdFactory
{
    private ulong _nextCharacter = 1;
    private ulong _nextFamily = 1;
    private ulong _nextBuilding = 1;
    private ulong _nextSettlement = 1;
    private ulong _nextCivilization = 1;
    private ulong _nextCulture = 2;
    private ulong _nextFaction = 1;
    private ulong _nextPoliticalGroup = 1;
    private ulong _nextMilitaryUnit = 1;
    private ulong _nextHousehold = 1;
    private ulong _nextHistoryEvent = 1;
    private ulong _nextResourceDeposit = 1;
    private ulong _nextDiplomaticPact = 1;
    private ulong _nextRegion = 1;
    private ulong _nextChunk = 1;
    private ulong _nextMigrationGroup = 1;

    public CharacterId NextCharacter() => new(_nextCharacter++);
    public FamilyId NextFamily() => new(_nextFamily++);
    public BuildingId NextBuilding() => new(_nextBuilding++);
    public SettlementId NextSettlement() => new(_nextSettlement++);
    public CivilizationId NextCivilization() => new(_nextCivilization++);
    public CultureId NextCulture() => new(_nextCulture++);
    public FactionId NextFaction() => new(_nextFaction++);
    public PoliticalGroupId NextPoliticalGroup() => new(_nextPoliticalGroup++);
    public MilitaryUnitId NextMilitaryUnit() => new(_nextMilitaryUnit++);
    public HouseholdId NextHousehold() => new(_nextHousehold++);
    public HistoryEventId NextHistoryEvent() => new(_nextHistoryEvent++);
    public ResourceDepositId NextResourceDeposit() => new(_nextResourceDeposit++);
    public DiplomaticPactId NextDiplomaticPact() => new(_nextDiplomaticPact++);
    public RegionId NextRegion() => new(_nextRegion++);
    public ChunkId NextChunk() => new(_nextChunk++);
    public MigrationGroupId NextMigrationGroup() => new(_nextMigrationGroup++);

    public EntityIdCounters Snapshot() => new(
        _nextCharacter,
        _nextFamily,
        _nextHousehold,
        _nextBuilding,
        _nextSettlement,
        _nextCivilization,
        _nextCulture,
        _nextFaction,
        _nextPoliticalGroup,
        _nextMilitaryUnit,
        _nextHistoryEvent,
        _nextResourceDeposit,
        _nextDiplomaticPact,
        _nextRegion,
        _nextChunk,
        _nextMigrationGroup);

    public void Restore(EntityIdCounters counters)
    {
        _nextCharacter = Math.Max(1, counters.NextCharacter);
        _nextFamily = Math.Max(1, counters.NextFamily);
        _nextHousehold = Math.Max(1, counters.NextHousehold);
        _nextBuilding = Math.Max(1, counters.NextBuilding);
        _nextSettlement = Math.Max(1, counters.NextSettlement);
        _nextCivilization = Math.Max(1, counters.NextCivilization);
        _nextCulture = Math.Max(2, counters.NextCulture);
        _nextFaction = Math.Max(1, counters.NextFaction);
        _nextPoliticalGroup = Math.Max(1, counters.NextPoliticalGroup);
        _nextMilitaryUnit = Math.Max(1, counters.NextMilitaryUnit);
        _nextHistoryEvent = Math.Max(1, counters.NextHistoryEvent);
        _nextResourceDeposit = Math.Max(1, counters.NextResourceDeposit);
        _nextDiplomaticPact = Math.Max(1, counters.NextDiplomaticPact);
        _nextRegion = Math.Max(1, counters.NextRegion);
        _nextChunk = Math.Max(1, counters.NextChunk);
        _nextMigrationGroup = Math.Max(1, counters.NextMigrationGroup);
    }
}

public readonly record struct EntityIdCounters(
    ulong NextCharacter,
    ulong NextFamily,
    ulong NextHousehold,
    ulong NextBuilding,
    ulong NextSettlement,
    ulong NextCivilization,
    ulong NextCulture,
    ulong NextFaction,
    ulong NextPoliticalGroup,
    ulong NextMilitaryUnit,
    ulong NextHistoryEvent,
    ulong NextResourceDeposit,
    ulong NextDiplomaticPact,
    ulong NextRegion,
    ulong NextChunk,
    ulong NextMigrationGroup);

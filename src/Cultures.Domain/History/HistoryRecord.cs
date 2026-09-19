using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.History;

public enum HistoryKind : byte
{
    CharacterBorn = 1,
    CharacterDied = 2,
    BuildingPlaced = 3,
    BuildingCompleted = 4,
    BuildingRemoved = 5,
    SettlementEmerged = 6,
    SettlementStageChanged = 7,
    SettlementAbandoned = 8,
    CultureCreated = 9,
    FactionCreated = 10,
    FactionMembershipChanged = 11,
    DiplomaticStanceChanged = 12,
    PoliticalGroupCreated = 13,
    MilitaryUnitCreated = 14,
    MilitaryUnitDisbanded = 15,
    AggregateBirths = 16,
    AggregateDeaths = 17,
    ProfessionChanged = 18,
    HouseholdFormed = 19,
    PartnershipFormed = 20,
    HouseholdHomeChanged = 21,
    SeasonChanged = 22,
    WildlifeHunted = 23,
    PactFormed = 24,
    PactBroken = 25
}

public enum HistoryImportance : byte
{
    Ordinary = 0,
    Major = 1
}

/// <summary>
/// Authoritative historical fact. Not AI memory and not player knowledge.
/// </summary>
public sealed class HistoryRecord
{
    public HistoryRecord(
        HistoryEventId id,
        ulong tick,
        HistoryKind kind,
        HistoryImportance importance,
        ulong subject,
        ulong secondary = 0,
        int? locationX = null,
        int? locationY = null,
        string? summary = null)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("History id is required.", nameof(id));

        Id = id;
        Tick = tick;
        Kind = kind;
        Importance = importance;
        Subject = subject;
        Secondary = secondary;
        LocationX = locationX;
        LocationY = locationY;
        Summary = summary ?? string.Empty;
    }

    public HistoryEventId Id { get; }
    public ulong Tick { get; }
    public HistoryKind Kind { get; }
    public HistoryImportance Importance { get; }
    public ulong Subject { get; }
    public ulong Secondary { get; }
    public int? LocationX { get; }
    public int? LocationY { get; }
    public string Summary { get; }

    public LogicalGridCoordinate? Location =>
        LocationX is { } x && LocationY is { } y ? new LogicalGridCoordinate(x, y) : null;

    public HistorySnapshot Snapshot() =>
        new(Id, Tick, Kind, Importance, Subject, Secondary, LocationX, LocationY, Summary);
}

public readonly record struct HistorySnapshot(
    HistoryEventId Id,
    ulong Tick,
    HistoryKind Kind,
    HistoryImportance Importance,
    ulong Subject,
    ulong Secondary,
    int? LocationX,
    int? LocationY,
    string Summary);

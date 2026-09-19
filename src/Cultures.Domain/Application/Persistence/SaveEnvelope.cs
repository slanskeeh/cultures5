using System.Text.Json;
using System.Text.Json.Serialization;
using Cultures.World;

namespace Cultures.Application.Persistence;

/// <summary>
/// Versioned save envelope. Static terrain is reconstructed from seed + generation contract.
/// v2 is header-only. v3 stores dynamic state. v4 adds pacts, speed, and session flags.
/// </summary>
public sealed record SaveEnvelope
{
    public const int CurrentVersion = 4;
    public const int MinimumSupportedVersion = 2;

    public int SaveVersion { get; init; } = CurrentVersion;
    public ulong WorldSeed { get; init; }
    public ulong SimulationTick { get; init; }
    public int GenerationVersion { get; init; } = WorldGeneration.CurrentVersion;
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public int ChunkWidth { get; init; }
    public int ChunkHeight { get; init; }
    public int CursorX { get; init; }
    public int CursorY { get; init; }
    public EntityIdCountersRecord? Ids { get; init; }
    public List<CharacterSaveRecord>? Characters { get; init; }
    public List<BuildingSaveRecord>? Buildings { get; init; }
    public List<SettlementSaveRecord>? Settlements { get; init; }
    public List<HouseholdSaveRecord>? Households { get; init; }
    public List<CultureRecord>? Cultures { get; init; }
    public List<FactionRecord>? Factions { get; init; }
    public List<FactionRelationRecord>? Relations { get; init; }
    public List<PoliticalGroupRecord>? PoliticalGroups { get; init; }
    public List<InternalPoliticsRecord>? Stability { get; init; }
    public List<MilitaryUnitRecord>? MilitaryUnits { get; init; }
    public List<ExplorationKnowledgeRecord>? Exploration { get; init; }
    public List<HistorySaveRecord>? History { get; init; }
    public List<DepositSaveRecord>? Deposits { get; init; }
    public List<WildlifeSaveRecord>? Wildlife { get; init; }
    public List<LodOverrideRecord>? LodOverrides { get; init; }
    public List<OccupancySaveRecord>? Occupancy { get; init; }
    public List<DiplomaticPactSaveRecord>? Pacts { get; init; }
    public int Speed { get; init; } = 1;
    public bool OnboardingComplete { get; init; }
}

public sealed record EntityIdCountersRecord(
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

public sealed class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public string Serialize(SaveEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return JsonSerializer.Serialize(envelope, Options);
    }

    public SaveEnvelope Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Save JSON is empty.", nameof(json));

        var envelope = JsonSerializer.Deserialize<SaveEnvelope>(json, Options)
            ?? throw new InvalidOperationException("Save JSON deserialized to null.");

        if (envelope.SaveVersion < SaveEnvelope.MinimumSupportedVersion
            || envelope.SaveVersion > SaveEnvelope.CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported save version {envelope.SaveVersion}.");
        }

        return SaveMigrations.ToCurrent(envelope);
    }
}

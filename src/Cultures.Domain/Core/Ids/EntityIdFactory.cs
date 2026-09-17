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
    public RegionId NextRegion() => new(_nextRegion++);
    public ChunkId NextChunk() => new(_nextChunk++);
    public MigrationGroupId NextMigrationGroup() => new(_nextMigrationGroup++);
}

using Cultures.Application.Persistence;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Application.Persistence;

public sealed record InventoryEntry(byte Type, int Quantity);

public sealed record SkillSaveRecord(byte Skill, int Experience);

public sealed record GridPointRecord(int X, int Y);

public sealed record CharacterSaveRecord(
    ulong Id,
    string Name,
    ulong AppearanceSeed,
    double AgeYears,
    byte LifeStage,
    int X,
    int Y,
    float Hunger,
    float Fatigue,
    float Health,
    float HealthMax,
    List<InventoryEntry> Inventory,
    List<SkillSaveRecord> Skills,
    List<ulong> Parents,
    List<ulong> Children,
    List<ulong> Caregivers,
    byte ActionKind,
    int ActionDuration,
    int ActionProgress,
    int? ActionTargetX,
    int? ActionTargetY,
    ulong ActionPartner,
    byte? ActionSkill,
    List<GridPointRecord> Path,
    ulong WorkplaceBuilding,
    int WorkplaceSlot,
    ulong Family,
    ulong Household,
    ulong Settlement,
    ulong Culture,
    ulong Faction,
    ulong PoliticalGroup,
    ulong MilitaryUnit,
    string Profession,
    ulong Partner,
    byte LodTier,
    bool PersistentIndividual,
    bool PlayerCommanded);

public sealed record BuildingSaveRecord(
    ulong Id,
    string TypeId,
    int OriginX,
    int OriginY,
    byte Lifecycle,
    ulong Settlement,
    string? Recipe,
    int ProductionProgress,
    int ProductionDuration,
    List<InventoryEntry> Inventory,
    List<ulong> Workers);

public sealed record SettlementSaveRecord(
    ulong Id,
    string NameKey,
    ulong FoundedTick,
    ulong Culture,
    ulong Leader,
    byte Lifecycle,
    int CoreX,
    int CoreY,
    int PresenceEvals,
    int UnmatchedEvals);

public sealed record HouseholdSaveRecord(ulong Id, ulong FoundedTick, ulong Home);

public sealed record HistorySaveRecord(
    ulong Id,
    ulong Tick,
    byte Kind,
    byte Importance,
    ulong Subject,
    ulong Secondary,
    int? X,
    int? Y,
    string Summary);

public sealed record DepositSaveRecord(ulong Id, int X, int Y, byte Resource, int Stock, int Capacity);

public sealed record WildlifeSaveRecord(int ChunkX, int ChunkY, int Deer, int Sheep, int Boar, int Birds);

public sealed record LodOverrideRecord(int ChunkX, int ChunkY, byte Tier);

public sealed record OccupancySaveRecord(int X, int Y, byte Kind, ulong Entity, bool Blocks);

public sealed record DiplomaticPactSaveRecord(ulong Id, ulong Lower, ulong Higher, byte Kind, ulong FormedTick);

public static class InventorySave
{
    public static List<InventoryEntry> ToEntries(Inventory inventory) =>
        inventory.Enumerate().Select(s => new InventoryEntry((byte)s.Type, s.Quantity)).ToList();

    public static void Fill(Inventory inventory, IEnumerable<InventoryEntry>? entries)
    {
        if (entries is null)
            return;
        foreach (var entry in entries)
            inventory.TryAdd((ResourceType)entry.Type, entry.Quantity);
    }
}

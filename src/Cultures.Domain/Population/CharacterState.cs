using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Authoritative character. Skills and family links belong to the person, not a workplace.
/// Personality/profession remain future extension points (AppearanceSeed).
/// </summary>
public sealed class CharacterState
{
    public CharacterState(CharacterId id, LogicalGridCoordinate position, ulong appearanceSeed)
    {
        Id = id;
        Position = position;
        AppearanceSeed = appearanceSeed;
        Name = $"Person {id.Value}";
        AgeYears = CharacterRules.StartingAgeYears;
        LifeStage = CharacterLifeStage.Adult;
        Needs.Hunger = CharacterRules.StartingHunger;
        Needs.Fatigue = CharacterRules.StartingFatigue;
        Family = FamilyId.None;
        Household = HouseholdId.None;
        Settlement = SettlementId.None;
        Culture = CultureId.Neutral;
        Faction = FactionId.None;
        PoliticalGroup = PoliticalGroupId.None;
        MilitaryUnit = MilitaryUnitId.None;
        Profession = ProfessionId.None;
        Partner = CharacterId.None;
        AssignedWorkplace = WorkplaceId.None;
    }

    public CharacterId Id { get; }
    public string Name { get; set; }
    public ulong AppearanceSeed { get; }
    public double AgeYears { get; set; }
    public CharacterLifeStage LifeStage { get; set; }
    public LogicalGridCoordinate Position { get; set; }
    public CharacterNeeds Needs { get; } = new();
    public CharacterHealth Health { get; } = new();
    public Inventory Inventory { get; } = new(CharacterRules.PersonalInventoryCapacity);
    public CharacterSkills Skills { get; } = new();
    public FamilyLinks FamilyLinks { get; } = new();
    public CharacterActivity Activity { get; } = new();
    public WorkplaceId AssignedWorkplace { get; set; }
    public FamilyId Family { get; set; }
    public HouseholdId Household { get; set; }
    public SettlementId Settlement { get; set; }
    public CultureId Culture { get; set; }
    public FactionId Faction { get; set; }
    public PoliticalGroupId PoliticalGroup { get; set; }
    public MilitaryUnitId MilitaryUnit { get; set; }
    public ProfessionId Profession { get; set; }
    public CharacterId Partner { get; set; }
    public SimulationLodTier LodTier { get; set; } = SimulationLodTier.Full;
    public bool IsPersistentIndividual { get; set; }
    public bool IsPlayerCommanded { get; set; }

    public bool IsProtectedFromAggregation => IsPersistentIndividual || IsPlayerCommanded;
    public bool IsIndividuallySimulated => LodTier.IsDetailed() || IsProtectedFromAggregation;

    public bool IsAlive => LifeStage != CharacterLifeStage.Dead && Health.IsAlive;

    public CharacterSnapshot Snapshot() => new(
        Id,
        Position,
        (float)AgeYears,
        LifeStage,
        Needs.Hunger,
        Needs.Fatigue,
        Health.Current,
        Inventory.GetQuantity(ResourceType.Food),
        Activity.Kind,
        Activity.ProgressTicks,
        AssignedWorkplace.Building.Value,
        AssignedWorkplace.Slot,
        Skills.GetLevel(SkillType.Farming),
        Skills.GetLevel(SkillType.Woodworking),
        Skills.GetLevel(SkillType.Stoneworking),
        Skills.GetLevel(SkillType.Crafting),
        FamilyLinks.Parents.Count > 0 ? FamilyLinks.Parents[0].Value : 0UL,
        FamilyLinks.Parents.Count > 1 ? FamilyLinks.Parents[1].Value : 0UL,
        FamilyLinks.Children.Count,
        Settlement.Value,
        (byte)LodTier,
        IsPersistentIndividual);
}

public readonly record struct CharacterSnapshot(
    CharacterId Id,
    LogicalGridCoordinate Position,
    float AgeYears,
    CharacterLifeStage LifeStage,
    float Hunger,
    float Fatigue,
    float Health,
    int Food,
    ActionKind Action,
    int ActionProgress,
    ulong WorkplaceBuilding,
    int WorkplaceSlot,
    int Farming,
    int Woodworking,
    int Stoneworking,
    int Crafting,
    ulong ParentA,
    ulong ParentB,
    int ChildCount,
    ulong Settlement,
    byte LodTier,
    bool PersistentIndividual);

using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Authoritative character. Composed of identity, body, needs, inventory and activity.
/// Family/skills/politics are extension points, not implemented here.
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
        Inventory.Food = CharacterRules.StartingFood;
        Family = FamilyId.None;
    }

    public CharacterId Id { get; }
    public string Name { get; }
    public ulong AppearanceSeed { get; }
    public double AgeYears { get; set; }
    public CharacterLifeStage LifeStage { get; set; }
    public LogicalGridCoordinate Position { get; set; }
    public CharacterNeeds Needs { get; } = new();
    public CharacterHealth Health { get; } = new();
    public CharacterInventory Inventory { get; } = new();
    public CharacterActivity Activity { get; } = new();
    public LogicalGridCoordinate WorkCell { get; set; }
    public FamilyId Family { get; set; }

    public bool IsAlive => LifeStage != CharacterLifeStage.Dead && Health.IsAlive;

    public CharacterSnapshot Snapshot() => new(
        Id,
        Position,
        (float)AgeYears,
        LifeStage,
        Needs.Hunger,
        Needs.Fatigue,
        Health.Current,
        Inventory.Food,
        Activity.Kind,
        Activity.ProgressTicks);
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
    int ActionProgress);

using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Deterministic priority selector. Starting rule, not final AI.
/// Hunger → food; fatigue → shelter; work if adult; else localized teaching.
/// </summary>
public sealed class CharacterDecisionSystem
{
    public CharacterDecisionSystem(
        GridNavigator navigator,
        ProductionSystem production,
        TeachingSystem teaching,
        SocialLifeSystem? social = null,
        LaborSystem? labor = null)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Teaching = teaching ?? throw new ArgumentNullException(nameof(teaching));
        Social = social;
        Labor = labor;
    }

    public GridNavigator Navigator { get; }
    public ProductionSystem Production { get; }
    public TeachingSystem Teaching { get; }
    public SocialLifeSystem? Social { get; set; }
    public LaborSystem? Labor { get; set; }

    public ActionKind ChooseKind(CharacterState character)
    {
        if (!character.IsAlive)
            return ActionKind.None;

        DropStaleWorkplace(character);

        if (SkillRules.IsDependent(character))
        {
            if (character.Needs.Hunger >= CharacterRules.HungerCritical && CanEatNow(character))
                return ActionKind.Eat;
            if (character.Needs.Hunger >= CharacterRules.HungerCritical && CaregiverHasFood(character))
                return ActionKind.Eat;
            return ActionKind.Idle;
        }

        if (character.Needs.Hunger >= CharacterRules.HungerCritical)
        {
            if (CanEatNow(character))
                return ActionKind.Eat;
            if (FindEdibleStorage() is not null)
                return ActionKind.Move;
            return WorkplaceKind(character);
        }

        if (character.Needs.Fatigue >= CharacterRules.FatigueCritical)
        {
            var shelter = FindPreferredShelter(character);
            if (shelter is null)
                return ActionKind.Idle;
            return character.Position.Equals(shelter.AccessCell) ? ActionKind.Sleep : ActionKind.Move;
        }

        if (character.Needs.Hunger >= 0.45f && CanEatNow(character))
            return ActionKind.Eat;
        if (character.Needs.Hunger >= 0.45f && FindEdibleStorage() is not null)
            return ActionKind.Move;

        if (character.IsPlayerCommanded)
            return ActionKind.Idle;

        if (Labor?.TryPlan(character, out var labor) == true)
            return labor.Kind;

        return WorkplaceKind(character);
    }

    public void AssignNext(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        DropStaleWorkplace(character);

        if (!character.Activity.NeedsDecision && !ShouldInterrupt(character))
            return;

        if (ShouldInterrupt(character))
        {
            if (character.Activity.Kind is ActionKind.Work)
                Production.AbandonWork(character);
            if (character.Activity.Kind is ActionKind.Teach or ActionKind.Learn)
                Teaching.Abandon(character);
        }

        var kind = ChooseKind(character);
        switch (kind)
        {
            case ActionKind.Eat:
                BeginEat(character);
                break;
            case ActionKind.Sleep:
                var shelter = FindPreferredShelter(character);
                character.Activity.Start(
                    ActionKind.Sleep,
                    CharacterRules.SleepDurationTicks,
                    shelter?.AccessCell ?? character.Position);
                break;
            case ActionKind.Work:
                BeginWork(character);
                break;
            case ActionKind.Gather:
            case ActionKind.Hunt:
            case ActionKind.Construct:
            case ActionKind.Haul:
            case ActionKind.Scout:
                BeginLabor(character);
                break;
            case ActionKind.Move:
                BeginMoveToNeed(character);
                break;
            case ActionKind.Idle:
                if (character.IsPlayerCommanded || !TryBeginTeachingOrMove(character))
                    character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
                break;
            default:
                character.Activity.Cancel();
                break;
        }
    }

    private bool TryBeginTeachingOrMove(CharacterState character)
    {
        var intent = Teaching.ConsiderAutonomous(character);
        if (intent is null)
            return false;

        if (!Teaching.Population.TryGet(intent.Value.Partner, out var partner))
            return false;

        if (intent.Value.Kind is ActionKind.Teach or ActionKind.Learn)
        {
            var teacher = intent.Value.Kind == ActionKind.Teach ? character : partner;
            var student = intent.Value.Kind == ActionKind.Teach ? partner : character;
            return Teaching.TryBegin(teacher, student, intent.Value.Skill, interrupt: false, out _);
        }

        BeginMove(character, intent.Value.Target, intent.Value.Partner, intent.Value.Skill);
        return true;
    }

    private bool ShouldInterrupt(CharacterState character)
    {
        var needed = ChooseKind(character);
        if (needed == character.Activity.Kind)
            return false;
        if (character.IsPlayerCommanded && character.Activity.Kind == ActionKind.Move)
            return needed is ActionKind.Eat or ActionKind.Sleep;
        return needed is ActionKind.Eat or ActionKind.Sleep;
    }

    private void BeginLabor(CharacterState character)
    {
        if (Labor?.TryPlan(character, out var plan) != true || plan.Kind is ActionKind.Move or ActionKind.Idle)
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        character.Activity.Start(
            plan.Kind,
            plan.DurationTicks,
            plan.Target ?? character.Position,
            jobResource: plan.Resource,
            jobBuilding: plan.Building);
    }

    private void BeginEat(CharacterState character)
    {
        if (!HasEdible(character.Inventory))
            TryTakeFood(character);
        if (HasEdible(character.Inventory))
        {
            character.Activity.Start(ActionKind.Eat, CharacterRules.EatDurationTicks);
            return;
        }

        BeginMoveToNeed(character);
    }

    private void BeginWork(CharacterState character)
    {
        var workplace = Production.FindFreeWorkplace(character);
        if (workplace is null)
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        Production.AssignWorkplace(character, workplace.Value);
        if (!Production.Buildings.TryGet(workplace.Value.Building, out var building))
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        if (!Production.TryEvaluate(building, character, out var evaluation))
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        Production.BeginWork(building, character);
        character.Activity.Start(ActionKind.Work, evaluation.DurationTicks, building.AccessCell);
    }

    private void BeginMoveToNeed(CharacterState character)
    {
        LogicalGridCoordinate? target = null;
        if (character.Needs.Hunger >= CharacterRules.HungerCritical
            || character.Needs.Hunger >= 0.45f)
        {
            if (!HasEdible(character.Inventory))
                target = FindEdibleStorage()?.AccessCell;
        }

        if (target is null && character.Needs.Fatigue >= CharacterRules.FatigueCritical)
            target = FindPreferredShelter(character)?.AccessCell;

        if (target is null && Labor?.TryPlan(character, out var labor) == true && labor.Target is { } jobTarget)
            target = jobTarget;

        if (target is null)
        {
            var workplace = Production.FindFreeWorkplace(character);
            if (workplace is not null)
            {
                Production.AssignWorkplace(character, workplace.Value);
                target = Production.AccessFor(workplace.Value);
            }
        }

        if (target is null)
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        BeginMove(character, target.Value);
    }

    private void BeginMove(
        CharacterState character,
        LogicalGridCoordinate target,
        CharacterId partner = default,
        SkillType? skill = null)
    {
        if (character.Position.Equals(target))
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks, target, partner, skill);
            return;
        }

        var path = Navigator.FindPath(character.Position, target);
        if (path is null || path.Count == 0)
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        character.Activity.Start(ActionKind.Move, path.Count, target, partner, skill);
        foreach (var step in path)
            character.Activity.RemainingPath.Enqueue(step);
    }

    private bool CanEatNow(CharacterState character)
    {
        if (HasEdible(character.Inventory))
            return true;
        var here = Production.BuildingAtAccess(character.Position);
        return here is { Definition.IsStorage: true } && HasEdible(here.Inventory);
    }

    private BuildingState? FindEdibleStorage()
    {
        foreach (var type in ResourceRules.Edible)
        {
            var storage = Production.FindStorageWith(type, 1);
            if (storage is not null)
                return storage;
        }

        return null;
    }

    private static bool HasEdible(Inventory inventory)
    {
        foreach (var type in ResourceRules.Edible)
        {
            if (inventory.Has(type, 1))
                return true;
        }

        return false;
    }

    private bool CaregiverHasFood(CharacterState character)
    {
        foreach (var caregiver in FamilyQueries.CaregiversOf(Teaching.Population, character))
        {
            if (!caregiver.IsAlive)
                continue;
            if (caregiver.Inventory.Has(ResourceType.Food, 1) || caregiver.Inventory.Has(ResourceType.WildBerries, 1) || HasEdible(caregiver.Inventory))
                return true;
        }

        return false;
    }

    private BuildingState? FindPreferredShelter(CharacterState character) =>
        Social?.HomeOf(character) ?? Production.FindShelter();

    private void TryTakeFood(CharacterState character)
    {
        var building = Production.BuildingAtAccess(character.Position);
        if (building is null || !building.Definition.IsStorage)
        {
            TryTakeFoodFromCaregiver(character);
            return;
        }
        foreach (var type in ResourceRules.Edible)
        {
            if (building.Inventory.TryTransferTo(character.Inventory, type, 1))
                return;
        }
    }

    private void TryTakeFoodFromCaregiver(CharacterState character)
    {
        foreach (var caregiver in FamilyQueries.CaregiversOf(Teaching.Population, character))
        {
            foreach (var type in ResourceRules.Edible)
            {
                if (caregiver.Inventory.TryTransferTo(character.Inventory, type, 1))
                    return;
            }
        }
    }

    private ActionKind WorkplaceKind(CharacterState character)
    {
        var workplace = Production.FindFreeWorkplace(character);
        if (workplace is null)
            return ActionKind.Idle;
        var access = Production.AccessFor(workplace.Value);
        if (access is not null && character.Position.Equals(access.Value))
            return ActionKind.Work;
        return ActionKind.Move;
    }

    private void DropStaleWorkplace(CharacterState character)
    {
        if (!character.AssignedWorkplace.IsAssigned)
            return;
        if (Production.Buildings.TryGet(character.AssignedWorkplace.Building, out _))
            return;
        character.AssignedWorkplace = WorkplaceId.None;
    }
}

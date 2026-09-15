using Cultures.Buildings;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Deterministic priority selector. Starting rule, not final AI.
/// Hunger → food; fatigue → shelter; otherwise workplace.
/// </summary>
public sealed class CharacterDecisionSystem
{
    public CharacterDecisionSystem(GridNavigator navigator, ProductionSystem production)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Production = production ?? throw new ArgumentNullException(nameof(production));
    }

    public GridNavigator Navigator { get; }
    public ProductionSystem Production { get; }

    public ActionKind ChooseKind(CharacterState character)
    {
        if (!character.IsAlive)
            return ActionKind.None;

        DropStaleWorkplace(character);

        if (character.Needs.Hunger >= CharacterRules.HungerCritical)
        {
            if (CanEatNow(character))
                return ActionKind.Eat;
            if (Production.FindStorageWith(ResourceType.Food, 1) is not null)
                return ActionKind.Move;
            if (Production.FindFreeWorkplace(character) is not null)
                return AtAssignedWorkplace(character) ? ActionKind.Work : ActionKind.Move;
            return ActionKind.Idle;
        }

        if (character.Needs.Fatigue >= CharacterRules.FatigueCritical)
        {
            var shelter = Production.FindShelter();
            if (shelter is null)
                return ActionKind.Idle;
            return character.Position.Equals(shelter.AccessCell) ? ActionKind.Sleep : ActionKind.Move;
        }

        if (character.Needs.Hunger >= 0.45f && CanEatNow(character))
            return ActionKind.Eat;
        if (character.Needs.Hunger >= 0.45f && Production.FindStorageWith(ResourceType.Food, 1) is not null)
            return ActionKind.Move;

        if (Production.FindFreeWorkplace(character) is not null)
            return AtAssignedWorkplace(character) ? ActionKind.Work : ActionKind.Move;

        return ActionKind.Idle;
    }

    public void AssignNext(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        DropStaleWorkplace(character);

        if (!character.Activity.NeedsDecision && !ShouldInterrupt(character))
            return;

        if (ShouldInterrupt(character) && character.Activity.Kind == ActionKind.Work)
            Production.AbandonWork(character);

        var kind = ChooseKind(character);
        switch (kind)
        {
            case ActionKind.Eat:
                BeginEat(character);
                break;
            case ActionKind.Sleep:
                var shelter = Production.FindShelter();
                character.Activity.Start(
                    ActionKind.Sleep,
                    CharacterRules.SleepDurationTicks,
                    shelter?.AccessCell ?? character.Position);
                break;
            case ActionKind.Work:
                BeginWork(character);
                break;
            case ActionKind.Move:
                BeginMoveToNeed(character);
                break;
            case ActionKind.Idle:
                character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
                break;
            default:
                character.Activity.Cancel();
                break;
        }
    }

    private bool ShouldInterrupt(CharacterState character)
    {
        var needed = ChooseKind(character);
        if (needed == character.Activity.Kind)
            return false;
        return needed is ActionKind.Eat or ActionKind.Sleep;
    }

    private void BeginEat(CharacterState character)
    {
        if (!character.Inventory.Has(ResourceType.Food, 1))
            TryTakeFood(character);
        if (character.Inventory.Has(ResourceType.Food, 1))
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
            if (!character.Inventory.Has(ResourceType.Food, 1))
                target = Production.FindStorageWith(ResourceType.Food, 1)?.AccessCell;
        }

        if (target is null && character.Needs.Fatigue >= CharacterRules.FatigueCritical)
            target = Production.FindShelter()?.AccessCell;

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

    private void BeginMove(CharacterState character, LogicalGridCoordinate target)
    {
        if (character.Position.Equals(target))
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        var path = Navigator.FindPath(character.Position, target);
        if (path is null || path.Count == 0)
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks);
            return;
        }

        character.Activity.Start(ActionKind.Move, path.Count, target);
        foreach (var step in path)
            character.Activity.RemainingPath.Enqueue(step);
    }

    private bool CanEatNow(CharacterState character)
    {
        if (character.Inventory.Has(ResourceType.Food, 1))
            return true;
        var here = Production.BuildingAtAccess(character.Position);
        return here is { Definition.IsStorage: true } && here.Inventory.Has(ResourceType.Food, 1);
    }

    private void TryTakeFood(CharacterState character)
    {
        var building = Production.BuildingAtAccess(character.Position);
        if (building is null || !building.Definition.IsStorage)
            return;
        building.Inventory.TryTransferTo(character.Inventory, ResourceType.Food, 1);
    }

    private bool AtAssignedWorkplace(CharacterState character)
    {
        if (!character.AssignedWorkplace.IsAssigned)
            return false;
        var access = Production.AccessFor(character.AssignedWorkplace);
        return access is not null && character.Position.Equals(access.Value);
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

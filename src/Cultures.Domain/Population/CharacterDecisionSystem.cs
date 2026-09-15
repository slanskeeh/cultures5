using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Deterministic priority selector. Starting rule, not final AI.
/// </summary>
public sealed class CharacterDecisionSystem
{
    public CharacterDecisionSystem(GridNavigator navigator)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public GridNavigator Navigator { get; }

    public ActionKind ChooseKind(CharacterState character)
    {
        if (!character.IsAlive)
            return ActionKind.None;

        if (character.Needs.Hunger >= CharacterRules.HungerCritical && character.Inventory.Food > 0)
            return ActionKind.Eat;
        if (character.Needs.Hunger >= CharacterRules.HungerCritical)
            return character.Position.Equals(character.WorkCell) ? ActionKind.Work : ActionKind.Move;
        if (character.Needs.Fatigue >= CharacterRules.FatigueCritical)
            return ActionKind.Sleep;
        if (character.Needs.Hunger >= 0.45f && character.Inventory.Food > 0)
            return ActionKind.Eat;
        if (character.Inventory.Food < 2)
            return character.Position.Equals(character.WorkCell) ? ActionKind.Work : ActionKind.Move;
        return ActionKind.Idle;
    }

    public void AssignNext(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        if (!character.Activity.NeedsDecision && !ShouldInterrupt(character))
            return;

        var kind = ChooseKind(character);
        switch (kind)
        {
            case ActionKind.Eat:
                character.Activity.Start(ActionKind.Eat, CharacterRules.EatDurationTicks);
                break;
            case ActionKind.Sleep:
                character.Activity.Start(ActionKind.Sleep, CharacterRules.SleepDurationTicks, character.Position);
                break;
            case ActionKind.Work:
                character.Activity.Start(ActionKind.Work, CharacterRules.WorkDurationTicks, character.WorkCell);
                break;
            case ActionKind.Move:
                BeginMove(character, character.WorkCell);
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
        if (needed is ActionKind.Eat or ActionKind.Sleep)
            return true;
        return false;
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
}

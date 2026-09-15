using Cultures.World;

namespace Cultures.Population;

public sealed class CharacterActionSystem
{
    public CharacterActionSystem(GridNavigator navigator)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public GridNavigator Navigator { get; }

    public void Advance(CharacterState character)
    {
        if (!character.IsAlive || character.Activity.Kind == ActionKind.None)
            return;

        switch (character.Activity.Kind)
        {
            case ActionKind.Move:
                AdvanceMove(character);
                break;
            case ActionKind.Sleep:
                character.Needs.Fatigue -= 1f / CharacterRules.SleepDurationTicks;
                character.Needs.Clamp();
                character.Activity.Advance();
                break;
            default:
                character.Activity.Advance();
                break;
        }

        if (character.Activity.IsComplete)
            Complete(character);
    }

    private void AdvanceMove(CharacterState character)
    {
        if (character.Activity.RemainingPath.Count == 0)
        {
            character.Activity.Cancel();
            return;
        }

        var next = character.Activity.RemainingPath.Peek();
        if (!Navigator.IsPassable(next))
        {
            character.Activity.Cancel();
            return;
        }

        character.Activity.RemainingPath.Dequeue();
        character.Position = next;
        character.Activity.Advance();
    }

    private static void Complete(CharacterState character)
    {
        switch (character.Activity.Kind)
        {
            case ActionKind.Eat:
                if (character.Inventory.Food > 0)
                {
                    character.Inventory.Food--;
                    character.Needs.Hunger -= CharacterRules.EatHungerRestore;
                    character.Needs.Clamp();
                }
                break;
            case ActionKind.Work:
                if (character.Inventory.Food < CharacterRules.MaxFood)
                    character.Inventory.Food++;
                break;
        }

        character.Activity.Cancel();
    }
}

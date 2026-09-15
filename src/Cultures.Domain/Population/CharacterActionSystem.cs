using Cultures.Buildings;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

public sealed class CharacterActionSystem
{
    public CharacterActionSystem(GridNavigator navigator, ProductionSystem production)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Production = production ?? throw new ArgumentNullException(nameof(production));
    }

    public GridNavigator Navigator { get; }
    public ProductionSystem Production { get; }

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
            case ActionKind.Work:
                AdvanceWork(character);
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
        if (character.Activity.IsComplete)
            TryTakeFood(character);
    }

    private void AdvanceWork(CharacterState character)
    {
        if (character.AssignedWorkplace.IsAssigned
            && Production.Buildings.TryGet(character.AssignedWorkplace.Building, out var building))
        {
            Production.AdvanceWork(building);
        }

        character.Activity.Advance();
    }

    private void Complete(CharacterState character)
    {
        switch (character.Activity.Kind)
        {
            case ActionKind.Eat:
                if (character.Inventory.TryRemove(ResourceType.Food, 1))
                {
                    character.Needs.Hunger -= CharacterRules.EatHungerRestore;
                    character.Needs.Clamp();
                }
                break;
            case ActionKind.Work:
                Production.TryCompleteWork(character);
                break;
        }

        character.Activity.Cancel();
    }

    private void TryTakeFood(CharacterState character)
    {
        var building = Production.BuildingAtAccess(character.Position);
        if (building is null || !building.Definition.IsStorage)
            return;
        if (character.Inventory.Has(ResourceType.Food, 1))
            return;
        building.Inventory.TryTransferTo(character.Inventory, ResourceType.Food, 1);
    }
}

using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

public sealed class CharacterActionSystem
{
    public CharacterActionSystem(
        GridNavigator navigator,
        ProductionSystem production,
        TeachingSystem teaching,
        LaborSystem? labor = null)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Teaching = teaching ?? throw new ArgumentNullException(nameof(teaching));
        Labor = labor;
    }

    public GridNavigator Navigator { get; }
    public ProductionSystem Production { get; }
    public TeachingSystem Teaching { get; }
    public LaborSystem? Labor { get; set; }

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

        if (!character.Activity.IsComplete)
            return;

        var partner = character.Activity.PartnerId;
        var skill = character.Activity.Skill;
        var kind = character.Activity.Kind;
        Complete(character);
        if (kind == ActionKind.Move && skill is { } teachSkill && partner.IsAssigned)
            TryBeginAfterMove(character, partner, teachSkill);
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
                foreach (var type in ResourceRules.Edible)
                {
                    if (!character.Inventory.TryRemove(type, 1))
                        continue;
                    character.Needs.Hunger -= CharacterRules.EatHungerRestore;
                    character.Needs.Clamp();
                    break;
                }
                break;
            case ActionKind.Work:
                Production.TryCompleteWork(character);
                break;
            case ActionKind.Teach:
                Teaching.Complete(character);
                break;
            case ActionKind.Gather:
            case ActionKind.Hunt:
            case ActionKind.Construct:
            case ActionKind.Haul:
            case ActionKind.Scout:
                Labor?.Complete(character);
                break;
        }

        character.Activity.Cancel();
    }

    private void TryBeginAfterMove(CharacterState character, CharacterId partnerId, SkillType skill)
    {
        if (!Teaching.Population.TryGet(partnerId, out var partner))
            return;
        if (Teaching.TryBegin(character, partner, skill, interrupt: true, out _))
            return;
        Teaching.TryBegin(partner, character, skill, interrupt: true, out _);
    }

    private void TryTakeFood(CharacterState character)
    {
        var building = Production.BuildingAtAccess(character.Position);
        if (building is null || !building.Definition.IsStorage)
            return;
        if (HasEdible(character.Inventory))
            return;
        foreach (var type in ResourceRules.Edible)
        {
            if (building.Inventory.TryTransferTo(character.Inventory, type, 1))
                return;
        }
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
}

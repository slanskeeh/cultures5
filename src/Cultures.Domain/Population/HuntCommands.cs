using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Economy;
using Cultures.World;

namespace Cultures.Population;

public sealed record HuntWildlifeCommand(CharacterId Hunter) : ICommand;

public sealed class HuntWildlifeHandler : ICommandHandler<HuntWildlifeCommand>
{
    public HuntWildlifeHandler(
        PopulationRoster population,
        NaturalResourceSystem ecology,
        EventBus events,
        SimulationClock clock,
        int foodYield)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Ecology = ecology ?? throw new ArgumentNullException(nameof(ecology));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        FoodYield = Math.Max(1, foodYield);
    }

    public PopulationRoster Population { get; }
    public NaturalResourceSystem Ecology { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public int FoodYield { get; }

    public CommandResult Handle(HuntWildlifeCommand command)
    {
        if (!Population.TryGet(command.Hunter, out var character) || !character.IsAlive)
            return CommandResult.Fail("Hunter is invalid.");
        if (character.LifeStage < CharacterLifeStage.Adult)
            return CommandResult.Fail("Character is too young to hunt.");
        if (character.Profession.IsAssigned && character.Profession != ProfessionId.Hunter)
            return CommandResult.Fail("Profession cannot hunt.");
        if (!Ecology.TryHunt(character.Position, out var species, out var error))
            return CommandResult.Fail(error);

        if (!character.Inventory.TryAdd(ResourceType.Food, FoodYield))
            return CommandResult.Fail("Inventory cannot hold the catch.");

        Events.Publish(new WildlifeHuntedEvent(Clock.Tick, character.Id.Value, species, FoodYield));
        return CommandResult.Ok();
    }
}

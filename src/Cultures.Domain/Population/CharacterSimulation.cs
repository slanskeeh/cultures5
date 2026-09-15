using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Sequences character systems for one simulation tick. Does not own world generation or rendering.
/// </summary>
public sealed class CharacterSimulation
{
    public CharacterSimulation(
        LogicalWorld world,
        PopulationRoster population,
        SimulationClock clock,
        EventBus events,
        ProductionSystem production)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Navigator = new GridNavigator(world);
        Aging = new CharacterAgingSystem(clock.Calendar);
        Needs = new CharacterNeedsSystem(clock.Calendar);
        Survival = new CharacterSurvivalSystem(clock.Calendar);
        Actions = new CharacterActionSystem(Navigator, production);
        Decisions = new CharacterDecisionSystem(Navigator, production);
    }

    public LogicalWorld World { get; }
    public PopulationRoster Population { get; }
    public SimulationClock Clock { get; }
    public EventBus Events { get; }
    public ProductionSystem Production { get; }
    public GridNavigator Navigator { get; }
    public CharacterAgingSystem Aging { get; }
    public CharacterNeedsSystem Needs { get; }
    public CharacterSurvivalSystem Survival { get; }
    public CharacterActionSystem Actions { get; }
    public CharacterDecisionSystem Decisions { get; }

    public void Tick()
    {
        foreach (var character in Population.All)
        {
            if (!character.IsAlive)
                continue;

            Needs.ApplyTick(character);
            Aging.ApplyTick(character);
            Survival.ApplyTick(character);
            if (!character.IsAlive)
            {
                Events.Publish(new CharacterDiedEvent(Clock.Tick, character.Id, character.Position));
                Production.ReleaseWorkplace(character);
                continue;
            }

            Actions.Advance(character);
            Decisions.AssignNext(character);
        }
    }
}

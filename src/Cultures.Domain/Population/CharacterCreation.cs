using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Population;

public sealed class CharacterCreation
{
    public CharacterCreation(
        PopulationRoster population,
        EntityIdFactory ids,
        LogicalWorld world,
        EventBus events,
        SimulationClock clock)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public PopulationRoster Population { get; }
    public EntityIdFactory Ids { get; }
    public LogicalWorld World { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public bool TryCreateChild(
        CharacterId parentA,
        CharacterId? parentB,
        out CharacterState? child,
        out string error)
    {
        child = null;
        error = "Birth failed.";
        if (Population.Count >= SkillRules.MaxPopulation)
        {
            error = "Population cap reached.";
            return false;
        }

        if (!Population.TryGet(parentA, out var first) || !first.IsAlive)
        {
            error = "First parent is invalid.";
            return false;
        }

        if (!SkillRules.CanTeach(first))
        {
            error = "First parent is too young.";
            return false;
        }

        CharacterState? second = null;
        if (parentB is { IsAssigned: true } other)
        {
            if (other == parentA)
            {
                error = "A character cannot be their own co-parent.";
                return false;
            }

            if (!Population.TryGet(other, out second) || !second.IsAlive || !SkillRules.CanTeach(second))
            {
                error = "Second parent is invalid.";
                return false;
            }
        }

        if (first.FamilyLinks.Children.Count >= SkillRules.MaxChildrenPerParent
            || (second?.FamilyLinks.Children.Count ?? 0) >= SkillRules.MaxChildrenPerParent)
        {
            error = "A parent has reached the child limit.";
            return false;
        }

        var id = Ids.NextCharacter();
        var position = first.Position;
        var navigator = new GridNavigator(World);
        foreach (var neighbor in navigator.PassableNeighbors(first.Position))
        {
            position = neighbor;
            break;
        }

        var appearance = first.AppearanceSeed ^ id.Value * 0x9E3779B97F4A7C15UL;
        child = new CharacterState(id, position, appearance)
        {
            AgeYears = SkillRules.NewbornAgeYears,
            LifeStage = CharacterAgingSystem.StageFor(SkillRules.NewbornAgeYears),
            Name = $"Child {id.Value}",
            Culture = first.Culture
        };

        if (!child.FamilyLinks.TryAddParent(first.Id) || !first.FamilyLinks.TryAddChild(id))
        {
            error = "Could not link first parent.";
            child = null;
            return false;
        }

        if (second is not null
            && (!child.FamilyLinks.TryAddParent(second.Id) || !second.FamilyLinks.TryAddChild(id)))
        {
            error = "Could not link second parent.";
            child = null;
            return false;
        }

        var parents = new List<CharacterState> { first };
        if (second is not null)
            parents.Add(second);
        child.Skills.InheritFrom(parents);
        if (first.Household.IsAssigned)
            child.Household = first.Household;
        Population.Add(child);
        Events.Publish(new ChildBornEvent(Clock.Tick, child.Id, first.Id, second?.Id ?? CharacterId.None));
        return true;
    }
}

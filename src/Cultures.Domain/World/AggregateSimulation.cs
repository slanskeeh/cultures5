using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.World;

/// <summary>
/// Applies demographic and resource change to aggregate/macro chunks without per-character AI.
/// Does not invent detailed personal history.
/// </summary>
public sealed class AggregateSimulation
{
    public AggregateSimulation(
        LogicalWorld world,
        PopulationRoster population,
        BuildingDirectory buildings,
        RecipeCatalog recipes,
        EventBus events,
        SimulationClock clock,
        CharacterCreation? creation = null)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Creation = creation;
    }

    public LogicalWorld World { get; }
    public PopulationRoster Population { get; }
    public BuildingDirectory Buildings { get; }
    public RecipeCatalog Recipes { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }
    public CharacterCreation? Creation { get; }

    public void Advance(SimulationLodTier tier, ulong ticks)
    {
        if (ticks == 0)
            return;

        var calendar = Clock.Calendar;
        var people = Population.Alive
            .Where(c => c.LodTier == tier && !c.IsProtectedFromAggregation)
            .OrderBy(c => c.Id.Value)
            .ToList();
        if (people.Count == 0)
            return;

        var byChunk = people.GroupBy(c => World.Chunks.ToAddress(c.Position).Chunk)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (chunk, members) in byChunk.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
        {
            Produce(chunk, ticks);
            foreach (var character in members)
                ApplyBody(character, ticks, calendar);

            ConsumeAndEat(chunk, members);
            var deaths = ResolveDeaths(members);
            if (deaths > 0)
                Events.Publish(new AggregateDeathsOccurredEvent(Clock.Tick, chunk, deaths));

            if (tier == SimulationLodTier.Macro)
                ConsiderBirth(chunk, members);

            var remaining = members.Count(c => c.IsAlive);
            var food = FoodInChunk(chunk);
            if (remaining > 0 && food <= 0)
                Events.Publish(new AggregateFoodShortageEvent(Clock.Tick, chunk, remaining, food));
        }
    }

    private void ApplyBody(CharacterState character, ulong ticks, SimulationCalendar calendar)
    {
        var days = ticks / (double)calendar.TicksPerDay;
        character.AgeYears += ticks / (double)calendar.TicksPerYear;
        character.LifeStage = CharacterAgingSystem.StageFor(character.AgeYears);
        character.Needs.Hunger += CharacterRules.HungerPerDay * (float)days;
        character.Needs.Fatigue += CharacterRules.FatiguePerDay * (float)days;
        character.Needs.Clamp();
        if (character.Needs.Hunger >= 1f)
            character.Health.Current -= CharacterRules.StarvationDamagePerDay * (float)days;
        if (character.AgeYears >= CharacterRules.MaxLifespanYears)
            character.Health.Current = 0f;
        if (character.Health.Current <= 0f)
        {
            character.Health.Current = 0f;
            character.LifeStage = CharacterLifeStage.Dead;
            character.Activity.Cancel();
        }
    }

    private void Produce(ChunkCoordinate chunk, ulong ticks)
    {
        foreach (var building in BuildingsIn(chunk))
        {
            if (building.Definition.Recipe is not { } recipeId || !Recipes.TryGet(recipeId, out var recipe))
                continue;
            var cycles = (int)(ticks / (ulong)recipe.DurationTicks);
            if (cycles <= 0)
                continue;
            foreach (var output in recipe.Outputs)
            {
                if (output.Quantity <= 0)
                    continue;
                building.Inventory.TryAdd(output.Type, output.Quantity * cycles);
            }
        }
    }

    private void ConsumeAndEat(ChunkCoordinate chunk, List<CharacterState> members)
    {
        foreach (var character in members.Where(c => c.IsAlive).OrderBy(c => c.Id.Value))
        {
            if (character.Needs.Hunger < CharacterRules.HungerCritical)
                continue;
            if (character.Inventory.Has(ResourceType.Food, 1))
            {
                character.Inventory.TryRemove(ResourceType.Food, 1);
                character.Needs.Hunger = Math.Max(0f, character.Needs.Hunger - CharacterRules.EatHungerRestore);
                continue;
            }

            var storage = BuildingsIn(chunk).FirstOrDefault(b => b.Definition.IsStorage && b.Inventory.Has(ResourceType.Food, 1));
            if (storage is null)
                continue;
            storage.Inventory.TryTransferTo(character.Inventory, ResourceType.Food, 1);
            if (!character.Inventory.Has(ResourceType.Food, 1))
                continue;
            character.Inventory.TryRemove(ResourceType.Food, 1);
            character.Needs.Hunger = Math.Max(0f, character.Needs.Hunger - CharacterRules.EatHungerRestore);
        }
    }

    private int ResolveDeaths(List<CharacterState> members)
    {
        var deaths = 0;
        foreach (var character in members)
        {
            if (character.IsAlive)
                continue;
            deaths++;
            character.Activity.Cancel();
        }

        return deaths;
    }

    private void ConsiderBirth(ChunkCoordinate chunk, List<CharacterState> members)
    {
        if (Creation is null || Population.Count >= SkillRules.MaxPopulation)
            return;
        var adults = members.Where(c => c.IsAlive && SkillRules.CanTeach(c)).OrderBy(c => c.Id.Value).ToList();
        if (adults.Count < 2)
            return;
        if (FoodInChunk(chunk) < members.Count(c => c.IsAlive) * 2)
            return;
        if (!Creation.TryCreateChild(adults[0].Id, adults[1].Id, out var child, out _))
            return;
        if (child is null)
            return;
        child.Position = members[0].Position;
        child.LodTier = SimulationLodTier.Macro;
        Events.Publish(new AggregateBirthsOccurredEvent(Clock.Tick, chunk, 1));
    }

    private int FoodInChunk(ChunkCoordinate chunk)
    {
        var food = 0;
        foreach (var character in Population.Alive)
        {
            if (!World.Chunks.ToAddress(character.Position).Chunk.Equals(chunk))
                continue;
            food += character.Inventory.GetQuantity(ResourceType.Food);
        }

        foreach (var building in BuildingsIn(chunk))
            food += building.Inventory.GetQuantity(ResourceType.Food);
        return food;
    }

    private IEnumerable<BuildingState> BuildingsIn(ChunkCoordinate chunk) =>
        Buildings.All.Where(b => b.IsActive && World.Chunks.ToAddress(b.Origin).Chunk.Equals(chunk));
}

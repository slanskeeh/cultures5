using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Time;
using Cultures.Economy;
using Cultures.Population;

namespace Cultures.World;

/// <summary>
/// Classifies chunks, transitions characters between detailed and aggregate simulation,
/// and schedules coarser update cadences. Does not own character/production/settlement rules.
/// </summary>
public sealed class LodSystem : ILodPolicy
{
    private int _ticksSinceClassification;
    private int _reducedBehaviorTicks;
    private ulong _ticksSinceAggregate;
    private ulong _ticksSinceMacro;

    public LodSystem(
        LogicalWorld world,
        PopulationRoster population,
        BuildingDirectory buildings,
        SimulationClock clock,
        EventBus events,
        Func<LogicalGridCoordinate> focus,
        AggregateSimulation aggregate,
        ProductionSystem production,
        TeachingSystem teaching)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Focus = focus ?? throw new ArgumentNullException(nameof(focus));
        Aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Teaching = teaching ?? throw new ArgumentNullException(nameof(teaching));
        Chunks = new ChunkSimulationDirectory();
        Groups = new List<MigrationGroup>();
    }

    public LogicalWorld World { get; }
    public PopulationRoster Population { get; }
    public BuildingDirectory Buildings { get; }
    public SimulationClock Clock { get; }
    public EventBus Events { get; }
    public Func<LogicalGridCoordinate> Focus { get; }
    public AggregateSimulation Aggregate { get; }
    public ProductionSystem Production { get; }
    public TeachingSystem Teaching { get; }
    public ChunkSimulationDirectory Chunks { get; }
    public List<MigrationGroup> Groups { get; }

    public bool SimulateBody(CharacterState character) =>
        character.IsAlive && character.IsIndividuallySimulated;

    public bool SimulateBehavior(CharacterState character)
    {
        if (!SimulateBody(character))
            return false;
        if (character.LodTier == SimulationLodTier.Full || character.IsProtectedFromAggregation)
            return true;
        return _reducedBehaviorTicks == 0;
    }

    public void Tick()
    {
        _ticksSinceClassification++;
        _reducedBehaviorTicks = (_reducedBehaviorTicks + 1) % LodRules.ReducedBehaviorIntervalTicks;
        _ticksSinceAggregate++;
        _ticksSinceMacro++;

        if (_ticksSinceClassification >= LodRules.ClassificationIntervalTicks)
        {
            _ticksSinceClassification = 0;
            Evaluate();
        }

        var aggregateEvery = LodRules.AggregateIntervalTicks(Clock.Calendar);
        if (_ticksSinceAggregate >= aggregateEvery)
        {
            _ticksSinceAggregate = 0;
            Aggregate.Advance(SimulationLodTier.Aggregate, aggregateEvery);
            RefreshCensus();
        }

        var macroEvery = LodRules.MacroIntervalTicks(Clock.Calendar);
        if (_ticksSinceMacro >= macroEvery)
        {
            _ticksSinceMacro = 0;
            Aggregate.Advance(SimulationLodTier.Macro, macroEvery);
            RefreshCensus();
        }
    }

    public void Evaluate()
    {
        _ticksSinceClassification = 0;
        var foci = CollectFoci().Distinct().ToArray();
        foreach (var character in Population.Alive)
            Chunks.GetOrCreate(World.Chunks.ToAddress(character.Position).Chunk);
        foreach (var building in Buildings.All)
            Chunks.GetOrCreate(World.Chunks.ToAddress(building.Origin).Chunk);
        foreach (var focus in foci)
            Chunks.GetOrCreate(focus);

        foreach (var state in Chunks.All)
        {
            var classified = SimulationLodClassifier.ClassifyNearest(state.Coordinate, foci, World.Configuration);
            var previous = state.EffectiveTier;
            state.SimulationTier = classified;
            if (previous != state.EffectiveTier)
                Events.Publish(new ChunkLodChangedEvent(Clock.Tick, state.Coordinate, previous, state.EffectiveTier));
        }

        var aggregated = new Dictionary<(int X, int Y), int>();
        var reconstructed = new Dictionary<(int X, int Y), int>();
        foreach (var character in Population.Alive.OrderBy(c => c.Id.Value))
        {
            var chunk = World.Chunks.ToAddress(character.Position).Chunk;
            var state = Chunks.GetOrCreate(chunk);
            var next = character.IsProtectedFromAggregation ? SimulationLodTier.Full : state.EffectiveTier;
            ApplyCharacterTransition(character, chunk, next, aggregated, reconstructed);
            character.LodTier = next;
        }

        foreach (var pair in aggregated.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
        {
            Events.Publish(new CharactersAggregatedEvent(
                Clock.Tick,
                new ChunkCoordinate(pair.Key.X, pair.Key.Y),
                pair.Value));
        }

        foreach (var pair in reconstructed.OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X))
        {
            Events.Publish(new CharactersReconstructedEvent(
                Clock.Tick,
                new ChunkCoordinate(pair.Key.X, pair.Key.Y),
                pair.Value));
        }

        RefreshCensus();
    }

    public SimulationLodTier Classify(LogicalGridCoordinate cell)
    {
        var chunk = World.Chunks.ToAddress(cell).Chunk;
        if (Chunks.TryGet(chunk, out var state) && state.ForcedTier is { } forced)
            return forced;
        return SimulationLodClassifier.ClassifyNearest(chunk, CollectFoci(), World.Configuration);
    }

    public SimulationLodTier Classify(ChunkCoordinate chunk)
    {
        if (Chunks.TryGet(chunk, out var state) && state.ForcedTier is { } forced)
            return forced;
        return SimulationLodClassifier.ClassifyNearest(chunk, CollectFoci(), World.Configuration);
    }

    public void ForceTier(ChunkCoordinate chunk, SimulationLodTier tier)
    {
        var state = Chunks.GetOrCreate(chunk);
        state.ForcedTier = tier;
        Evaluate();
    }

    public void ClearForce(ChunkCoordinate chunk)
    {
        if (Chunks.TryGet(chunk, out var state))
            state.ForcedTier = null;
        Evaluate();
    }

    public void SetPresentation(ChunkCoordinate chunk, ChunkPresentationPresence presence)
    {
        Chunks.GetOrCreate(chunk).Presentation = presence;
    }

    public WorldResourceTotals ResourceTotals()
    {
        var food = 0;
        var wood = 0;
        var stone = 0;
        foreach (var character in Population.All)
        {
            food += character.Inventory.GetQuantity(ResourceType.Food);
            wood += character.Inventory.GetQuantity(ResourceType.Wood);
            stone += character.Inventory.GetQuantity(ResourceType.Stone);
        }

        foreach (var building in Buildings.All)
        {
            food += building.Inventory.GetQuantity(ResourceType.Food);
            wood += building.Inventory.GetQuantity(ResourceType.Wood);
            stone += building.Inventory.GetQuantity(ResourceType.Stone);
        }

        return new WorldResourceTotals(food, wood, stone);
    }

    public void RefreshCensus()
    {
        foreach (var state in Chunks.All)
            state.Census.Clear();

        foreach (var character in Population.Alive.OrderBy(c => c.Id.Value))
        {
            var chunk = World.Chunks.ToAddress(character.Position).Chunk;
            var census = Chunks.GetOrCreate(chunk).Census;
            census.Population++;
            switch (character.LifeStage)
            {
                case CharacterLifeStage.Infant:
                    census.Infants++;
                    break;
                case CharacterLifeStage.Child:
                    census.Children++;
                    break;
                case CharacterLifeStage.Adolescent:
                    census.Adolescents++;
                    break;
                case CharacterLifeStage.Adult:
                    census.Adults++;
                    break;
                case CharacterLifeStage.Elder:
                    census.Elders++;
                    break;
            }

            census.Food += character.Inventory.GetQuantity(ResourceType.Food);
            census.Wood += character.Inventory.GetQuantity(ResourceType.Wood);
            census.Stone += character.Inventory.GetQuantity(ResourceType.Stone);
            census.FarmingSkillSum += character.Skills.GetLevel(SkillType.Farming);
            census.WoodworkingSkillSum += character.Skills.GetLevel(SkillType.Woodworking);
            census.StoneworkingSkillSum += character.Skills.GetLevel(SkillType.Stoneworking);
            census.CraftingSkillSum += character.Skills.GetLevel(SkillType.Crafting);
            if (character.Settlement.IsAssigned)
                census.Settlement = character.Settlement;
        }

        foreach (var building in Buildings.All.OrderBy(b => b.Id.Value))
        {
            var census = Chunks.GetOrCreate(World.Chunks.ToAddress(building.Origin).Chunk).Census;
            census.Buildings++;
            if (building.Definition.IsShelter)
                census.Shelters++;
            census.Food += building.Inventory.GetQuantity(ResourceType.Food);
            census.Wood += building.Inventory.GetQuantity(ResourceType.Wood);
            census.Stone += building.Inventory.GetQuantity(ResourceType.Stone);
            if (building.AssociatedSettlement.IsAssigned)
                census.Settlement = building.AssociatedSettlement;
        }

        foreach (var state in Chunks.All)
        {
            var unemployed = Math.Max(0, state.Census.Adults + state.Census.Elders - state.Census.Buildings);
            var housingGap = Math.Max(0, state.Census.Population - state.Census.Shelters);
            var previous = state.Census.MigrationPressure;
            state.Census.MigrationPressure = unemployed + housingGap;
            if (previous != state.Census.MigrationPressure)
            {
                Events.Publish(new MigrationPressureChangedEvent(
                    Clock.Tick,
                    state.Coordinate,
                    state.Census.MigrationPressure));
            }

            state.LastClassifiedTick = Clock.Tick;
        }
    }

    private void ApplyCharacterTransition(
        CharacterState character,
        ChunkCoordinate chunk,
        SimulationLodTier next,
        Dictionary<(int X, int Y), int> aggregated,
        Dictionary<(int X, int Y), int> reconstructed)
    {
        var previous = character.LodTier;
        if (previous.IsDetailed() && next.IsAggregate())
        {
            Production.ReleaseWorkplace(character);
            Teaching.Abandon(character);
            character.Activity.Cancel();
            var key = (chunk.X, chunk.Y);
            aggregated[key] = aggregated.GetValueOrDefault(key) + 1;
        }
        else if (previous.IsAggregate() && next.IsDetailed())
        {
            character.Activity.Cancel();
            var key = (chunk.X, chunk.Y);
            reconstructed[key] = reconstructed.GetValueOrDefault(key) + 1;
        }
    }

    private IEnumerable<ChunkCoordinate> CollectFoci()
    {
        yield return World.Chunks.ToAddress(Focus()).Chunk;
        foreach (var character in Population.Alive.OrderBy(c => c.Id.Value))
        {
            if (!character.IsProtectedFromAggregation)
                continue;
            yield return World.Chunks.ToAddress(character.Position).Chunk;
        }
    }
}

public readonly record struct WorldResourceTotals(int Food, int Wood, int Stone);

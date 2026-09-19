using Cultures.Application.Persistence;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Randomness;
using Cultures.Core.Time;
using Cultures.Exploration;
using Cultures.Military;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;
using Cultures.World.Commands;

namespace Cultures.Application;

/// <summary>
/// Headless simulation host. Can advance time without a Godot scene.
/// </summary>
public sealed class SimulationHost
{
    public SimulationHost(
        ulong worldSeed,
        SimulationCalendar? calendar = null,
        ulong initialTick = 0,
        WorldConfiguration? world = null,
        int populationCount = CharacterRules.DefaultPopulation,
        bool placeDevelopmentBuildings = true)
    {
        WorldSeed = worldSeed;
        Clock = new SimulationClock(calendar, initialTick);
        Random = new SeededRandom(worldSeed);
        Events = new EventBus();
        Commands = new CommandProcessor();
        Ids = new EntityIdFactory();
        World = new LogicalWorld(world ?? WorldConfiguration.DebugSample, worldSeed);
        Cursor = new SimulationCursor(
            World,
            Events,
            () => Clock.Tick,
            new LogicalGridCoordinate(0, World.Configuration.Height / 2));
        Buildings = new BuildingDirectory();
        Catalog = BuildingCatalog.Development;
        Recipes = RecipeCatalog.Development;
        Placement = new BuildingPlacementSystem(World, Buildings, Catalog, Ids);
        Production = new ProductionSystem(
            World,
            Buildings,
            new ProductionResolver(Recipes, new NeutralEnvironmentProductionModifier()),
            Events,
            Clock);

        if (placeDevelopmentBuildings)
            DevelopmentSiteBootstrap.Place(Placement, World);

        Population = PopulationSpawner.Spawn(World, Ids, worldSeed, populationCount);
        Teaching = new TeachingSystem(Population, World, Events, Clock);
        Creation = new CharacterCreation(Population, Ids, World, Events, Clock);
        Aggregate = new AggregateSimulation(World, Population, Buildings, Recipes, Events, Clock, Creation);
        Lod = new LodSystem(
            World,
            Population,
            Buildings,
            Clock,
            Events,
            () => Cursor.Position,
            Aggregate,
            Production,
            Teaching);
        Characters = new CharacterSimulation(World, Population, Clock, Events, Production, Teaching, Lod);
        Settlements = new SettlementDirectory();
        SettlementDetection = new SettlementSystem(World, Population, Buildings, Settlements, Ids, Events, Clock, Recipes);
        Exploration = new ExplorationSystem(World, Clock, Events);
        Civilization = new CivilizationSystem(worldSeed, Ids, Population, Events, Clock);
        Politics = new InternalPoliticsSystem(worldSeed, Ids, Population, Civilization.Factions, Events, Clock);
        Military = new MilitarySystem(worldSeed, Ids, Population, Civilization.Factions, Events, Clock);
        Civilization.SeedBaseline();
        Politics.SeedBaseline();
        Military.SeedBaseline();

        Commands.Register(new PingCommandHandler());
        Commands.Register(new MoveDebugCursorHandler(Cursor));
        Commands.Register(new SetOccupancyHandler(World));
        Commands.Register(new PlaceBuildingHandler(Placement, Events, Clock));
        Commands.Register(new RemoveBuildingHandler(Placement, Production, Events, Clock));
        Commands.Register(new TeachCharacterHandler(Population, Teaching, Characters.Navigator));
        Commands.Register(new CreateChildHandler(Creation));
        Commands.Register(new AddSkillExperienceHandler(Population));
        Commands.Register(new EvaluateSettlementsHandler(SettlementDetection));
        Commands.Register(new RefreshLodHandler(Lod));
        Commands.Register(new ForceChunkLodHandler(Lod));
        Commands.Register(new ClearChunkLodOverrideHandler(Lod));
        Commands.Register(new ProtectCharacterHandler(Population, Lod));
        Commands.Register(new SetChunkPresentationHandler(Lod));
        Commands.Register(new RumorChunkHandler(Exploration));
        Commands.Register(new ScoutChunkHandler(Exploration));
        Commands.Register(new MapChunkHandler(Exploration));
        Commands.Register(new ConfirmChunkHandler(Exploration));
        Commands.Register(new AnalyzeChunkHandler(Exploration));
        Commands.Register(new CreateCultureHandler(Civilization));
        Commands.Register(new CreateFactionHandler(Civilization));
        Commands.Register(new AssignFactionMembershipHandler(Civilization));
        Commands.Register(new AssignCultureHandler(Civilization));
        Commands.Register(new SetFactionRelationHandler(Civilization.Diplomacy));
        Commands.Register(new SetDiplomaticStanceHandler(Civilization.Diplomacy));
        Commands.Register(new CreatePoliticalGroupHandler(Politics));
        Commands.Register(new AssignPoliticalGroupHandler(Politics));
        Commands.Register(new SetPoliticalGroupInfluenceHandler(Politics));
        Commands.Register(new SetInternalStabilityHandler(Politics));
        Commands.Register(new CreateMilitaryUnitHandler(Military));
        Commands.Register(new AssignCharacterToMilitaryUnitHandler(Military));
        Commands.Register(new RemoveCharacterFromMilitaryUnitHandler(Military));
        Commands.Register(new DisbandMilitaryUnitHandler(Military));
    }

    public ulong WorldSeed { get; }
    public SimulationClock Clock { get; }
    public IDeterministicRandom Random { get; }
    public EventBus Events { get; }
    public CommandProcessor Commands { get; }
    public EntityIdFactory Ids { get; }
    public LogicalWorld World { get; }
    public SimulationCursor Cursor { get; }
    public BuildingDirectory Buildings { get; }
    public BuildingCatalog Catalog { get; }
    public RecipeCatalog Recipes { get; }
    public BuildingPlacementSystem Placement { get; }
    public ProductionSystem Production { get; }
    public TeachingSystem Teaching { get; }
    public CharacterCreation Creation { get; }
    public PopulationRoster Population { get; }
    public CharacterSimulation Characters { get; }
    public SettlementDirectory Settlements { get; }
    public SettlementSystem SettlementDetection { get; }
    public AggregateSimulation Aggregate { get; }
    public LodSystem Lod { get; }
    public ExplorationSystem Exploration { get; }
    public CivilizationSystem Civilization { get; }
    public DiplomacySystem Diplomacy => Civilization.Diplomacy;
    public InternalPoliticsSystem Politics { get; }
    public MilitarySystem Military { get; }

    public ulong Step(ulong ticks)
    {
        var previous = Clock.Tick;
        var advanced = Clock.Advance(ticks);
        for (ulong i = 0; i < advanced; i++)
        {
            Characters.Tick();
            SettlementDetection.Tick();
            Lod.Tick();
        }

        if (advanced > 0)
            Events.Publish(new TickAdvancedEvent(Clock.Tick, previous, advanced));

        return advanced;
    }

    public SaveEnvelope CreateSave() => SaveEnvelopeFactory.FromHost(WorldSeed, Clock, World);

    public static SimulationHost FromSave(SaveEnvelope envelope, SimulationCalendar? calendar = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var config = WorldConfiguration.Create(
            envelope.WorldWidth > 0 ? envelope.WorldWidth : WorldConfiguration.DebugWidth,
            envelope.WorldHeight > 0 ? envelope.WorldHeight : WorldConfiguration.DebugHeight,
            envelope.ChunkWidth > 0 ? envelope.ChunkWidth : WorldConfiguration.DebugChunkWidth,
            envelope.ChunkHeight > 0 ? envelope.ChunkHeight : WorldConfiguration.DebugChunkHeight,
            envelope.GenerationVersion > 0 ? envelope.GenerationVersion : WorldGeneration.CurrentVersion);
        return new SimulationHost(envelope.WorldSeed, calendar, envelope.SimulationTick, config);
    }
}

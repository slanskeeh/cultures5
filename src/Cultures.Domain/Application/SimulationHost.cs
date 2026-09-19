using System.Diagnostics;
using Cultures.Application.Persistence;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Randomness;
using Cultures.Core.Time;
using Cultures.Exploration;
using Cultures.History;
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
        bool placeDevelopmentBuildings = true,
        bool seedContent = true,
        SimulationBalance? balance = null)
    {
        WorldSeed = worldSeed;
        Balance = balance ?? SimulationBalance.Development;
        Diagnostics = new SimulationDiagnostics();
        Clock = new SimulationClock(calendar, initialTick);
        Random = new SeededRandom(worldSeed);
        Events = new EventBus();
        Commands = new CommandProcessor();
        Ids = new EntityIdFactory();
        History = new HistoryRecorder(Ids, Events);
        World = new LogicalWorld(world ?? WorldConfiguration.DebugSample, worldSeed);
        Cursor = new SimulationCursor(
            World,
            Events,
            () => Clock.Tick,
            new LogicalGridCoordinate(0, World.Configuration.Height / 2));
        Buildings = new BuildingDirectory();
        Catalog = BuildingCatalog.Development;
        Recipes = RecipeCatalog.Development;
        Professions = ProfessionCatalog.Development;
        Placement = new BuildingPlacementSystem(World, Buildings, Catalog, Ids);
        Production = new ProductionSystem(
            World,
            Buildings,
            new ProductionResolver(Recipes, new ContextualEnvironmentProductionModifier()),
            Events,
            Clock);
        Ecology = new NaturalResourceSystem(worldSeed, World, Ids);
        Production.Ecology = Ecology;
        WorldEvents = new WorldEventSystem(Clock, Events);

        if (seedContent && placeDevelopmentBuildings)
            DevelopmentSiteBootstrap.Place(Placement, World);

        Population = seedContent
            ? PopulationSpawner.Spawn(World, Ids, worldSeed, populationCount)
            : new PopulationRoster();
        if (seedContent)
            ProfessionBootstrap.AssignStarting(Population, Professions);
        Households = new HouseholdDirectory();
        Teaching = new TeachingSystem(Population, World, Events, Clock);
        Creation = new CharacterCreation(Population, Ids, World, Events, Clock);
        Social = new SocialLifeSystem(Ids, Population, Households, Buildings, Professions, Events, Clock);
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
        Exploration = new ExplorationSystem(World, Clock, Events);
        Labor = new LaborSystem(World, Population, Production, Ecology, Exploration, Professions);
        Characters = new CharacterSimulation(World, Population, Clock, Events, Production, Teaching, Lod);
        Characters.AttachSocial(Social);
        Characters.AttachLabor(Labor);
        Settlements = new SettlementDirectory();
        SettlementDetection = new SettlementSystem(World, Population, Buildings, Settlements, Ids, Events, Clock, Recipes);
        Civilization = new CivilizationSystem(worldSeed, Ids, Population, Events, Clock);
        Politics = new InternalPoliticsSystem(worldSeed, Ids, Population, Civilization.Factions, Events, Clock);
        Military = new MilitarySystem(worldSeed, Ids, Population, Civilization.Factions, Events, Clock);
        if (seedContent)
        {
            Civilization.SeedBaseline();
            Politics.SeedBaseline();
            Military.SeedBaseline();
        }

        RegisterCommands();
        if (seedContent)
            SeedEcologyNearContent();
    }

    public ulong WorldSeed { get; }
    public SimulationBalance Balance { get; }
    public SimulationDiagnostics Diagnostics { get; }
    public bool OnboardingComplete { get; set; }
    public SaveEnvelope? LastAutosave { get; private set; }
    public SimulationClock Clock { get; }
    public IDeterministicRandom Random { get; }
    public EventBus Events { get; }
    public CommandProcessor Commands { get; }
    public EntityIdFactory Ids { get; }
    public HistoryRecorder History { get; }
    public LogicalWorld World { get; }
    public SimulationCursor Cursor { get; }
    public BuildingDirectory Buildings { get; }
    public BuildingCatalog Catalog { get; }
    public RecipeCatalog Recipes { get; }
    public ProfessionCatalog Professions { get; }
    public BuildingPlacementSystem Placement { get; }
    public ProductionSystem Production { get; }
    public NaturalResourceSystem Ecology { get; }
    public WorldEventSystem WorldEvents { get; }
    public TeachingSystem Teaching { get; }
    public CharacterCreation Creation { get; }
    public SocialLifeSystem Social { get; }
    public HouseholdDirectory Households { get; }
    public PopulationRoster Population { get; }
    public CharacterSimulation Characters { get; }
    public SettlementDirectory Settlements { get; }
    public SettlementSystem SettlementDetection { get; }
    public AggregateSimulation Aggregate { get; }
    public LodSystem Lod { get; }
    public ExplorationSystem Exploration { get; }
    public LaborSystem Labor { get; }
    public CivilizationSystem Civilization { get; }
    public DiplomacySystem Diplomacy => Civilization.Diplomacy;
    public InternalPoliticsSystem Politics { get; }
    public MilitarySystem Military { get; }

    public ulong Step(ulong ticks)
    {
        var watch = Stopwatch.StartNew();
        var previous = Clock.Tick;
        ulong applied = 0;
        try
        {
            for (ulong i = 0; i < ticks; i++)
            {
                if (Clock.IsPaused)
                    break;
                var advanced = Clock.Advance(1);
                if (advanced == 0)
                    break;
                Characters.Tick();
                SettlementDetection.Tick();
                Lod.Tick();
                Ecology.Tick();
                WorldEvents.Tick();
                applied++;
            }

            if (applied > 0)
                Events.Publish(new TickAdvancedEvent(Clock.Tick, previous, applied));

            MaybeAutosave();
        }
        catch (Exception ex)
        {
            Diagnostics.RecordFault(ex);
        }
        finally
        {
            Diagnostics.RecordStep(watch.ElapsedMilliseconds, Clock.Tick, Population.Count);
        }

        return applied;
    }

    public SaveEnvelope CreateSave() => SimulationPersistence.Capture(this);

    public string WriteSave(ISaveStore store, string slot, SaveSerializer? serializer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        serializer ??= new SaveSerializer();
        var json = serializer.Serialize(CreateSave());
        store.Write(slot, json);
        return json;
    }

    public static SimulationHost FromSave(SaveEnvelope envelope, SimulationCalendar? calendar = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        envelope = SaveMigrations.ToCurrent(envelope);
        var config = WorldConfiguration.Create(
            envelope.WorldWidth > 0 ? envelope.WorldWidth : WorldConfiguration.DebugWidth,
            envelope.WorldHeight > 0 ? envelope.WorldHeight : WorldConfiguration.DebugHeight,
            envelope.ChunkWidth > 0 ? envelope.ChunkWidth : WorldConfiguration.DebugChunkWidth,
            envelope.ChunkHeight > 0 ? envelope.ChunkHeight : WorldConfiguration.DebugChunkHeight,
            envelope.GenerationVersion > 0 ? envelope.GenerationVersion : WorldGeneration.CurrentVersion);

        if (envelope.SaveVersion == 2)
            return new SimulationHost(envelope.WorldSeed, calendar, envelope.SimulationTick, config);

        var host = new SimulationHost(
            envelope.WorldSeed,
            calendar,
            envelope.SimulationTick,
            config,
            populationCount: 0,
            placeDevelopmentBuildings: false,
            seedContent: false);
        SimulationPersistence.Restore(host, envelope);
        host.Cursor.Restore(new LogicalGridCoordinate(envelope.CursorX, envelope.CursorY));
        return host;
    }

    public static SimulationHost LoadSave(ISaveStore store, string slot, SaveSerializer? serializer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        serializer ??= new SaveSerializer();
        if (!store.TryRead(slot, out var json))
            throw new InvalidOperationException($"Save slot '{slot}' was not found.");
        return FromSave(serializer.Deserialize(json));
    }

    private void MaybeAutosave()
    {
        var interval = Balance.AutosaveIntervalTicks;
        if (interval <= 0 || Clock.Tick == 0 || Clock.Tick % (ulong)interval != 0)
            return;
        LastAutosave = CreateSave();
    }

    private void RegisterCommands()
    {
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
        Commands.Register(new FormDiplomaticPactHandler(Civilization.Diplomacy));
        Commands.Register(new BreakDiplomaticPactHandler(Civilization.Diplomacy));
        Commands.Register(new CreatePoliticalGroupHandler(Politics));
        Commands.Register(new AssignPoliticalGroupHandler(Politics));
        Commands.Register(new SetPoliticalGroupInfluenceHandler(Politics));
        Commands.Register(new SetInternalStabilityHandler(Politics));
        Commands.Register(new CreateMilitaryUnitHandler(Military));
        Commands.Register(new AssignCharacterToMilitaryUnitHandler(Military));
        Commands.Register(new RemoveCharacterFromMilitaryUnitHandler(Military));
        Commands.Register(new DisbandMilitaryUnitHandler(Military));
        Commands.Register(new AssignProfessionHandler(Social));
        Commands.Register(new MatchProfessionHandler(Social));
        Commands.Register(new DirectLaborHandler(Population));
        Commands.Register(new StopLaborHandler(Population));
        Commands.Register(new OrderMoveHandler(Population, Characters.Navigator, Production, Teaching));
        Commands.Register(new SetDebugCursorHandler(Cursor));
        Commands.Register(new FormHouseholdHandler(Social));
        Commands.Register(new SetHouseholdHomeHandler(Social));
        Commands.Register(new FormPartnershipHandler(Social));
        Commands.Register(new LeaveHouseholdHandler(Social));
        Commands.Register(new HuntWildlifeHandler(Population, Ecology, Events, Clock, Balance.HuntFoodYield));
    }

    private void SeedEcologyNearContent()
    {
        foreach (var building in Buildings.All)
            Ecology.EnsureCell(building.Origin);
        foreach (var person in Population.All)
            Ecology.EnsureCell(person.Position);
    }
}

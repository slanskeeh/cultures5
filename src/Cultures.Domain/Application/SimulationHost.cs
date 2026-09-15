using Cultures.Application.Persistence;
using Cultures.Buildings;
using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Randomness;
using Cultures.Core.Time;
using Cultures.Population;
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
        Characters = new CharacterSimulation(World, Population, Clock, Events, Production);

        Commands.Register(new PingCommandHandler());
        Commands.Register(new MoveDebugCursorHandler(Cursor));
        Commands.Register(new SetOccupancyHandler(World));
        Commands.Register(new PlaceBuildingHandler(Placement, Events, Clock));
        Commands.Register(new RemoveBuildingHandler(Placement, Production, Events, Clock));
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
    public PopulationRoster Population { get; }
    public CharacterSimulation Characters { get; }

    public ulong Step(ulong ticks)
    {
        var previous = Clock.Tick;
        var advanced = Clock.Advance(ticks);
        for (ulong i = 0; i < advanced; i++)
            Characters.Tick();

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

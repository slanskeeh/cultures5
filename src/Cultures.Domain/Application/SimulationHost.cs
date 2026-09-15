using Cultures.Application.Persistence;
using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Randomness;
using Cultures.Core.Time;
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
        WorldConfiguration? world = null)
    {
        WorldSeed = worldSeed;
        Clock = new SimulationClock(calendar, initialTick);
        Random = new SeededRandom(worldSeed);
        Events = new EventBus();
        Commands = new CommandProcessor();
        Ids = new EntityIdFactory();
        World = new LogicalWorld(world ?? WorldConfiguration.DebugSample);
        Cursor = new SimulationCursor(
            World,
            Events,
            () => Clock.Tick,
            new LogicalGridCoordinate(0, World.Configuration.Height / 2));

        Commands.Register(new PingCommandHandler());
        Commands.Register(new MoveDebugCursorHandler(Cursor));
        Commands.Register(new SetOccupancyHandler(World));
    }

    public ulong WorldSeed { get; }
    public SimulationClock Clock { get; }
    public IDeterministicRandom Random { get; }
    public EventBus Events { get; }
    public CommandProcessor Commands { get; }
    public EntityIdFactory Ids { get; }
    public LogicalWorld World { get; }
    public SimulationCursor Cursor { get; }

    public ulong Step(ulong ticks)
    {
        var previous = Clock.Tick;
        var advanced = Clock.Advance(ticks);
        if (advanced > 0)
            Events.Publish(new TickAdvancedEvent(Clock.Tick, previous, advanced));

        return advanced;
    }

    public SaveEnvelope CreateSave() => SaveEnvelopeFactory.FromClock(WorldSeed, Clock);

    public static SimulationHost FromSave(SaveEnvelope envelope, SimulationCalendar? calendar = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return new SimulationHost(envelope.WorldSeed, calendar, envelope.SimulationTick);
    }
}

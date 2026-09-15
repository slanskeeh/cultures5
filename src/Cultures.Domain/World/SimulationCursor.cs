using Cultures.Core.Commands;
using Cultures.Core.Events;

namespace Cultures.World;

/// <summary>
/// Application-owned debug cursor. Occupancy of cells is independent of this position.
/// </summary>
public sealed class SimulationCursor
{
    private readonly LogicalWorld _world;
    private readonly EventBus _events;
    private readonly Func<ulong> _tick;

    public SimulationCursor(LogicalWorld world, EventBus events, Func<ulong> tick, LogicalGridCoordinate start)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        Position = start;
    }

    public LogicalGridCoordinate Position { get; private set; }

    public CommandResult TryMove(int dx, int dy)
    {
        var from = Position;
        var raw = new WorldCoordinate(from.X + dx, from.Y + dy);
        var resolution = _world.Topology.Resolve(raw);
        if (!resolution.TryGetCell(out var to))
            return CommandResult.Fail("Target is outside the world.");

        Position = to;
        var crossedSeam = dx != 0 && _world.Topology.WrapX(from.X + dx) != from.X + dx;
        _events.Publish(new DebugCursorMovedEvent(_tick(), from, to, crossedSeam));
        return CommandResult.Ok();
    }
}

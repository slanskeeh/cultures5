using Cultures.Core.Commands;
using Cultures.World;

namespace Cultures.World.Commands;

public sealed record MoveDebugCursorCommand(int Dx, int Dy) : ICommand;

public sealed record SetOccupancyCommand(WorldCoordinate Position, Occupancy Occupancy) : ICommand;

public sealed class MoveDebugCursorHandler : ICommandHandler<MoveDebugCursorCommand>
{
    private readonly SimulationCursor _cursor;

    public MoveDebugCursorHandler(SimulationCursor cursor)
    {
        _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
    }

    public CommandResult Handle(MoveDebugCursorCommand command) =>
        _cursor.TryMove(command.Dx, command.Dy);
}

public sealed class SetOccupancyHandler : ICommandHandler<SetOccupancyCommand>
{
    private readonly LogicalWorld _world;

    public SetOccupancyHandler(LogicalWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public CommandResult Handle(SetOccupancyCommand command)
    {
        if (!_world.Grid.TrySetOccupancy(command.Position, command.Occupancy))
            return CommandResult.Fail("Position is outside the world.");

        return CommandResult.Ok();
    }
}

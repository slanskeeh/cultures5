using Cultures.Core.Commands;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Buildings;

public sealed record PlaceBuildingCommand(
    BuildingTypeId TypeId,
    LogicalGridCoordinate Origin,
    bool CompleteImmediately = true) : ICommand;

public sealed record RemoveBuildingCommand(BuildingId Building) : ICommand;

public sealed class PlaceBuildingHandler : ICommandHandler<PlaceBuildingCommand>
{
    public PlaceBuildingHandler(BuildingPlacementSystem placement, EventBus events, SimulationClock clock)
    {
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public BuildingPlacementSystem Placement { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public CommandResult Handle(PlaceBuildingCommand command)
    {
        var result = Placement.TryPlace(command.TypeId, command.Origin, command.CompleteImmediately);
        if (!result.Success || result.Building is null)
            return CommandResult.Fail(result.Error ?? "Placement failed.");

        Events.Publish(new BuildingPlacedEvent(Clock.Tick, result.Building.Id, result.Building.TypeId.Value));
        if (result.Building.IsActive)
            Events.Publish(new BuildingCompletedEvent(Clock.Tick, result.Building.Id));
        return CommandResult.Ok();
    }
}

public sealed class RemoveBuildingHandler : ICommandHandler<RemoveBuildingCommand>
{
    public RemoveBuildingHandler(BuildingPlacementSystem placement, ProductionSystem production, EventBus events, SimulationClock clock)
    {
        Placement = placement ?? throw new ArgumentNullException(nameof(placement));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public BuildingPlacementSystem Placement { get; }
    public ProductionSystem Production { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public CommandResult Handle(RemoveBuildingCommand command)
    {
        if (Production.Buildings.TryGet(command.Building, out var building))
        {
            foreach (var slot in building.Workplaces)
                slot.Worker = CharacterId.None;
        }

        var result = Placement.TryRemove(command.Building);
        if (!result.Success)
            return CommandResult.Fail(result.Error ?? "Removal failed.");

        Events.Publish(new BuildingRemovedEvent(Clock.Tick, command.Building));
        return CommandResult.Ok();
    }
}

using Cultures.Buildings;
using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

public sealed record DirectLaborCommand(CharacterId Character) : ICommand;

public sealed record StopLaborCommand(CharacterId Character) : ICommand;

public sealed class DirectLaborHandler : ICommandHandler<DirectLaborCommand>
{
    public DirectLaborHandler(PopulationRoster population)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
    }

    public PopulationRoster Population { get; }

    public CommandResult Handle(DirectLaborCommand command)
    {
        if (!Population.TryGet(command.Character, out var character) || !character.IsAlive)
            return CommandResult.Fail("Character is invalid.");
        if (character.LifeStage < CharacterLifeStage.Adult)
            return CommandResult.Fail("Character is too young to take orders.");

        character.IsPlayerCommanded = false;
        character.IsPersistentIndividual = true;
        character.Activity.Cancel();
        return CommandResult.Ok();
    }
}

public sealed class StopLaborHandler : ICommandHandler<StopLaborCommand>
{
    public StopLaborHandler(PopulationRoster population)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
    }

    public PopulationRoster Population { get; }

    public CommandResult Handle(StopLaborCommand command)
    {
        if (!Population.TryGet(command.Character, out var character) || !character.IsAlive)
            return CommandResult.Fail("Character is invalid.");

        character.IsPlayerCommanded = false;
        character.Activity.Cancel();
        return CommandResult.Ok();
    }
}

public sealed record OrderMoveCommand(CharacterId Character, LogicalGridCoordinate Destination) : ICommand;

public sealed class OrderMoveHandler : ICommandHandler<OrderMoveCommand>
{
    public OrderMoveHandler(PopulationRoster population, GridNavigator navigator, ProductionSystem production, TeachingSystem teaching)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Production = production ?? throw new ArgumentNullException(nameof(production));
        Teaching = teaching ?? throw new ArgumentNullException(nameof(teaching));
    }

    public PopulationRoster Population { get; }
    public GridNavigator Navigator { get; }
    public ProductionSystem Production { get; }
    public TeachingSystem Teaching { get; }

    public CommandResult Handle(OrderMoveCommand command)
    {
        if (!Population.TryGet(command.Character, out var character) || !character.IsAlive)
            return CommandResult.Fail("Character is invalid.");
        if (character.LifeStage < CharacterLifeStage.Adult)
            return CommandResult.Fail("Character is too young to take orders.");
        if (!Navigator.IsPassable(command.Destination))
            return CommandResult.Fail("Destination is not passable.");

        if (character.Activity.Kind is ActionKind.Work)
            Production.AbandonWork(character);
        if (character.Activity.Kind is ActionKind.Teach or ActionKind.Learn)
            Teaching.Abandon(character);

        character.IsPlayerCommanded = true;
        character.IsPersistentIndividual = true;

        if (character.Position.Equals(command.Destination))
        {
            character.Activity.Start(ActionKind.Idle, CharacterRules.IdleDurationTicks, command.Destination);
            return CommandResult.Ok();
        }

        var path = Navigator.FindPath(character.Position, command.Destination);
        if (path is null || path.Count == 0)
            return CommandResult.Fail("No path to that cell.");

        character.Activity.Start(ActionKind.Move, path.Count, command.Destination);
        foreach (var step in path)
            character.Activity.RemainingPath.Enqueue(step);
        return CommandResult.Ok();
    }
}

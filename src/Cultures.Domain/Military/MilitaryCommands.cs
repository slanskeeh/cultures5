using Cultures.Core.Commands;
using Cultures.Core.Ids;

namespace Cultures.Military;

public sealed record CreateMilitaryUnitCommand(FactionId Faction, string? Name = null) : ICommand;

public sealed record AssignCharacterToMilitaryUnitCommand(CharacterId Character, MilitaryUnitId Unit) : ICommand;

public sealed record RemoveCharacterFromMilitaryUnitCommand(CharacterId Character, MilitaryUnitId Unit) : ICommand;

public sealed record DisbandMilitaryUnitCommand(MilitaryUnitId Unit) : ICommand;

public sealed class CreateMilitaryUnitHandler : ICommandHandler<CreateMilitaryUnitCommand>
{
    public CreateMilitaryUnitHandler(MilitarySystem military)
    {
        Military = military ?? throw new ArgumentNullException(nameof(military));
    }

    public MilitarySystem Military { get; }

    public CommandResult Handle(CreateMilitaryUnitCommand command) =>
        Military.TryCreateUnit(command.Faction, command.Name, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class AssignCharacterToMilitaryUnitHandler : ICommandHandler<AssignCharacterToMilitaryUnitCommand>
{
    public AssignCharacterToMilitaryUnitHandler(MilitarySystem military)
    {
        Military = military ?? throw new ArgumentNullException(nameof(military));
    }

    public MilitarySystem Military { get; }

    public CommandResult Handle(AssignCharacterToMilitaryUnitCommand command) =>
        Military.TryAssignUnit(command.Character, command.Unit, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class RemoveCharacterFromMilitaryUnitHandler : ICommandHandler<RemoveCharacterFromMilitaryUnitCommand>
{
    public RemoveCharacterFromMilitaryUnitHandler(MilitarySystem military)
    {
        Military = military ?? throw new ArgumentNullException(nameof(military));
    }

    public MilitarySystem Military { get; }

    public CommandResult Handle(RemoveCharacterFromMilitaryUnitCommand command) =>
        Military.TryRemoveFromUnit(command.Character, command.Unit, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class DisbandMilitaryUnitHandler : ICommandHandler<DisbandMilitaryUnitCommand>
{
    public DisbandMilitaryUnitHandler(MilitarySystem military)
    {
        Military = military ?? throw new ArgumentNullException(nameof(military));
    }

    public MilitarySystem Military { get; }

    public CommandResult Handle(DisbandMilitaryUnitCommand command) =>
        Military.TryDisband(command.Unit, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

using Cultures.Core.Commands;
using Cultures.Core.Ids;

namespace Cultures.Civilization;

public sealed record CreateCultureCommand(string? Name = null) : ICommand;

public sealed record CreateFactionCommand(CultureId Culture, string? Name = null) : ICommand;

public sealed record AssignFactionMembershipCommand(CharacterId Character, FactionId Faction) : ICommand;

public sealed record AssignCultureCommand(CharacterId Character, CultureId Culture) : ICommand;

public sealed record SetFactionRelationCommand(FactionId Left, FactionId Right, FactionRelationStance Stance) : ICommand;

public sealed class CreateCultureHandler : ICommandHandler<CreateCultureCommand>
{
    public CreateCultureHandler(CivilizationSystem civilization)
    {
        Civilization = civilization ?? throw new ArgumentNullException(nameof(civilization));
    }

    public CivilizationSystem Civilization { get; }

    public CommandResult Handle(CreateCultureCommand command) =>
        Civilization.TryCreateCulture(command.Name, null, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class CreateFactionHandler : ICommandHandler<CreateFactionCommand>
{
    public CreateFactionHandler(CivilizationSystem civilization)
    {
        Civilization = civilization ?? throw new ArgumentNullException(nameof(civilization));
    }

    public CivilizationSystem Civilization { get; }

    public CommandResult Handle(CreateFactionCommand command) =>
        Civilization.TryCreateFaction(command.Culture, command.Name, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class AssignFactionMembershipHandler : ICommandHandler<AssignFactionMembershipCommand>
{
    public AssignFactionMembershipHandler(CivilizationSystem civilization)
    {
        Civilization = civilization ?? throw new ArgumentNullException(nameof(civilization));
    }

    public CivilizationSystem Civilization { get; }

    public CommandResult Handle(AssignFactionMembershipCommand command) =>
        Civilization.TryAssignFaction(command.Character, command.Faction, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class AssignCultureHandler : ICommandHandler<AssignCultureCommand>
{
    public AssignCultureHandler(CivilizationSystem civilization)
    {
        Civilization = civilization ?? throw new ArgumentNullException(nameof(civilization));
    }

    public CivilizationSystem Civilization { get; }

    public CommandResult Handle(AssignCultureCommand command) =>
        Civilization.TryAssignCulture(command.Character, command.Culture, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class SetFactionRelationHandler : ICommandHandler<SetFactionRelationCommand>
{
    public SetFactionRelationHandler(CivilizationSystem civilization)
    {
        Civilization = civilization ?? throw new ArgumentNullException(nameof(civilization));
    }

    public CivilizationSystem Civilization { get; }

    public CommandResult Handle(SetFactionRelationCommand command) =>
        Civilization.TrySetRelation(command.Left, command.Right, command.Stance, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

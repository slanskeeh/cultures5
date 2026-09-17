using Cultures.Core.Commands;
using Cultures.Core.Ids;

namespace Cultures.Civilization;

public sealed record CreatePoliticalGroupCommand(FactionId Faction, string? Name = null) : ICommand;

public sealed record AssignPoliticalGroupCommand(CharacterId Character, PoliticalGroupId Group) : ICommand;

public sealed record SetPoliticalGroupInfluenceCommand(PoliticalGroupId Group, int Influence) : ICommand;

public sealed record SetInternalStabilityCommand(FactionId Faction, int Stability) : ICommand;

public sealed class CreatePoliticalGroupHandler : ICommandHandler<CreatePoliticalGroupCommand>
{
    public CreatePoliticalGroupHandler(InternalPoliticsSystem politics)
    {
        Politics = politics ?? throw new ArgumentNullException(nameof(politics));
    }

    public InternalPoliticsSystem Politics { get; }

    public CommandResult Handle(CreatePoliticalGroupCommand command) =>
        Politics.TryCreateGroup(command.Faction, command.Name, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class AssignPoliticalGroupHandler : ICommandHandler<AssignPoliticalGroupCommand>
{
    public AssignPoliticalGroupHandler(InternalPoliticsSystem politics)
    {
        Politics = politics ?? throw new ArgumentNullException(nameof(politics));
    }

    public InternalPoliticsSystem Politics { get; }

    public CommandResult Handle(AssignPoliticalGroupCommand command) =>
        Politics.TryAssignGroup(command.Character, command.Group, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class SetPoliticalGroupInfluenceHandler : ICommandHandler<SetPoliticalGroupInfluenceCommand>
{
    public SetPoliticalGroupInfluenceHandler(InternalPoliticsSystem politics)
    {
        Politics = politics ?? throw new ArgumentNullException(nameof(politics));
    }

    public InternalPoliticsSystem Politics { get; }

    public CommandResult Handle(SetPoliticalGroupInfluenceCommand command) =>
        Politics.TrySetInfluence(command.Group, command.Influence, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class SetInternalStabilityHandler : ICommandHandler<SetInternalStabilityCommand>
{
    public SetInternalStabilityHandler(InternalPoliticsSystem politics)
    {
        Politics = politics ?? throw new ArgumentNullException(nameof(politics));
    }

    public InternalPoliticsSystem Politics { get; }

    public CommandResult Handle(SetInternalStabilityCommand command) =>
        Politics.TrySetStability(command.Faction, command.Stability, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

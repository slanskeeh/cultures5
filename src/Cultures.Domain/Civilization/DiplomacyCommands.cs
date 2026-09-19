using Cultures.Core.Commands;
using Cultures.Core.Ids;

namespace Cultures.Civilization;

public sealed record SetDiplomaticStanceCommand(
    FactionId Left,
    FactionId Right,
    FactionRelationStance Stance) : ICommand;

public sealed class SetDiplomaticStanceHandler : ICommandHandler<SetDiplomaticStanceCommand>
{
    public SetDiplomaticStanceHandler(DiplomacySystem diplomacy)
    {
        Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public DiplomacySystem Diplomacy { get; }

    public CommandResult Handle(SetDiplomaticStanceCommand command) =>
        Diplomacy.TrySetStance(command.Left, command.Right, command.Stance, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed record FormDiplomaticPactCommand(
    FactionId Left,
    FactionId Right,
    DiplomaticPactKind Kind) : ICommand;

public sealed class FormDiplomaticPactHandler : ICommandHandler<FormDiplomaticPactCommand>
{
    public FormDiplomaticPactHandler(DiplomacySystem diplomacy)
    {
        Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public DiplomacySystem Diplomacy { get; }

    public CommandResult Handle(FormDiplomaticPactCommand command) =>
        Diplomacy.TryFormPact(command.Left, command.Right, command.Kind, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed record BreakDiplomaticPactCommand(DiplomaticPactId Pact) : ICommand;

public sealed class BreakDiplomaticPactHandler : ICommandHandler<BreakDiplomaticPactCommand>
{
    public BreakDiplomaticPactHandler(DiplomacySystem diplomacy)
    {
        Diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
    }

    public DiplomacySystem Diplomacy { get; }

    public CommandResult Handle(BreakDiplomaticPactCommand command) =>
        Diplomacy.TryBreakPact(command.Pact, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

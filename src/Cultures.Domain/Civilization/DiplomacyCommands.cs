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

using Cultures.Core.Commands;

namespace Cultures.Settlement;

public sealed record EvaluateSettlementsCommand : ICommand;

public sealed class EvaluateSettlementsHandler : ICommandHandler<EvaluateSettlementsCommand>
{
    public EvaluateSettlementsHandler(SettlementSystem settlements)
    {
        Settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
    }

    public SettlementSystem Settlements { get; }

    public CommandResult Handle(EvaluateSettlementsCommand command)
    {
        Settlements.Evaluate();
        return CommandResult.Ok();
    }
}

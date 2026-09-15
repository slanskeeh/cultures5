using Cultures.Core.Commands;

namespace Cultures.Core.Commands;

/// <summary>
/// Trivial command used to prove command routing without gameplay.
/// </summary>
public sealed record PingCommand(string Token) : ICommand;

public sealed class PingCommandHandler : ICommandHandler<PingCommand>
{
    public string? LastToken { get; private set; }
    public int ExecutionCount { get; private set; }

    public CommandResult Handle(PingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            return CommandResult.Fail("Token is required.");

        LastToken = command.Token;
        ExecutionCount++;
        return CommandResult.Ok();
    }
}

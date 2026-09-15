namespace Cultures.Core.Commands;

/// <summary>
/// A request that may fail. Distinct from events, which are facts.
/// </summary>
public interface ICommand;

public readonly record struct CommandResult(bool Success, string? Error = null)
{
    public static CommandResult Ok() => new(true);
    public static CommandResult Fail(string error) => new(false, error);
}

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    CommandResult Handle(TCommand command);
}

public sealed class CommandProcessor
{
    private readonly Dictionary<Type, Func<ICommand, CommandResult>> _handlers = new();

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[typeof(TCommand)] = command => handler.Handle((TCommand)command);
    }

    public CommandResult Execute(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_handlers.TryGetValue(command.GetType(), out var handler))
            return CommandResult.Fail($"No handler registered for {command.GetType().Name}.");

        return handler(command);
    }
}

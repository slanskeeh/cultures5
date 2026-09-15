namespace Cultures.Core.Events;

public interface IEventBus
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISimulationEvent;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : ISimulationEvent;
    void Publish<TEvent>(TEvent simulationEvent) where TEvent : ISimulationEvent;
}

/// <summary>
/// In-process dispatcher. Subscribers are invoked independently from a snapshot,
/// so one handler cannot mutate the subscription list seen by this publish.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : ISimulationEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = [];
            _handlers[type] = list;
        }

        list.Add(handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : ISimulationEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            return;

        list.Remove(handler);
    }

    public void Publish<TEvent>(TEvent simulationEvent) where TEvent : ISimulationEvent
    {
        ArgumentNullException.ThrowIfNull(simulationEvent);
        if (!_handlers.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
            return;

        var snapshot = list.ToArray();
        foreach (var handler in snapshot)
            ((Action<TEvent>)handler)(simulationEvent);
    }
}

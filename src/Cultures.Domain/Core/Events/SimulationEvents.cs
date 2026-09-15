namespace Cultures.Core.Events;

/// <summary>
/// A fact that already happened. Events are immutable.
/// </summary>
public interface ISimulationEvent
{
    ulong Tick { get; }
}

/// <summary>
/// Example immutable event used by the Phase 0 host and tests.
/// </summary>
public sealed record TickAdvancedEvent(ulong Tick, ulong PreviousTick, ulong TicksAdvanced) : ISimulationEvent;

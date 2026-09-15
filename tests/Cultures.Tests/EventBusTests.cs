using Cultures.Application;
using Cultures.Core.Events;

namespace Cultures.Tests;

public sealed class EventBusTests
{
    [Fact]
    public void Two_subscribers_react_independently()
    {
        var bus = new EventBus();
        var first = new List<ulong>();
        var second = new List<ulong>();

        bus.Subscribe<TickAdvancedEvent>(e => first.Add(e.TicksAdvanced));
        bus.Subscribe<TickAdvancedEvent>(e => second.Add(e.Tick));

        bus.Publish(new TickAdvancedEvent(Tick: 10, PreviousTick: 4, TicksAdvanced: 6));

        Assert.Equal([6UL], first);
        Assert.Equal([10UL], second);
    }

    [Fact]
    public void Unsubscribing_one_handler_does_not_stop_the_other()
    {
        var bus = new EventBus();
        var remaining = 0;
        var removed = 0;

        Action<TickAdvancedEvent> extra = _ => removed++;
        bus.Subscribe<TickAdvancedEvent>(_ => remaining++);
        bus.Subscribe(extra);
        bus.Unsubscribe(extra);

        bus.Publish(new TickAdvancedEvent(1, 0, 1));

        Assert.Equal(1, remaining);
        Assert.Equal(0, removed);
    }

    [Fact]
    public void Host_publishes_tick_events_to_all_subscribers()
    {
        var host = new SimulationHost(worldSeed: 11);
        var ticks = new List<ulong>();
        var copies = new List<ulong>();

        host.Events.Subscribe<TickAdvancedEvent>(e => ticks.Add(e.TicksAdvanced));
        host.Events.Subscribe<TickAdvancedEvent>(e => copies.Add(e.CurrentTickOrSelf()));

        host.Step(5);

        Assert.Equal([5UL], ticks);
        Assert.Equal([5UL], copies);
    }
}

file static class TickAdvancedEventTestExtensions
{
    public static ulong CurrentTickOrSelf(this TickAdvancedEvent e) => e.Tick;
}

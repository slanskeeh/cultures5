using Cultures.Core.Time;

namespace Cultures.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void Advance_is_independent_of_any_frame_delta()
    {
        var clock = new SimulationClock();
        Assert.Equal(100UL, clock.Advance(100));
        Assert.Equal(100UL, clock.Tick);
        Assert.Equal(0UL, clock.Advance(0));
        Assert.Equal(100UL, clock.Tick);
    }

    [Fact]
    public void Pause_blocks_advancement()
    {
        var clock = new SimulationClock();
        clock.Pause();
        Assert.Equal(0UL, clock.Advance(50));
        Assert.Equal(0UL, clock.Tick);
        Assert.True(clock.IsPaused);

        clock.Resume();
        Assert.Equal(12UL, clock.Advance(12));
        Assert.Equal(12UL, clock.Tick);
        Assert.False(clock.IsPaused);
    }

    [Fact]
    public void Repeated_single_ticks_match_bulk_advance()
    {
        var stepwise = new SimulationClock();
        var bulk = new SimulationClock();

        for (var i = 0; i < 90; i++)
            stepwise.Advance(1);

        bulk.Advance(90);

        Assert.Equal(bulk.Tick, stepwise.Tick);
        Assert.Equal(bulk.Date, stepwise.Date);
    }

    [Fact]
    public void Calendar_converts_tick_hierarchy()
    {
        var calendar = SimulationCalendar.Default;
        var clock = new SimulationClock(calendar);

        clock.Advance(calendar.TicksPerHour);
        Assert.Equal(1, clock.Date.HourOfDay);
        Assert.Equal(0, clock.Date.MinuteOfHour);

        clock.Advance(calendar.TicksPerDay - calendar.TicksPerHour);
        Assert.Equal(1, clock.Date.DayOfSeason);
        Assert.Equal(0, clock.Date.HourOfDay);

        var yearClock = new SimulationClock(calendar, initialTick: calendar.TicksPerYear + (calendar.TicksPerSeason * 2));
        Assert.Equal(1UL, yearClock.Date.Year);
        Assert.Equal(2, yearClock.Date.SeasonIndex);
    }
}

namespace Cultures.Core.Time;

/// <summary>
/// Calendar conversion rates. Starting defaults, not final game values.
/// Hierarchy: Tick → Minute → Hour → Day → Season → Year.
/// </summary>
public sealed class SimulationCalendar
{
    public static SimulationCalendar Default { get; } = new();

    public int TicksPerMinute { get; init; } = 1;
    public int MinutesPerHour { get; init; } = 60;
    public int HoursPerDay { get; init; } = 24;
    public int DaysPerSeason { get; init; } = 30;
    public int SeasonsPerYear { get; init; } = 4;

    public ulong TicksPerHour => (ulong)TicksPerMinute * (ulong)MinutesPerHour;
    public ulong TicksPerDay => TicksPerHour * (ulong)HoursPerDay;
    public ulong TicksPerSeason => TicksPerDay * (ulong)DaysPerSeason;
    public ulong TicksPerYear => TicksPerSeason * (ulong)SeasonsPerYear;

    public CalendarDate FromTick(ulong tick)
    {
        var year = tick / TicksPerYear;
        var rem = tick % TicksPerYear;
        var season = (int)(rem / TicksPerSeason);
        rem %= TicksPerSeason;
        var day = (int)(rem / TicksPerDay);
        rem %= TicksPerDay;
        var hour = (int)(rem / TicksPerHour);
        rem %= TicksPerHour;
        var minute = (int)(rem / (ulong)TicksPerMinute);
        var tickOfMinute = (int)(rem % (ulong)TicksPerMinute);

        return new CalendarDate(
            Tick: tick,
            TickOfMinute: tickOfMinute,
            MinuteOfHour: minute,
            HourOfDay: hour,
            DayOfSeason: day,
            SeasonIndex: season,
            Year: year);
    }
}

/// <summary>
/// 0-based calendar breakdown derived from an absolute tick.
/// </summary>
public readonly record struct CalendarDate(
    ulong Tick,
    int TickOfMinute,
    int MinuteOfHour,
    int HourOfDay,
    int DayOfSeason,
    int SeasonIndex,
    ulong Year);

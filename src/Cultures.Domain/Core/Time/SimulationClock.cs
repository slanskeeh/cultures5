namespace Cultures.Core.Time;

/// <summary>
/// Authoritative simulation clock. Advances only when explicitly requested;
/// rendering FPS never drives this type.
/// </summary>
public sealed class SimulationClock
{
    public SimulationClock(SimulationCalendar? calendar = null, ulong initialTick = 0)
    {
        Calendar = calendar ?? SimulationCalendar.Default;
        Tick = initialTick;
    }

    public SimulationCalendar Calendar { get; }
    public ulong Tick { get; private set; }
    public bool IsPaused { get; private set; }
    public int Speed { get; private set; } = 1;
    public CalendarDate Date => Calendar.FromTick(Tick);

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;
    public void SetPaused(bool paused) => IsPaused = paused;

    public void SetSpeed(int speed) => Speed = Math.Clamp(speed, 1, 8);

    public void CycleSpeed() => SetSpeed(Speed >= 8 ? 1 : Speed * 2);

    /// <summary>
    /// Advances the clock by <paramref name="ticks"/> unless paused.
    /// Returns the number of ticks actually applied.
    /// </summary>
    public ulong Advance(ulong ticks)
    {
        if (IsPaused || ticks == 0)
            return 0;

        Tick += ticks;
        return ticks;
    }
}

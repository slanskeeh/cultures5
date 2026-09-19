using Cultures.Core.Events;
using Cultures.Core.Time;

namespace Cultures.World;

/// <summary>
/// Calendar-driven world facts. Does not start wars or rewrite geography.
/// </summary>
public sealed class WorldEventSystem
{
    private int _lastSeason = -1;

    public WorldEventSystem(SimulationClock clock, EventBus events)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public SimulationClock Clock { get; }
    public EventBus Events { get; }

    public void Tick()
    {
        var season = Clock.Date.SeasonIndex;
        if (_lastSeason < 0)
        {
            _lastSeason = season;
            return;
        }

        if (season == _lastSeason)
            return;

        _lastSeason = season;
        Events.Publish(new SeasonChangedEvent(Clock.Tick, season, Clock.Date.Year));
    }

    public void RestoreSeason(int seasonIndex) => _lastSeason = seasonIndex;
}

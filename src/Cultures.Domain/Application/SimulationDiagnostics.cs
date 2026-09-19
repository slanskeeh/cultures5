using System.Diagnostics;

namespace Cultures.Application;

/// <summary>
/// Step timing and last fault. Does not mutate simulation.
/// </summary>
public sealed class SimulationDiagnostics
{
    private readonly List<string> _faults = [];

    public string? LastFault { get; private set; }
    public long LastStepMilliseconds { get; private set; }
    public ulong LastStepTick { get; private set; }
    public int LastPopulation { get; private set; }
    public IReadOnlyList<string> Faults => _faults;

    public void RecordStep(long milliseconds, ulong tick, int population)
    {
        LastStepMilliseconds = milliseconds;
        LastStepTick = tick;
        LastPopulation = population;
    }

    public void RecordFault(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        LastFault = $"{exception.GetType().Name}: {exception.Message}";
        _faults.Add($"{DateTime.UtcNow:O} {LastFault}");
        if (_faults.Count > 16)
            _faults.RemoveAt(0);
    }

    public string Render()
    {
        var fault = string.IsNullOrEmpty(LastFault) ? "none" : LastFault;
        return $"step {LastStepMilliseconds}ms  tick {LastStepTick}  pop {LastPopulation}  fault {fault}";
    }

    public static long ElapsedMilliseconds(Stopwatch watch) => watch.ElapsedMilliseconds;
}

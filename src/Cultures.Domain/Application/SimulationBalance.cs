namespace Cultures.Application;

/// <summary>
/// Centralized Alpha tunables. Not final game-design values.
/// </summary>
public sealed class SimulationBalance
{
    public static SimulationBalance Development { get; } = new();

    public int HuntFoodYield { get; init; } = 2;
    public int AutosaveIntervalTicks { get; init; }
    public int MaxSpeed { get; init; } = 8;
}

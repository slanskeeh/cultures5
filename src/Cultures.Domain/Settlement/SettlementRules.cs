namespace Cultures.Settlement;

/// <summary>
/// Provisional emergence and lifecycle numbers. Not final game design (OD-019).
/// </summary>
public static class SettlementRules
{
    public const int EvaluationIntervalTicks = 60;
    public const int MinPeople = 4;
    public const int MinBuildings = 2;
    public const int EstablishedAfterEvals = 3;
    public const int DeclineAfterUnmatchedEvals = 3;
    public const int AbandonAfterUnmatchedEvals = 6;
}

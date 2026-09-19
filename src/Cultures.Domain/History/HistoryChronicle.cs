namespace Cultures.History;

/// <summary>
/// Readable recent chronology for presentation. Not AI memory.
/// </summary>
public static class HistoryChronicle
{
    public static string RenderRecent(HistoryRecorder history, int count = 8)
    {
        ArgumentNullException.ThrowIfNull(history);
        var events = history.GetRecentEvents(count);
        if (events.Count == 0)
            return "no history";
        return string.Join(" | ", events.Select(e => $"{e.Tick}:{e.Summary}"));
    }

    public static IReadOnlyList<HistoryRecord> MajorRecent(HistoryRecorder history, int count = 8)
    {
        ArgumentNullException.ThrowIfNull(history);
        return history.Directory.All
            .Where(e => e.Importance == HistoryImportance.Major)
            .TakeLast(count)
            .ToArray();
    }
}

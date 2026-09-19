using Cultures.Core.Ids;

namespace Cultures.History;

/// <summary>
/// Central sparse history store. Not per-character logs.
/// </summary>
public sealed class HistoryDirectory
{
    private readonly Dictionary<ulong, HistoryRecord> _byId = new();
    private readonly List<HistoryRecord> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<HistoryRecord> All => _order;

    public void Add(HistoryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!_byId.TryAdd(record.Id.Value, record))
            throw new InvalidOperationException($"Duplicate history {record.Id}.");
        _order.Add(record);
    }

    public bool TryGet(HistoryEventId id, out HistoryRecord record) =>
        _byId.TryGetValue(id.Value, out record!);

    public IReadOnlyList<HistoryRecord> GetRecent(int count)
    {
        if (count <= 0 || _order.Count == 0)
            return [];
        var start = Math.Max(0, _order.Count - count);
        return _order.Skip(start).ToArray();
    }

    public IReadOnlyList<HistoryRecord> ForSubject(ulong subject) =>
        _order.Where(r => r.Subject == subject || r.Secondary == subject).ToArray();

    public IReadOnlyList<HistoryRecord> ForCharacter(CharacterId id) => ForSubject(id.Value);

    public IReadOnlyList<HistoryRecord> ForSettlement(SettlementId id) => ForSubject(id.Value);

    public IReadOnlyList<HistoryRecord> ForFaction(FactionId id) => ForSubject(id.Value);

    public IReadOnlyList<HistoryRecord> ForMilitaryUnit(MilitaryUnitId id) => ForSubject(id.Value);

    public IReadOnlyList<HistoryRecord> ForHousehold(HouseholdId id) => ForSubject(id.Value);

    public HistoryRecord this[int index] => _order[index];
}

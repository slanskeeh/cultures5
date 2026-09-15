using Cultures.Core.Ids;

namespace Cultures.Settlement;

/// <summary>
/// Settlement instances keyed by SettlementId. Not a rule god-object.
/// </summary>
public sealed class SettlementDirectory
{
    private readonly Dictionary<ulong, SettlementState> _byId = new();
    private readonly List<SettlementState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<SettlementState> All => _order;

    public IEnumerable<SettlementState> Active => _order.Where(s => s.IsActive);

    public void Add(SettlementState settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        if (!_byId.TryAdd(settlement.Id.Value, settlement))
            throw new InvalidOperationException($"Duplicate settlement {settlement.Id}.");
        _order.Add(settlement);
    }

    public bool TryGet(SettlementId id, out SettlementState settlement) =>
        _byId.TryGetValue(id.Value, out settlement!);

    public SettlementState this[int index] => _order[index];
}

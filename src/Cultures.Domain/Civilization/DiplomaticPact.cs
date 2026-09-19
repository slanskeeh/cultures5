using Cultures.Core.Ids;

namespace Cultures.Civilization;

public enum DiplomaticPactKind : byte
{
    Trade = 1,
    NonAggression = 2,
    Alliance = 3
}

/// <summary>
/// Explicit pact. Hostile is still not war. A pact does not move armies or goods.
/// </summary>
public sealed class DiplomaticPact
{
    public DiplomaticPact(DiplomaticPactId id, FactionId lower, FactionId higher, DiplomaticPactKind kind, ulong formedTick)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Pact id is required.", nameof(id));
        if (lower.Value >= higher.Value)
            throw new ArgumentException("Pact pair must be normalized lower < higher.");

        Id = id;
        Lower = lower;
        Higher = higher;
        Kind = kind;
        FormedTick = formedTick;
    }

    public DiplomaticPactId Id { get; }
    public FactionId Lower { get; }
    public FactionId Higher { get; }
    public DiplomaticPactKind Kind { get; }
    public ulong FormedTick { get; }
}

public sealed class DiplomaticPactDirectory
{
    private readonly Dictionary<ulong, DiplomaticPact> _byId = new();
    private readonly Dictionary<(ulong A, ulong B, byte Kind), DiplomaticPact> _byPair = new();
    private readonly List<DiplomaticPact> _order = [];

    public int Count => _order.Count;
    public IReadOnlyList<DiplomaticPact> All => _order;

    public void Add(DiplomaticPact pact)
    {
        ArgumentNullException.ThrowIfNull(pact);
        if (!_byId.TryAdd(pact.Id.Value, pact))
            throw new InvalidOperationException($"Duplicate pact {pact.Id}.");
        _byPair[(pact.Lower.Value, pact.Higher.Value, (byte)pact.Kind)] = pact;
        _order.Add(pact);
    }

    public bool TryGet(DiplomaticPactId id, out DiplomaticPact pact) =>
        _byId.TryGetValue(id.Value, out pact!);

    public bool Has(FactionId a, FactionId b, DiplomaticPactKind kind)
    {
        var (lower, higher) = FactionRelationDirectory.Normalize(a, b);
        return _byPair.ContainsKey((lower.Value, higher.Value, (byte)kind));
    }

    public bool Remove(DiplomaticPactId id)
    {
        if (!_byId.Remove(id.Value, out var pact))
            return false;
        _byPair.Remove((pact.Lower.Value, pact.Higher.Value, (byte)pact.Kind));
        _order.Remove(pact);
        return true;
    }

    public IReadOnlyList<DiplomaticPact> ForPair(FactionId a, FactionId b)
    {
        var (lower, higher) = FactionRelationDirectory.Normalize(a, b);
        return _order.Where(p => p.Lower == lower && p.Higher == higher).ToArray();
    }
}

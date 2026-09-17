using Cultures.Core.Ids;

namespace Cultures.Civilization;

public enum FactionRelationStance : byte
{
    Neutral = 0,
    Friendly = 1,
    Hostile = 2
}

/// <summary>
/// Symmetric pair of factions. Missing directory entries are Neutral.
/// </summary>
public sealed class FactionRelation
{
    public FactionRelation(FactionId lower, FactionId higher, FactionRelationStance stance)
    {
        if (lower.Value >= higher.Value)
            throw new ArgumentException("Relation pair must be normalized lower < higher.");

        Lower = lower;
        Higher = higher;
        Stance = stance;
    }

    public FactionId Lower { get; }
    public FactionId Higher { get; }
    public FactionRelationStance Stance { get; set; }

    public FactionRelationSnapshot Snapshot() => new(Lower, Higher, Stance);
}

public readonly record struct FactionRelationSnapshot(
    FactionId Lower,
    FactionId Higher,
    FactionRelationStance Stance);

public sealed class FactionRelationDirectory
{
    private readonly Dictionary<(ulong A, ulong B), FactionRelation> _byPair = new();
    private readonly List<FactionRelation> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<FactionRelation> All => _order;

    public static (FactionId Lower, FactionId Higher) Normalize(FactionId a, FactionId b) =>
        a.Value < b.Value ? (a, b) : (b, a);

    public FactionRelation GetOrCreate(FactionId a, FactionId b)
    {
        var (lower, higher) = Normalize(a, b);
        if (_byPair.TryGetValue((lower.Value, higher.Value), out var existing))
            return existing;

        var relation = new FactionRelation(lower, higher, FactionRelationStance.Neutral);
        _byPair[(lower.Value, higher.Value)] = relation;
        _order.Add(relation);
        return relation;
    }

    public bool TryGet(FactionId a, FactionId b, out FactionRelation relation)
    {
        var (lower, higher) = Normalize(a, b);
        return _byPair.TryGetValue((lower.Value, higher.Value), out relation!);
    }

    public void Remove(FactionId a, FactionId b)
    {
        var (lower, higher) = Normalize(a, b);
        if (!_byPair.Remove((lower.Value, higher.Value), out var relation))
            return;
        _order.Remove(relation);
    }
}

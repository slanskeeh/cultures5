using Cultures.Core.Ids;

namespace Cultures.Civilization;

public enum FactionLifecycle : byte
{
    Active = 0,
    Dormant = 1
}

/// <summary>
/// Organized social group. Not a settlement and not a culture.
/// </summary>
public sealed class FactionState
{
    public FactionState(FactionId id, string name, CultureId culture, ulong foundedTick)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Faction id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Faction name is required.", nameof(name));
        if (!culture.IsAssigned)
            throw new ArgumentException("Faction requires a culture.", nameof(culture));

        Id = id;
        Name = name;
        Culture = culture;
        FoundedTick = foundedTick;
        HomeSettlement = SettlementId.None;
        Lifecycle = FactionLifecycle.Active;
    }

    public FactionId Id { get; }
    public string Name { get; }
    public CultureId Culture { get; }
    public ulong FoundedTick { get; }
    public SettlementId HomeSettlement { get; set; }
    public FactionLifecycle Lifecycle { get; set; }

    public FactionSnapshot Snapshot() => new(
        Id,
        Name,
        Culture,
        FoundedTick,
        HomeSettlement,
        Lifecycle);
}

public readonly record struct FactionSnapshot(
    FactionId Id,
    string Name,
    CultureId Culture,
    ulong FoundedTick,
    SettlementId HomeSettlement,
    FactionLifecycle Lifecycle);

public sealed class FactionDirectory
{
    private readonly Dictionary<ulong, FactionState> _byId = new();
    private readonly List<FactionState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<FactionState> All => _order;

    public void Add(FactionState faction)
    {
        ArgumentNullException.ThrowIfNull(faction);
        if (!_byId.TryAdd(faction.Id.Value, faction))
            throw new InvalidOperationException($"Duplicate faction {faction.Id}.");
        _order.Add(faction);
    }

    public bool TryGet(FactionId id, out FactionState faction) =>
        _byId.TryGetValue(id.Value, out faction!);

    public FactionState this[int index] => _order[index];
}

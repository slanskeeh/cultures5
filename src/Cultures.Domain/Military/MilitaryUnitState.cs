using Cultures.Core.Ids;

namespace Cultures.Military;

public enum MilitaryUnitLifecycle : byte
{
    Active = 0,
    Disbanded = 1
}

/// <summary>
/// Faction-owned force identity. Not a culture, political group, or war.
/// Phase 12 stores no combat stats, equipment, or world occupancy.
/// </summary>
public sealed class MilitaryUnitState
{
    public MilitaryUnitState(MilitaryUnitId id, FactionId faction, string name, ulong generationSalt)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Military unit id is required.", nameof(id));
        if (!faction.IsAssigned)
            throw new ArgumentException("Military unit requires a faction.", nameof(faction));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Military unit name is required.", nameof(name));

        Id = id;
        Faction = faction;
        Name = name;
        GenerationSalt = generationSalt;
        Lifecycle = MilitaryUnitLifecycle.Active;
    }

    public MilitaryUnitId Id { get; }
    public FactionId Faction { get; }
    public string Name { get; }
    public ulong GenerationSalt { get; }
    public MilitaryUnitLifecycle Lifecycle { get; set; }

    public bool IsActive => Lifecycle == MilitaryUnitLifecycle.Active;

    public MilitaryUnitSnapshot Snapshot() => new(Id, Faction, Name, GenerationSalt, Lifecycle);
}

public readonly record struct MilitaryUnitSnapshot(
    MilitaryUnitId Id,
    FactionId Faction,
    string Name,
    ulong GenerationSalt,
    MilitaryUnitLifecycle Lifecycle);

/// <summary>
/// Sparse unit directory keyed by id and owning faction. Disbanded units remain listed.
/// </summary>
public sealed class MilitaryUnitDirectory
{
    private readonly Dictionary<ulong, MilitaryUnitState> _byId = new();
    private readonly Dictionary<ulong, List<MilitaryUnitState>> _byFaction = new();
    private readonly List<MilitaryUnitState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<MilitaryUnitState> All => _order;

    public void Add(MilitaryUnitState unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!_byId.TryAdd(unit.Id.Value, unit))
            throw new InvalidOperationException($"Duplicate military unit {unit.Id}.");
        _order.Add(unit);
        if (!_byFaction.TryGetValue(unit.Faction.Value, out var list))
        {
            list = [];
            _byFaction[unit.Faction.Value] = list;
        }

        list.Add(unit);
    }

    public bool TryGet(MilitaryUnitId id, out MilitaryUnitState unit) =>
        _byId.TryGetValue(id.Value, out unit!);

    public IReadOnlyList<MilitaryUnitState> ForFaction(FactionId faction) =>
        _byFaction.TryGetValue(faction.Value, out var list) ? list : [];

    public MilitaryUnitState this[int index] => _order[index];
}

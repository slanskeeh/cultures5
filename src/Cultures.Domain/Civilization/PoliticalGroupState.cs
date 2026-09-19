using Cultures.Core.Ids;

namespace Cultures.Civilization;

/// <summary>
/// Compact political preference data. Not behavior and not a historical class system.
/// </summary>
public readonly record struct PoliticalGroupTraits(byte Tradition, byte Authority, byte Commerce);

public readonly record struct InternalStability(int Value)
{
    public static InternalStability Default => new(PoliticsRules.StabilityDefault);

    public InternalStabilityBand Band => Value <= PoliticsRules.UnstableCeiling
        ? InternalStabilityBand.Unstable
        : Value <= PoliticsRules.TenseCeiling
            ? InternalStabilityBand.Tense
            : InternalStabilityBand.Stable;

    public static bool IsInRange(int value) =>
        value is >= PoliticsRules.StabilityMin and <= PoliticsRules.StabilityMax;
}

public enum InternalStabilityBand : byte
{
    Unstable = 0,
    Tense = 1,
    Stable = 2
}

/// <summary>
/// Faction-local interest group. Not a culture, faction, or settlement.
/// </summary>
public sealed class PoliticalGroupState
{
    public PoliticalGroupState(
        PoliticalGroupId id,
        FactionId faction,
        string name,
        PoliticalGroupTraits traits,
        ulong generationSalt)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Political group id is required.", nameof(id));
        if (!faction.IsAssigned)
            throw new ArgumentException("Political group requires a faction.", nameof(faction));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Political group name is required.", nameof(name));

        Id = id;
        Faction = faction;
        Name = name;
        Traits = traits;
        GenerationSalt = generationSalt;
        Influence = PoliticsRules.InfluenceMin;
    }

    public PoliticalGroupId Id { get; }
    public FactionId Faction { get; }
    public string Name { get; }
    public PoliticalGroupTraits Traits { get; }
    public ulong GenerationSalt { get; }
    public int Influence { get; set; }

    public PoliticalGroupSnapshot Snapshot() => new(Id, Faction, Name, Traits, GenerationSalt, Influence);
}

public readonly record struct PoliticalGroupSnapshot(
    PoliticalGroupId Id,
    FactionId Faction,
    string Name,
    PoliticalGroupTraits Traits,
    ulong GenerationSalt,
    int Influence);

public sealed class PoliticalGroupDirectory
{
    private readonly Dictionary<ulong, PoliticalGroupState> _byId = new();
    private readonly Dictionary<ulong, List<PoliticalGroupState>> _byFaction = new();
    private readonly List<PoliticalGroupState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<PoliticalGroupState> All => _order;

    public void Add(PoliticalGroupState group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!_byId.TryAdd(group.Id.Value, group))
            throw new InvalidOperationException($"Duplicate political group {group.Id}.");
        _order.Add(group);
        if (!_byFaction.TryGetValue(group.Faction.Value, out var list))
        {
            list = [];
            _byFaction[group.Faction.Value] = list;
        }

        list.Add(group);
    }

    public bool TryGet(PoliticalGroupId id, out PoliticalGroupState group) =>
        _byId.TryGetValue(id.Value, out group!);

    public IReadOnlyList<PoliticalGroupState> ForFaction(FactionId faction) =>
        _byFaction.TryGetValue(faction.Value, out var list) ? list : [];

    public PoliticalGroupState this[int index] => _order[index];
}

/// <summary>
/// Sparse faction-level stability. Missing entries are default 50.
/// </summary>
public sealed class InternalPoliticsDirectory
{
    private readonly Dictionary<ulong, InternalStability> _byFaction = new();

    public int Count => _byFaction.Count;

    public InternalStability Of(FactionId faction) =>
        _byFaction.TryGetValue(faction.Value, out var stability) ? stability : InternalStability.Default;

    public void Set(FactionId faction, InternalStability stability)
    {
        if (stability.Value == PoliticsRules.StabilityDefault)
            _byFaction.Remove(faction.Value);
        else
            _byFaction[faction.Value] = stability;
    }

    public IEnumerable<(FactionId Faction, InternalStability Stability)> Entries
    {
        get
        {
            foreach (var pair in _byFaction)
                yield return (new FactionId(pair.Key), pair.Value);
        }
    }
}

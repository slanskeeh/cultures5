using Cultures.Core.Ids;

namespace Cultures.Civilization;

/// <summary>
/// Compact cultural identity. Traits are data, not behavior.
/// </summary>
public readonly record struct CultureTraits(
    byte LanguageFamily,
    byte NamingStyle,
    byte ArchitectureTendency,
    byte FoodPreference,
    byte SocialCustom);

/// <summary>
/// Shared cultural identity. Not a faction and not a settlement.
/// </summary>
public sealed class CultureState
{
    public CultureState(CultureId id, string name, CultureTraits traits, ulong generationSalt)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Culture id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Culture name is required.", nameof(name));

        Id = id;
        Name = name;
        Traits = traits;
        GenerationSalt = generationSalt;
    }

    public CultureId Id { get; }
    public string Name { get; }
    public CultureTraits Traits { get; }
    public ulong GenerationSalt { get; }

    public CultureSnapshot Snapshot() => new(Id, Name, Traits, GenerationSalt);
}

public readonly record struct CultureSnapshot(
    CultureId Id,
    string Name,
    CultureTraits Traits,
    ulong GenerationSalt);

public sealed class CultureDirectory
{
    private readonly Dictionary<ulong, CultureState> _byId = new();
    private readonly List<CultureState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<CultureState> All => _order;

    public void Add(CultureState culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!_byId.TryAdd(culture.Id.Value, culture))
            throw new InvalidOperationException($"Duplicate culture {culture.Id}.");
        _order.Add(culture);
    }

    public bool TryGet(CultureId id, out CultureState culture) =>
        _byId.TryGetValue(id.Value, out culture!);

    public CultureState this[int index] => _order[index];
}

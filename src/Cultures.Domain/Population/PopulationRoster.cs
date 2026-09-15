using Cultures.Core.Ids;

namespace Cultures.Population;

/// <summary>
/// Population keyed by stable CharacterId, not array index.
/// </summary>
public sealed class PopulationRoster
{
    private readonly Dictionary<ulong, CharacterState> _byId = new();
    private readonly List<CharacterState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<CharacterState> All => _order;

    public IEnumerable<CharacterState> Alive => _order.Where(c => c.IsAlive);

    public void Add(CharacterState character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (!_byId.TryAdd(character.Id.Value, character))
            throw new InvalidOperationException($"Duplicate character {character.Id}.");
        _order.Add(character);
    }

    public bool TryGet(CharacterId id, out CharacterState character) =>
        _byId.TryGetValue(id.Value, out character!);

    public CharacterState this[int index] => _order[index];
}

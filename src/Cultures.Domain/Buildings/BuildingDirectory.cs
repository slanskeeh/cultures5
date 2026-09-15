using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Buildings;

/// <summary>
/// Building instances keyed by BuildingId. Not a rule god-object.
/// </summary>
public sealed class BuildingDirectory
{
    private readonly Dictionary<ulong, BuildingState> _byId = new();
    private readonly List<BuildingState> _order = new();

    public int Count => _order.Count;

    public IReadOnlyList<BuildingState> All => _order;

    public void Add(BuildingState building)
    {
        ArgumentNullException.ThrowIfNull(building);
        if (!_byId.TryAdd(building.Id.Value, building))
            throw new InvalidOperationException($"Duplicate building {building.Id}.");
        _order.Add(building);
    }

    public bool TryGet(BuildingId id, out BuildingState building) =>
        _byId.TryGetValue(id.Value, out building!);

    public bool Remove(BuildingId id)
    {
        if (!_byId.Remove(id.Value, out var building))
            return false;
        _order.Remove(building);
        return true;
    }

    public BuildingState? FindAt(LogicalGridCoordinate cell)
    {
        foreach (var building in _order)
        {
            foreach (var occupied in building.FootprintCells)
            {
                if (occupied.Equals(cell))
                    return building;
            }
        }

        return null;
    }

    public BuildingState this[int index] => _order[index];
}

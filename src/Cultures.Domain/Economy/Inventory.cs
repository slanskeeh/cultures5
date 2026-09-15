namespace Cultures.Economy;

/// <summary>
/// Integer resource collection. Capacity is total items, or null for unlimited.
/// </summary>
public sealed class Inventory
{
    private readonly Dictionary<ResourceType, int> _amounts = new();

    public Inventory(int? capacity = null)
    {
        if (capacity is < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    public int? Capacity { get; }

    public int TotalQuantity
    {
        get
        {
            var total = 0;
            foreach (var amount in _amounts.Values)
                total += amount;
            return total;
        }
    }

    public int GetQuantity(ResourceType type) => _amounts.GetValueOrDefault(type);

    public bool Has(ResourceType type, int quantity)
    {
        if (quantity <= 0)
            return false;
        return GetQuantity(type) >= quantity;
    }

    public bool TryAdd(ResourceType type, int quantity)
    {
        if (quantity <= 0)
            return false;
        if (Capacity is int cap && TotalQuantity + quantity > cap)
            return false;

        _amounts[type] = GetQuantity(type) + quantity;
        return true;
    }

    public bool TryAdd(ResourceStack stack) => TryAdd(stack.Type, stack.Quantity);

    public bool TryRemove(ResourceType type, int quantity)
    {
        if (quantity <= 0)
            return false;
        var current = GetQuantity(type);
        if (current < quantity)
            return false;

        var remaining = current - quantity;
        if (remaining == 0)
            _amounts.Remove(type);
        else
            _amounts[type] = remaining;
        return true;
    }

    public bool TryRemove(ResourceStack stack) => TryRemove(stack.Type, stack.Quantity);

    public bool TryTransferTo(Inventory destination, ResourceType type, int quantity)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!Has(type, quantity))
            return false;
        if (!destination.TryAdd(type, quantity))
            return false;
        if (!TryRemove(type, quantity))
        {
            destination.TryRemove(type, quantity);
            return false;
        }

        return true;
    }

    public void TransferAllPossibleTo(Inventory destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        foreach (var stack in Enumerate().ToArray())
        {
            for (var i = 0; i < stack.Quantity; i++)
            {
                if (!TryTransferTo(destination, stack.Type, 1))
                    break;
            }
        }
    }

    public IEnumerable<ResourceStack> Enumerate()
    {
        foreach (var pair in _amounts.OrderBy(static kv => kv.Key))
            yield return new ResourceStack(pair.Key, pair.Value);
    }
}

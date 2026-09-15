namespace Cultures.Economy;

/// <summary>
/// Initial development resources, not the final catalogue.
/// </summary>
public enum ResourceType : byte
{
    Food = 1,
    Wood = 2,
    Stone = 3
}

public readonly record struct ResourceStack
{
    public ResourceStack(ResourceType type, int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Resource quantity cannot be negative.");

        Type = type;
        Quantity = quantity;
    }

    public ResourceType Type { get; }
    public int Quantity { get; }

    public override string ToString() => $"{Type} x{Quantity}";
}

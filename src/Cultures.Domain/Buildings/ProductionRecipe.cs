using Cultures.Economy;

namespace Cultures.Buildings;

/// <summary>
/// Data-driven recipe. Duration and outputs are bases; resolvers may modify them.
/// </summary>
public sealed class ProductionRecipe
{
    public ProductionRecipe(
        RecipeId id,
        int durationTicks,
        IReadOnlyList<ResourceStack>? inputs = null,
        IReadOnlyList<ResourceStack>? outputs = null)
    {
        if (durationTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationTicks));

        Id = id;
        DurationTicks = durationTicks;
        Inputs = inputs ?? Array.Empty<ResourceStack>();
        Outputs = outputs ?? Array.Empty<ResourceStack>();
    }

    public RecipeId Id { get; }
    public int DurationTicks { get; }
    public IReadOnlyList<ResourceStack> Inputs { get; }
    public IReadOnlyList<ResourceStack> Outputs { get; }

    public bool CanExecute(Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        foreach (var input in Inputs)
        {
            if (!inventory.Has(input.Type, input.Quantity))
                return false;
        }

        return true;
    }

    public bool TryExecute(Inventory inventory, IReadOnlyList<ResourceStack> outputs)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(outputs);
        if (!CanExecute(inventory))
            return false;

        foreach (var input in Inputs)
            inventory.TryRemove(input);

        var written = new List<ResourceStack>();
        foreach (var output in outputs)
        {
            if (output.Quantity <= 0)
                continue;
            if (inventory.TryAdd(output))
            {
                written.Add(output);
                continue;
            }

            foreach (var added in written)
                inventory.TryRemove(added);
            foreach (var input in Inputs)
                inventory.TryAdd(input);
            return false;
        }

        return true;
    }
}

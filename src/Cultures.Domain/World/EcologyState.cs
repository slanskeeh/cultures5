using Cultures.Core.Ids;
using Cultures.Economy;

namespace Cultures.World;

public enum WildlifeSpecies : byte
{
    Deer = 1,
    Sheep = 2,
    Boar = 3,
    Birds = 4
}

/// <summary>
/// Persistent extractable natural stock at a world cell. Not building inventory.
/// </summary>
public sealed class ResourceDeposit
{
    public ResourceDeposit(ResourceDepositId id, LogicalGridCoordinate cell, ResourceType resource, int stock, int capacity)
    {
        if (!id.IsAssigned)
            throw new ArgumentException("Deposit id is required.", nameof(id));
        if (stock < 0 || capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(stock));

        Id = id;
        Cell = cell;
        Resource = resource;
        Stock = stock;
        Capacity = Math.Max(capacity, stock);
    }

    public ResourceDepositId Id { get; }
    public LogicalGridCoordinate Cell { get; }
    public ResourceType Resource { get; }
    public int Stock { get; set; }
    public int Capacity { get; }

    public ResourceDepositSnapshot Snapshot() => new(Id, Cell, Resource, Stock, Capacity);
}

public readonly record struct ResourceDepositSnapshot(
    ResourceDepositId Id,
    LogicalGridCoordinate Cell,
    ResourceType Resource,
    int Stock,
    int Capacity);

public sealed class ResourceDepositDirectory
{
    private readonly Dictionary<ulong, ResourceDeposit> _byId = new();
    private readonly Dictionary<(int X, int Y, byte Resource), ResourceDeposit> _byCell = new();
    private readonly List<ResourceDeposit> _order = new();

    public int Count => _order.Count;
    public IReadOnlyList<ResourceDeposit> All => _order;

    public void Add(ResourceDeposit deposit)
    {
        ArgumentNullException.ThrowIfNull(deposit);
        if (!_byId.TryAdd(deposit.Id.Value, deposit))
            throw new InvalidOperationException($"Duplicate deposit {deposit.Id}.");
        _byCell[(deposit.Cell.X, deposit.Cell.Y, (byte)deposit.Resource)] = deposit;
        _order.Add(deposit);
    }

    public bool TryGet(ResourceDepositId id, out ResourceDeposit deposit) =>
        _byId.TryGetValue(id.Value, out deposit!);

    public bool TryGetAt(LogicalGridCoordinate cell, ResourceType resource, out ResourceDeposit deposit) =>
        _byCell.TryGetValue((cell.X, cell.Y, (byte)resource), out deposit!);

    public ResourceDeposit this[int index] => _order[index];
}

public sealed class WildlifePopulation
{
    public WildlifePopulation(ChunkCoordinate chunk)
    {
        Chunk = chunk;
    }

    public ChunkCoordinate Chunk { get; }
    public int Deer { get; set; }
    public int Sheep { get; set; }
    public int Boar { get; set; }
    public int Birds { get; set; }

    public int Total => Deer + Sheep + Boar + Birds;

    public WildlifeSnapshot Snapshot() => new(Chunk, Deer, Sheep, Boar, Birds);
}

public readonly record struct WildlifeSnapshot(ChunkCoordinate Chunk, int Deer, int Sheep, int Boar, int Birds);

public sealed class WildlifeDirectory
{
    private readonly Dictionary<(int X, int Y), WildlifePopulation> _byChunk = new();
    private readonly List<WildlifePopulation> _order = new();

    public int Count => _order.Count;
    public IReadOnlyList<WildlifePopulation> All => _order;

    public WildlifePopulation GetOrCreate(ChunkCoordinate chunk)
    {
        if (_byChunk.TryGetValue((chunk.X, chunk.Y), out var existing))
            return existing;
        var created = new WildlifePopulation(chunk);
        _byChunk[(chunk.X, chunk.Y)] = created;
        _order.Add(created);
        return created;
    }

    public bool TryGet(ChunkCoordinate chunk, out WildlifePopulation population) =>
        _byChunk.TryGetValue((chunk.X, chunk.Y), out population!);
}

public static class EcologyRules
{
    public const int DepositCapacity = 8;
    public const int RegenerationIntervalTicks = 48;
    public const int WildlifeIntervalTicks = 96;
    public const int ExtractAmount = 1;

    public static float Fertility(GeneratedTerrain terrain)
    {
        if (terrain.IsWater)
            return 0f;
        var moisture = terrain.Climate.Moisture;
        var temperature = terrain.Climate.Temperature;
        var elevationPenalty = terrain.Elevation > 0.72f ? 0.35f : 0f;
        var biome = terrain.Biome switch
        {
            BiomeId.TemperateLand => 0.85f,
            BiomeId.Forest => 0.70f,
            BiomeId.Tundra => 0.35f,
            BiomeId.Desert => 0.18f,
            BiomeId.Highland => 0.28f,
            BiomeId.Ice => 0.05f,
            BiomeId.Swamp => 0.90f,
            BiomeId.Savanna => 0.48f,
            BiomeId.Taiga => 0.55f,
            _ => 0.40f
        };
        var riverBonus = terrain.HasRiver ? 0.18f : 0f;
        return Math.Clamp(biome * (0.45f + moisture * 0.40f + temperature * 0.15f) - elevationPenalty + riverBonus, 0f, 1f);
    }

    public static ResourceType? Suitability(BiomeId biome) => biome switch
    {
        BiomeId.Forest => ResourceType.Wood,
        BiomeId.Taiga => ResourceType.Wood,
        BiomeId.Highland => ResourceType.Stone,
        BiomeId.TemperateLand => ResourceType.Clay,
        BiomeId.Swamp => ResourceType.Clay,
        BiomeId.Savanna => ResourceType.Mineral,
        BiomeId.Desert => ResourceType.Mineral,
        _ => null
    };
}

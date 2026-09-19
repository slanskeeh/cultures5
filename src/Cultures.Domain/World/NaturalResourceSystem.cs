using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Population;

namespace Cultures.World;

/// <summary>
/// Sparse natural stocks and wildlife aggregates. Regenerates without presentation.
/// </summary>
public sealed class NaturalResourceSystem
{
    private int _regenTicks;
    private int _wildlifeTicks;

    public NaturalResourceSystem(ulong worldSeed, LogicalWorld world, EntityIdFactory ids)
    {
        WorldSeed = worldSeed;
        World = world ?? throw new ArgumentNullException(nameof(world));
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Deposits = new ResourceDepositDirectory();
        Wildlife = new WildlifeDirectory();
    }

    public ulong WorldSeed { get; }
    public LogicalWorld World { get; }
    public EntityIdFactory Ids { get; }
    public ResourceDepositDirectory Deposits { get; }
    public WildlifeDirectory Wildlife { get; }

    public void EnsureCell(LogicalGridCoordinate cell)
    {
        var terrain = World.Grid.GetCell(cell);
        if (terrain.IsWater)
            return;

        TrySeedDeposit(cell, terrain.Biome, EcologyRules.Suitability(terrain.Biome));
        if (terrain.Biome == BiomeId.Forest || terrain.Biome == BiomeId.Taiga)
            TrySeedDeposit(cell, terrain.Biome, ResourceType.WildBerries);
        if (terrain.HasRiver || terrain.Biome == BiomeId.TemperateLand || terrain.Biome == BiomeId.Swamp)
            TrySeedDeposit(cell, terrain.Biome, ResourceType.Clay);
        if (terrain.Biome == BiomeId.Highland)
            TrySeedDeposit(cell, terrain.Biome, ResourceType.Metal);

        var chunk = World.Chunks.ToAddress(cell).Chunk;
        EnsureWildlife(chunk);
    }

    public bool TryExtract(LogicalGridCoordinate cell, ResourceType resource, int amount)
    {
        EnsureCell(cell);
        if (!Deposits.TryGetAt(cell, resource, out var deposit))
            return resource is ResourceType.Food or ResourceType.WildBerries;
        if (deposit.Stock < amount)
            return false;
        deposit.Stock -= amount;
        return true;
    }

    public void OnProduced(BuildingState building, ProductionEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(building);
        EnsureCell(building.Origin);
        foreach (var output in evaluation.Outputs)
        {
            if (output.Type is ResourceType.Wood or ResourceType.Stone or ResourceType.Clay
                or ResourceType.Metal or ResourceType.Mineral or ResourceType.WildBerries)
            {
                TryExtract(building.Origin, output.Type, EcologyRules.ExtractAmount);
            }
        }
    }

    public bool TryHunt(LogicalGridCoordinate cell, out WildlifeSpecies species, out string error)
    {
        species = default;
        error = "Hunt failed.";
        EnsureCell(cell);
        var chunk = World.Chunks.ToAddress(cell).Chunk;
        if (!Wildlife.TryGet(chunk, out var wildlife) || wildlife.Total <= 0)
        {
            error = "No wildlife in this chunk.";
            return false;
        }

        if (wildlife.Deer > 0)
        {
            wildlife.Deer--;
            species = WildlifeSpecies.Deer;
            return true;
        }

        if (wildlife.Boar > 0)
        {
            wildlife.Boar--;
            species = WildlifeSpecies.Boar;
            return true;
        }

        if (wildlife.Sheep > 0)
        {
            wildlife.Sheep--;
            species = WildlifeSpecies.Sheep;
            return true;
        }

        wildlife.Birds--;
        species = WildlifeSpecies.Birds;
        return true;
    }

    public void Tick()
    {
        _regenTicks++;
        _wildlifeTicks++;
        if (_regenTicks >= EcologyRules.RegenerationIntervalTicks)
        {
            _regenTicks = 0;
            foreach (var deposit in Deposits.All)
            {
                if (deposit.Stock < deposit.Capacity)
                    deposit.Stock++;
            }
        }

        if (_wildlifeTicks < EcologyRules.WildlifeIntervalTicks)
            return;
        _wildlifeTicks = 0;
        foreach (var wildlife in Wildlife.All)
        {
            wildlife.Deer = Math.Min(12, wildlife.Deer + (wildlife.Deer < 2 ? 1 : 0));
            wildlife.Sheep = Math.Min(10, wildlife.Sheep + (wildlife.Sheep < 2 ? 1 : 0));
            wildlife.Boar = Math.Min(8, wildlife.Boar + (wildlife.Boar < 1 ? 1 : 0));
            wildlife.Birds = Math.Min(16, wildlife.Birds + (wildlife.Birds < 4 ? 1 : 0));
        }
    }

    private void TrySeedDeposit(LogicalGridCoordinate cell, BiomeId biome, ResourceType? resource)
    {
        if (resource is not { } type)
            return;
        if (Deposits.TryGetAt(cell, type, out _))
            return;

        var hash = Mix(WorldSeed, (ulong)(uint)cell.X, (ulong)(uint)cell.Y, (ulong)type);
        var stock = 2 + (int)(hash % (ulong)(EcologyRules.DepositCapacity - 1));
        if (biome == BiomeId.Ice || biome == BiomeId.Ocean)
            return;
        Deposits.Add(new ResourceDeposit(Ids.NextResourceDeposit(), cell, type, stock, EcologyRules.DepositCapacity));
    }

    private void EnsureWildlife(ChunkCoordinate chunk)
    {
        if (Wildlife.TryGet(chunk, out _))
            return;
        var hash = Mix(WorldSeed, 0xD1B54A32D192ED03UL, (ulong)(uint)chunk.X, (ulong)(uint)chunk.Y);
        var wildlife = Wildlife.GetOrCreate(chunk);
        wildlife.Deer = (int)(hash % 4);
        wildlife.Sheep = (int)((hash >> 8) % 3);
        wildlife.Boar = (int)((hash >> 16) % 2);
        wildlife.Birds = 2 + (int)((hash >> 24) % 5);
    }

    private static ulong Mix(ulong a, ulong b, ulong c, ulong d)
    {
        unchecked
        {
            var h = a + 0x9E3779B97F4A7C15UL;
            h = (h ^ b) * 0xBF58476D1CE4E5B9UL;
            h = (h ^ c) * 0x94D049BB133111EBUL;
            h = (h ^ d) * 0x9E3779B97F4A7C15UL;
            return h;
        }
    }
}

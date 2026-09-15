using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Settlement;

public enum SettlementLifecycle : byte
{
    Emerging = 0,
    Established = 1,
    Declining = 2,
    Abandoned = 3
}

/// <summary>
/// Persistent settlement identity and lifecycle. Population is derived from characters.
/// </summary>
public sealed class SettlementState
{
    public SettlementState(SettlementId id, string nameKey, ulong foundedTick)
    {
        if (string.IsNullOrWhiteSpace(nameKey))
            throw new ArgumentException("Settlement name key is required.", nameof(nameKey));

        Id = id;
        NameKey = nameKey;
        FoundedTick = foundedTick;
        Culture = CultureId.Neutral;
        Leader = CharacterId.None;
        Lifecycle = SettlementLifecycle.Emerging;
        Core = default;
        Statistics = new SettlementStatistics();
    }

    public SettlementId Id { get; }
    public string NameKey { get; }
    public ulong FoundedTick { get; }
    public CultureId Culture { get; set; }
    public CharacterId Leader { get; set; }
    public SettlementLifecycle Lifecycle { get; set; }
    public LogicalGridCoordinate Core { get; set; }
    public int PresenceEvals { get; set; }
    public int UnmatchedEvals { get; set; }
    public SettlementStatistics Statistics { get; }

    public bool IsAbandoned => Lifecycle == SettlementLifecycle.Abandoned;
    public bool IsActive => Lifecycle is SettlementLifecycle.Emerging
        or SettlementLifecycle.Established
        or SettlementLifecycle.Declining;

    public SettlementSnapshot Snapshot() => new(
        Id,
        NameKey,
        Culture.Value,
        Leader.Value,
        Lifecycle,
        Core,
        PresenceEvals,
        UnmatchedEvals,
        Statistics.Population,
        Statistics.Adults,
        Statistics.Children,
        Statistics.Infants,
        Statistics.Elders,
        Statistics.ActiveBuildings,
        Statistics.Shelters,
        Statistics.StorageBuildings,
        Statistics.FoodStored,
        Statistics.Workers,
        Statistics.UnemployedAdults,
        Statistics.EstimatedFoodProduction);
}

public sealed class SettlementStatistics
{
    public int Population { get; set; }
    public int Infants { get; set; }
    public int Children { get; set; }
    public int Adolescents { get; set; }
    public int Adults { get; set; }
    public int Elders { get; set; }
    public int ActiveBuildings { get; set; }
    public int Shelters { get; set; }
    public int StorageBuildings { get; set; }
    public int FoodStored { get; set; }
    public int Workers { get; set; }
    public int UnemployedAdults { get; set; }
    public int EstimatedFoodProduction { get; set; }
    public int ShelterCapacity { get; set; }

    public void Clear()
    {
        Population = 0;
        Infants = 0;
        Children = 0;
        Adolescents = 0;
        Adults = 0;
        Elders = 0;
        ActiveBuildings = 0;
        Shelters = 0;
        StorageBuildings = 0;
        FoodStored = 0;
        Workers = 0;
        UnemployedAdults = 0;
        EstimatedFoodProduction = 0;
        ShelterCapacity = 0;
    }
}

public readonly record struct SettlementSnapshot(
    SettlementId Id,
    string NameKey,
    ulong Culture,
    ulong Leader,
    SettlementLifecycle Lifecycle,
    LogicalGridCoordinate Core,
    int PresenceEvals,
    int UnmatchedEvals,
    int Population,
    int Adults,
    int Children,
    int Infants,
    int Elders,
    int ActiveBuildings,
    int Shelters,
    int StorageBuildings,
    int FoodStored,
    int Workers,
    int UnemployedAdults,
    int EstimatedFoodProduction);

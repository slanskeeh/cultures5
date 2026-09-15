using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

/// <summary>
/// Architectural seam for future migration. Not gameplay.
/// </summary>
public sealed class MigrationGroup
{
    public MigrationGroup(
        MigrationGroupId id,
        SettlementId origin,
        SettlementId destination,
        int population,
        CultureId culture)
    {
        if (population < 0)
            throw new ArgumentOutOfRangeException(nameof(population));

        Id = id;
        Origin = origin;
        Destination = destination;
        Population = population;
        Culture = culture;
    }

    public MigrationGroupId Id { get; }
    public SettlementId Origin { get; set; }
    public SettlementId Destination { get; set; }
    public int Population { get; set; }
    public CultureId Culture { get; set; }
    public List<CharacterId> Members { get; } = new();
}

using Cultures.Civilization;
using Cultures.Core.Ids;

namespace Cultures.Application.Persistence;

/// <summary>
/// Serialization seam for cultures and factions. Not wired into save envelope v2.
/// </summary>
public sealed record CultureRecord(
    ulong Id,
    string Name,
    byte LanguageFamily,
    byte NamingStyle,
    byte ArchitectureTendency,
    byte FoodPreference,
    byte SocialCustom,
    ulong GenerationSalt);

public sealed record FactionRecord(
    ulong Id,
    string Name,
    ulong Culture,
    ulong FoundedTick,
    ulong HomeSettlement,
    byte Lifecycle);

public sealed record FactionRelationRecord(ulong Lower, ulong Higher, byte Stance);

public static class CivilizationMapper
{
    public static CultureRecord ToRecord(CultureState culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return new CultureRecord(
            culture.Id.Value,
            culture.Name,
            culture.Traits.LanguageFamily,
            culture.Traits.NamingStyle,
            culture.Traits.ArchitectureTendency,
            culture.Traits.FoodPreference,
            culture.Traits.SocialCustom,
            culture.GenerationSalt);
    }

    public static CultureState FromRecord(CultureRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new CultureState(
            new CultureId(record.Id),
            record.Name,
            new CultureTraits(
                record.LanguageFamily,
                record.NamingStyle,
                record.ArchitectureTendency,
                record.FoodPreference,
                record.SocialCustom),
            record.GenerationSalt);
    }

    public static FactionRecord ToRecord(FactionState faction)
    {
        ArgumentNullException.ThrowIfNull(faction);
        return new FactionRecord(
            faction.Id.Value,
            faction.Name,
            faction.Culture.Value,
            faction.FoundedTick,
            faction.HomeSettlement.Value,
            (byte)faction.Lifecycle);
    }

    public static FactionState FromRecord(FactionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new FactionState(
            new FactionId(record.Id),
            record.Name,
            new CultureId(record.Culture),
            record.FoundedTick)
        {
            HomeSettlement = new SettlementId(record.HomeSettlement),
            Lifecycle = (FactionLifecycle)record.Lifecycle
        };
    }

    public static FactionRelationRecord ToRecord(FactionRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);
        return new FactionRelationRecord(relation.Lower.Value, relation.Higher.Value, (byte)relation.Stance);
    }

    public static FactionRelation FromRecord(FactionRelationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new FactionRelation(
            new FactionId(record.Lower),
            new FactionId(record.Higher),
            (FactionRelationStance)record.Stance);
    }
}

using Cultures.Core.Ids;

namespace Cultures.Settlement;

/// <summary>
/// Deterministic name keys. Not the final culture-language naming system (OD-020).
/// </summary>
public static class SettlementNames
{
    public static string Key(ulong worldSeed, SettlementId id)
    {
        var mixed = worldSeed ^ (id.Value * 0x9E3779B97F4A7C15UL);
        return $"set.{mixed:x8}";
    }
}

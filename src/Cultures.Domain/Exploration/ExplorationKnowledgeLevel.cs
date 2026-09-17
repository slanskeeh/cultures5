namespace Cultures.Exploration;

/// <summary>
/// What the player knows about a place. Missing directory entries are Unknown.
/// </summary>
public enum ExplorationKnowledgeLevel : byte
{
    Unknown = 0,
    Rumored = 1,
    Scouted = 2,
    Mapped = 3,
    Confirmed = 4,
    Analyzed = 5
}

/// <summary>
/// How a knowledge record was last written. Not a rumor narrative.
/// </summary>
public enum ExplorationSource : byte
{
    None = 0,
    DebugRumor = 1,
    Scout = 2,
    Map = 3,
    Confirm = 4,
    Analyze = 5
}

/// <summary>
/// Future discovery kinds. Phase 8 does not populate these.
/// </summary>
public enum DiscoveryKind : byte
{
    Resource = 1,
    Landmark = 2,
    Settlement = 3,
    Civilization = 4,
    River = 5,
    Pass = 6,
    Deposit = 7
}

public static class ExplorationKnowledgeLevels
{
    public static bool CanAdvanceTo(this ExplorationKnowledgeLevel current, ExplorationKnowledgeLevel target)
    {
        return target switch
        {
            ExplorationKnowledgeLevel.Rumored => current == ExplorationKnowledgeLevel.Unknown,
            ExplorationKnowledgeLevel.Scouted => current is ExplorationKnowledgeLevel.Unknown
                or ExplorationKnowledgeLevel.Rumored,
            ExplorationKnowledgeLevel.Mapped => current == ExplorationKnowledgeLevel.Scouted,
            ExplorationKnowledgeLevel.Confirmed => current == ExplorationKnowledgeLevel.Mapped,
            ExplorationKnowledgeLevel.Analyzed => current == ExplorationKnowledgeLevel.Confirmed,
            _ => false
        };
    }
}

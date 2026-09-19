namespace Cultures.Economy;

/// <summary>
/// Playable resource rules. Not the final catalogue (OD-011 / AD-128).
/// </summary>
public static class ResourceRules
{
    public static readonly ResourceType[] Edible =
    [
        ResourceType.Food,
        ResourceType.WildBerries,
        ResourceType.Mushrooms,
        ResourceType.Fish,
        ResourceType.Meat
    ];

    public static bool IsEdible(ResourceType type) =>
        type is ResourceType.Food or ResourceType.WildBerries or ResourceType.Mushrooms
            or ResourceType.Fish or ResourceType.Meat;
}

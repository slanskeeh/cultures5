namespace Cultures.Buildings;

public readonly record struct BuildingTypeId(string Value)
{
    public static BuildingTypeId Shelter { get; } = new("shelter");
    public static BuildingTypeId Farm { get; } = new("farm");
    public static BuildingTypeId Storage { get; } = new("storage");
    public static BuildingTypeId Workshop { get; } = new("workshop");
    public static BuildingTypeId HuntingCamp { get; } = new("hunting_camp");
    public static BuildingTypeId Fishery { get; } = new("fishery");

    public override string ToString() => Value;
}

public readonly record struct RecipeId(string Value)
{
    public static RecipeId FarmFood { get; } = new("farm.food");
    public static RecipeId FarmBerries { get; } = new("farm.berries");
    public static RecipeId WorkshopWood { get; } = new("workshop.wood");
    public static RecipeId WorkshopStone { get; } = new("workshop.stone");
    public static RecipeId HuntFood { get; } = new("hunt.food");
    public static RecipeId FisheryFood { get; } = new("fishery.food");

    public override string ToString() => Value;
}

public enum BuildingLifecycle : byte
{
    Planned = 0,
    Constructing = 1,
    Active = 2,
    Disabled = 3
}

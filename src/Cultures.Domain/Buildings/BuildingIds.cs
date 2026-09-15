namespace Cultures.Buildings;

public readonly record struct BuildingTypeId(string Value)
{
    public static BuildingTypeId Shelter { get; } = new("shelter");
    public static BuildingTypeId Farm { get; } = new("farm");
    public static BuildingTypeId Storage { get; } = new("storage");
    public static BuildingTypeId Workshop { get; } = new("workshop");

    public override string ToString() => Value;
}

public readonly record struct RecipeId(string Value)
{
    public static RecipeId FarmFood { get; } = new("farm.food");
    public static RecipeId WorkshopWood { get; } = new("workshop.wood");

    public override string ToString() => Value;
}

public enum BuildingLifecycle : byte
{
    Planned = 0,
    Constructing = 1,
    Active = 2,
    Disabled = 3
}

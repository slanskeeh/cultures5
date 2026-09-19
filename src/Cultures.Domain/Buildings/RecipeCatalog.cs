using Cultures.Economy;
using Cultures.Population;

namespace Cultures.Buildings;

public sealed class RecipeCatalog
{
    /// <summary>Temporary development recipes (not the final content list).</summary>
    public static RecipeCatalog Development { get; } = CreateDevelopment();

    private readonly Dictionary<string, ProductionRecipe> _byId;

    public RecipeCatalog(IEnumerable<ProductionRecipe> recipes)
    {
        _byId = recipes.ToDictionary(r => r.Id.Value, StringComparer.Ordinal);
    }

    public ProductionRecipe Get(RecipeId id)
    {
        if (!_byId.TryGetValue(id.Value, out var recipe))
            throw new KeyNotFoundException($"Unknown recipe '{id.Value}'.");
        return recipe;
    }

    public bool TryGet(RecipeId id, out ProductionRecipe recipe) =>
        _byId.TryGetValue(id.Value, out recipe!);

    private static RecipeCatalog CreateDevelopment() => new(
    [
        new ProductionRecipe(
            RecipeId.FarmFood,
            durationTicks: 24,
            outputs: [new ResourceStack(ResourceType.Food, 1)],
            skill: SkillType.Farming),
        new ProductionRecipe(
            RecipeId.FarmBerries,
            durationTicks: 20,
            outputs: [new ResourceStack(ResourceType.WildBerries, 1)],
            skill: SkillType.Farming),
        new ProductionRecipe(
            RecipeId.WorkshopWood,
            durationTicks: 20,
            outputs: [new ResourceStack(ResourceType.Wood, 1)],
            skill: SkillType.Woodworking),
        new ProductionRecipe(
            RecipeId.WorkshopStone,
            durationTicks: 22,
            outputs: [new ResourceStack(ResourceType.Stone, 1)],
            skill: SkillType.Stoneworking),
        new ProductionRecipe(
            RecipeId.HuntFood,
            durationTicks: 18,
            outputs: [new ResourceStack(ResourceType.Food, 1)],
            skill: SkillType.Hunting),
        new ProductionRecipe(
            RecipeId.FisheryFood,
            durationTicks: 18,
            outputs: [new ResourceStack(ResourceType.Food, 1)],
            skill: SkillType.Fishing)
    ]);
}

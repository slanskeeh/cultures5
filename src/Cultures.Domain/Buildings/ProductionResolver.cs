using Cultures.Economy;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Buildings;

/// <summary>
/// Future hook: biome/climate/season may change this. Phase 4 is always 1.0x.
/// </summary>
public readonly record struct ProductionModifier(float OutputScale, float DurationScale)
{
    public static ProductionModifier Neutral { get; } = new(1f, 1f);
}

public interface IEnvironmentProductionModifier
{
    ProductionModifier Evaluate(BuildingState building, TerrainCell terrain, ProductionRecipe recipe);
}

/// <summary>
/// Phase 4 placeholder. Do not encode biome-specific products here.
/// </summary>
public sealed class NeutralEnvironmentProductionModifier : IEnvironmentProductionModifier
{
    public ProductionModifier Evaluate(BuildingState building, TerrainCell terrain, ProductionRecipe recipe) =>
        ProductionModifier.Neutral;
}

public readonly record struct ProductionEvaluation(
    RecipeId Recipe,
    int DurationTicks,
    IReadOnlyList<ResourceStack> Inputs,
    IReadOnlyList<ResourceStack> Outputs,
    ProductionModifier Modifier);

/// <summary>
/// Resolves recipe + environment (+ future worker skill) into a concrete production result.
/// Building type is the function; environment influences the result.
/// </summary>
public sealed class ProductionResolver
{
    public ProductionResolver(RecipeCatalog recipes, IEnvironmentProductionModifier environment)
    {
        Recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public RecipeCatalog Recipes { get; }
    public IEnvironmentProductionModifier Environment { get; }

    public bool TryEvaluate(
        BuildingState building,
        TerrainCell terrain,
        CharacterState? worker,
        out ProductionEvaluation evaluation)
    {
        evaluation = default;
        if (building.Definition.Recipe is not { } recipeId)
            return false;
        if (!Recipes.TryGet(recipeId, out var recipe))
            return false;

        var modifier = Environment.Evaluate(building, terrain, recipe);
        var skillLevel = 0;
        if (recipe.Skill is { } skill && worker is not null)
            skillLevel = worker.Skills.GetLevel(skill);

        var durationHundredths = Math.Clamp(120 - skillLevel / 5, 80, 140);
        var duration = Math.Max(
            1,
            (int)MathF.Round(recipe.DurationTicks * modifier.DurationScale * durationHundredths / 100f));
        var outputs = new ResourceStack[recipe.Outputs.Count];
        for (var i = 0; i < recipe.Outputs.Count; i++)
        {
            var raw = recipe.Outputs[i];
            var quantity = Math.Max(0, (int)MathF.Round(raw.Quantity * modifier.OutputScale));
            if (skillLevel >= SkillRules.BonusOutputLevel)
                quantity++;
            if (quantity <= 0 && raw.Quantity > 0)
                quantity = 1;
            outputs[i] = new ResourceStack(raw.Type, quantity);
        }

        evaluation = new ProductionEvaluation(recipe.Id, duration, recipe.Inputs, outputs, modifier);
        return true;
    }
}

namespace Cultures.Population;

/// <summary>
/// Assigns starting field jobs without skill. Deterministic by roster order.
/// </summary>
public static class ProfessionBootstrap
{
    public static void AssignStarting(PopulationRoster population, ProfessionCatalog professions)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(professions);
        if (professions.Starting.Count == 0 || population.Count < CharacterRules.DefaultPopulation)
            return;

        var index = 0;
        var fieldSlots = professions.Starting.Count * 2;
        foreach (var character in population.All)
        {
            if (!character.IsAlive || character.LifeStage < CharacterLifeStage.Adult)
                continue;
            if (character.Profession.IsAssigned)
            {
                index++;
                continue;
            }

            if (index < fieldSlots)
                character.Profession = professions.Starting[index % professions.Starting.Count].Id;
            index++;
        }
    }
}

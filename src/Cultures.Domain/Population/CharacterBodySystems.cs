using Cultures.Core.Time;

namespace Cultures.Population;

public sealed class CharacterAgingSystem
{
    public CharacterAgingSystem(SimulationCalendar calendar)
    {
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }

    public SimulationCalendar Calendar { get; }

    public void ApplyTick(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        character.AgeYears += 1.0 / Calendar.TicksPerYear;
        character.LifeStage = StageFor(character.AgeYears);
    }

    public static CharacterLifeStage StageFor(double ageYears)
    {
        if (ageYears >= CharacterRules.MaxLifespanYears)
            return CharacterLifeStage.Dead;
        if (ageYears < CharacterRules.InfantUntilYears)
            return CharacterLifeStage.Infant;
        if (ageYears < CharacterRules.ChildUntilYears)
            return CharacterLifeStage.Child;
        if (ageYears < CharacterRules.AdolescentUntilYears)
            return CharacterLifeStage.Adolescent;
        if (ageYears >= CharacterRules.ElderFromYears)
            return CharacterLifeStage.Elder;
        return CharacterLifeStage.Adult;
    }
}

public sealed class CharacterNeedsSystem
{
    public CharacterNeedsSystem(SimulationCalendar calendar)
    {
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }

    public SimulationCalendar Calendar { get; }

    public void ApplyTick(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        var day = Calendar.TicksPerDay;
        character.Needs.Hunger += CharacterRules.HungerPerDay / day;
        if (character.Activity.Kind != ActionKind.Sleep)
            character.Needs.Fatigue += CharacterRules.FatiguePerDay / day;
        character.Needs.Clamp();
    }
}

public sealed class CharacterSurvivalSystem
{
    public CharacterSurvivalSystem(SimulationCalendar calendar)
    {
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
    }

    public SimulationCalendar Calendar { get; }

    public void ApplyTick(CharacterState character)
    {
        if (!character.IsAlive)
            return;

        if (character.Needs.Hunger >= 1f)
            character.Health.Current -= CharacterRules.StarvationDamagePerDay / Calendar.TicksPerDay;

        if (character.AgeYears >= CharacterRules.MaxLifespanYears)
            character.Health.Current = 0f;

        if (character.Health.Current <= 0f)
        {
            character.Health.Current = 0f;
            character.LifeStage = CharacterLifeStage.Dead;
            character.Activity.Cancel();
        }
    }
}

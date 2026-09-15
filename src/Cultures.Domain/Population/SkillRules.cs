namespace Cultures.Population;

/// <summary>
/// Provisional skill/family/teaching numbers. Not final game design.
/// </summary>
public static class SkillRules
{
    public const int Cap = 100;
    public const int XpPerLevel = 100;
    public const int WorkXpPerCompletion = 10;
    public const int TeachXpPerSession = 15;
    public const int ParentTeachBonusXp = 5;
    public const int InheritancePermille = 80;
    public const int BonusOutputLevel = 50;
    public const int MinTeacherLevel = 8;
    public const int TeachingDurationTicks = 48;
    public const int TeachingRange = 4;
    public const int MaxPopulation = 40;
    public const int MaxParents = 2;
    public const int MaxChildrenPerParent = 3;
    public const float NewbornAgeYears = 0f;
    public const float MinLearningAgeYears = 4f;
    public const int AdultFarmingBase = 12;
    public const int AdultFarmingSpan = 16;
    public const int AdultWoodworkingBase = 4;
    public const int AdultWoodworkingSpan = 12;

    public static int MaxExperience => Cap * XpPerLevel;

    public static int LevelToXp(int level) => Math.Clamp(level, 0, Cap) * XpPerLevel;

    public static int InheritedXp(int parentLevel) =>
        LevelToXp(parentLevel) * InheritancePermille / 1000;

    public static bool CanWork(CharacterState character) =>
        character.IsAlive && character.LifeStage is CharacterLifeStage.Adult or CharacterLifeStage.Elder;

    public static bool IsDependent(CharacterState character) =>
        character.IsAlive && character.LifeStage == CharacterLifeStage.Infant;

    public static bool CanLearn(CharacterState character) =>
        character.IsAlive
        && !IsDependent(character)
        && character.AgeYears >= MinLearningAgeYears
        && character.LifeStage != CharacterLifeStage.Dead;

    public static bool CanTeach(CharacterState character) =>
        character.IsAlive && character.LifeStage is CharacterLifeStage.Adult or CharacterLifeStage.Elder;
}

using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.World;

namespace Cultures.Population;

public readonly record struct TeachingIntent(
    ActionKind Kind,
    CharacterId Partner,
    SkillType Skill,
    LogicalGridCoordinate Target);

public sealed class TeachingSystem
{
    public TeachingSystem(PopulationRoster population, LogicalWorld world, EventBus events, SimulationClock clock)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public PopulationRoster Population { get; }
    public LogicalWorld World { get; }
    public EventBus Events { get; }
    public SimulationClock Clock { get; }

    public bool TryValidate(CharacterState teacher, CharacterState student, SkillType skill, out string error)
    {
        error = "Teaching is not allowed.";
        if (!SkillRules.CanTeach(teacher))
        {
            error = "Teacher cannot teach.";
            return false;
        }

        if (!SkillRules.CanLearn(student))
        {
            error = "Student cannot learn.";
            return false;
        }

        if (teacher.Id == student.Id)
        {
            error = "A character cannot teach themselves.";
            return false;
        }

        if (teacher.Skills.GetLevel(skill) < SkillRules.MinTeacherLevel)
        {
            error = "Teacher skill is too low.";
            return false;
        }

        if (teacher.Skills.GetLevel(skill) <= student.Skills.GetLevel(skill))
        {
            error = "Teacher is not more skilled than the student.";
            return false;
        }

        if (student.Skills.GetLevel(skill) >= SkillRules.Cap)
        {
            error = "Student skill is capped.";
            return false;
        }

        return true;
    }

    public bool IsInRange(CharacterState a, CharacterState b) => Distance(a.Position, b.Position) <= SkillRules.TeachingRange;

    public int Distance(LogicalGridCoordinate a, LogicalGridCoordinate b) =>
        World.Topology.HorizontalDistance(a, b) + Math.Abs(a.Y - b.Y);

    public bool TryBegin(CharacterState teacher, CharacterState student, SkillType skill, bool interrupt, out string error)
    {
        if (!TryValidate(teacher, student, skill, out error))
            return false;
        if (!IsInRange(teacher, student))
        {
            error = "Teacher and student are too far apart.";
            return false;
        }

        if (interrupt)
        {
            teacher.Activity.Cancel();
            student.Activity.Cancel();
        }
        else if (!teacher.Activity.NeedsDecision || !student.Activity.NeedsDecision)
        {
            error = "A participant is busy.";
            return false;
        }

        teacher.Activity.Start(
            ActionKind.Teach,
            SkillRules.TeachingDurationTicks,
            student.Position,
            student.Id,
            skill);
        student.Activity.Start(
            ActionKind.Learn,
            SkillRules.TeachingDurationTicks,
            teacher.Position,
            teacher.Id,
            skill);
        Events.Publish(new TeachingStartedEvent(Clock.Tick, teacher.Id, student.Id, skill));
        return true;
    }

    public void Complete(CharacterState teacher)
    {
        if (teacher.Activity.Kind != ActionKind.Teach || teacher.Activity.Skill is not { } skill)
            return;
        if (!Population.TryGet(teacher.Activity.PartnerId, out var student))
            return;

        var xp = SkillRules.TeachXpPerSession;
        if (FamilyQueries.AreParentAndChild(teacher, student))
            xp += SkillRules.ParentTeachBonusXp;

        var previous = student.Skills.GetLevel(skill);
        if (student.Skills.TryAddExperience(skill, xp, out var newLevel) && newLevel > previous)
            Events.Publish(new SkillImprovedEvent(Clock.Tick, student.Id, skill, newLevel));

        Events.Publish(new TeachingCompletedEvent(Clock.Tick, teacher.Id, student.Id, skill));
        student.Activity.Cancel();
    }

    public void Abandon(CharacterState character)
    {
        var partnerId = character.Activity.PartnerId;
        if (partnerId.IsAssigned && Population.TryGet(partnerId, out var partner)
            && partner.Activity.Kind is ActionKind.Teach or ActionKind.Learn)
        {
            partner.Activity.Cancel();
        }

        if (character.Activity.Kind is ActionKind.Teach or ActionKind.Learn)
            character.Activity.Cancel();
    }

    public TeachingIntent? ConsiderAutonomous(CharacterState character)
    {
        if (!character.IsAlive || SkillRules.IsDependent(character) || !character.Activity.NeedsDecision)
            return null;
        if (character.Needs.Hunger >= CharacterRules.HungerCritical
            || character.Needs.Fatigue >= CharacterRules.FatigueCritical)
            return null;

        if (character.LifeStage is CharacterLifeStage.Child or CharacterLifeStage.Adolescent)
            return ConsiderStudent(character);

        return ConsiderTeacher(character);
    }

    private TeachingIntent? ConsiderStudent(CharacterState student)
    {
        CharacterState? best = null;
        SkillType skill = SkillType.Farming;
        var bestLevel = -1;
        foreach (var parent in FamilyQueries.ParentsOf(Population, student))
        {
            if (!TryBestSkill(parent, student, out var candidate, out var level))
                continue;
            if (level <= bestLevel)
                continue;
            bestLevel = level;
            best = parent;
            skill = candidate;
        }

        if (best is null)
            return null;
        if (IsInRange(student, best))
            return new TeachingIntent(ActionKind.Learn, best.Id, skill, best.Position);
        return new TeachingIntent(ActionKind.Move, best.Id, skill, best.Position);
    }

    private TeachingIntent? ConsiderTeacher(CharacterState teacher)
    {
        foreach (var child in FamilyQueries.ChildrenOf(Population, teacher))
        {
            if (!child.Activity.NeedsDecision)
                continue;
            if (!TryBestSkill(teacher, child, out var skill, out _))
                continue;
            if (IsInRange(teacher, child))
                return new TeachingIntent(ActionKind.Teach, child.Id, skill, child.Position);
            return new TeachingIntent(ActionKind.Move, child.Id, skill, child.Position);
        }

        return null;
    }

    private bool TryBestSkill(CharacterState teacher, CharacterState student, out SkillType skill, out int teacherLevel)
    {
        skill = SkillType.Farming;
        teacherLevel = -1;
        foreach (SkillType candidate in Enum.GetValues<SkillType>())
        {
            if (!TryValidate(teacher, student, candidate, out _))
                continue;
            var level = teacher.Skills.GetLevel(candidate);
            if (level <= teacherLevel)
                continue;
            teacherLevel = level;
            skill = candidate;
        }

        return teacherLevel >= SkillRules.MinTeacherLevel;
    }
}

using Cultures.Application;
using Cultures.Buildings;
using Cultures.Core.Events;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class FamilyRelationshipTests
{
    [Fact]
    public void Child_has_queryable_parents_and_siblings()
    {
        var host = new SimulationHost(1, populationCount: 4);
        var a = host.Population[0];
        var b = host.Population[1];
        Assert.True(host.Creation.TryCreateChild(a.Id, b.Id, out var first, out _));
        Assert.True(host.Creation.TryCreateChild(a.Id, b.Id, out var second, out _));
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Contains(a.Id, first!.FamilyLinks.Parents);
        Assert.Contains(b.Id, first.FamilyLinks.Parents);
        Assert.True(FamilyQueries.AreParentAndChild(a, first));
        Assert.Contains(second, FamilyQueries.SiblingsOf(host.Population, first));
        Assert.Equal(first.Id, FamilyQueries.ChildrenOf(host.Population, a).First().Id);
    }

    [Fact]
    public void Multi_generation_relationships_are_queryable()
    {
        var population = new PopulationRoster();
        var grand = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(1, 1), 1);
        var parent = new CharacterState(new CharacterId(2), new LogicalGridCoordinate(2, 1), 2);
        var child = new CharacterState(new CharacterId(3), new LogicalGridCoordinate(3, 1), 3)
        {
            AgeYears = 8,
            LifeStage = CharacterLifeStage.Child
        };
        population.Add(grand);
        population.Add(parent);
        population.Add(child);
        Assert.True(FamilyQueries.TryLinkParentChild(grand, parent));
        Assert.True(FamilyQueries.TryLinkParentChild(parent, child));
        Assert.Equal(grand.Id, FamilyQueries.GrandparentsOf(population, child).Single().Id);
    }

    [Fact]
    public void Self_parent_link_is_rejected()
    {
        var person = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1);
        Assert.False(FamilyQueries.TryLinkParentChild(person, person));
    }
}

public sealed class BirthAndLifeStageTests
{
    [Fact]
    public void Birth_assigns_new_id_age_parents_and_is_deterministic()
    {
        var a = new SimulationHost(4, populationCount: 2);
        var b = new SimulationHost(4, populationCount: 2);
        Assert.True(a.Commands.Execute(new CreateChildCommand(a.Population[0].Id, a.Population[1].Id)).Success);
        Assert.True(b.Commands.Execute(new CreateChildCommand(b.Population[0].Id, b.Population[1].Id)).Success);
        var childA = a.Population[2];
        var childB = b.Population[2];
        Assert.NotEqual(a.Population[0].Id, childA.Id);
        Assert.Equal(childA.Id, childB.Id);
        Assert.Equal(SkillRules.NewbornAgeYears, childA.AgeYears);
        Assert.Equal(CharacterLifeStage.Infant, childA.LifeStage);
        Assert.Equal(childA.FamilyLinks.Parents, childB.FamilyLinks.Parents);
        Assert.Equal(childA.FamilyLinks.Caregivers, childA.FamilyLinks.Parents);
        Assert.Equal(childA.Skills.GetLevel(SkillType.Farming), childB.Skills.GetLevel(SkillType.Farming));
    }

    [Fact]
    public void Population_cap_prevents_explosion()
    {
        var host = new SimulationHost(1, populationCount: SkillRules.MaxPopulation);
        var result = host.Commands.Execute(new CreateChildCommand(host.Population[0].Id, host.Population[1].Id));
        Assert.False(result.Success);
        Assert.Equal(SkillRules.MaxPopulation, host.Population.Count);
    }

    [Fact]
    public void Age_determines_life_stage_and_children_cannot_work()
    {
        var host = new SimulationHost(1, populationCount: 2);
        Assert.Equal(CharacterLifeStage.Adult, CharacterAgingSystem.StageFor(22));
        Assert.Equal(CharacterLifeStage.Infant, CharacterAgingSystem.StageFor(0));
        Assert.Equal(CharacterLifeStage.Infant, CharacterAgingSystem.StageFor(2));
        Assert.Equal(CharacterLifeStage.Child, CharacterAgingSystem.StageFor(8));
        Assert.Equal(CharacterLifeStage.Adolescent, CharacterAgingSystem.StageFor(14));
        Assert.Equal(CharacterLifeStage.Elder, CharacterAgingSystem.StageFor(61));
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        Assert.Equal(0, child!.AgeYears);
        Assert.Equal(CharacterLifeStage.Infant, child.LifeStage);
        Assert.False(SkillRules.CanWork(child));
        Assert.Null(host.Production.FindFreeWorkplace(child));
        Assert.NotNull(host.Production.FindFreeWorkplace(host.Population[0]));
    }

    [Fact]
    public void Infant_does_not_work_or_navigate_independently()
    {
        var world = new LogicalWorld(WorldConfiguration.DebugSample, 1);
        var decisions = new CharacterDecisionSystem(
            new GridNavigator(world),
            TestProduction.ForWorld(world),
            TestProduction.Teaching(world));
        var infant = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1)
        {
            AgeYears = 0,
            LifeStage = CharacterLifeStage.Infant
        };
        infant.Needs.Hunger = 0.9f;
        Assert.Equal(ActionKind.Idle, decisions.ChooseKind(infant));
        Assert.True(infant.Inventory.TryAdd(Cultures.Economy.ResourceType.Food, 1));
        Assert.Equal(ActionKind.Eat, decisions.ChooseKind(infant));
        Assert.False(SkillRules.CanLearn(infant));
        Assert.False(SkillRules.CanTeach(infant));
    }

    [Fact]
    public void Life_stage_transitions_are_age_driven()
    {
        var aging = new CharacterAgingSystem(SimulationCalendar.Default);
        var character = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1)
        {
            AgeYears = 0,
            LifeStage = CharacterLifeStage.Infant
        };
        character.AgeYears = CharacterRules.InfantUntilYears;
        aging.ApplyTick(character);
        Assert.Equal(CharacterLifeStage.Child, character.LifeStage);

        character.AgeYears = CharacterRules.ChildUntilYears;
        aging.ApplyTick(character);
        Assert.Equal(CharacterLifeStage.Adolescent, character.LifeStage);

        character.AgeYears = CharacterRules.AdolescentUntilYears;
        aging.ApplyTick(character);
        Assert.Equal(CharacterLifeStage.Adult, character.LifeStage);

        character.AgeYears = CharacterRules.ElderFromYears;
        aging.ApplyTick(character);
        Assert.Equal(CharacterLifeStage.Elder, character.LifeStage);
    }
}

public sealed class SkillAndProductionTests
{
    [Fact]
    public void Character_can_hold_and_query_multiple_skills()
    {
        var character = new CharacterState(new CharacterId(1), new LogicalGridCoordinate(0, 0), 1);
        character.Skills.SetLevel(SkillType.Farming, 20);
        character.Skills.SetLevel(SkillType.Crafting, 7);
        Assert.Equal(20, character.Skills.GetLevel(SkillType.Farming));
        Assert.Equal(7, character.Skills.GetLevel(SkillType.Crafting));
        Assert.Equal(0, character.Skills.GetLevel(SkillType.Stoneworking));
        Assert.True(character.Skills.Has(SkillType.Farming, 10));
    }

    [Fact]
    public void Work_increases_relevant_skill_only()
    {
        var host = new SimulationHost(1, populationCount: 1);
        var farm = host.Buildings.All.First(b => b.TypeId == BuildingTypeId.Farm);
        var worker = host.Population[0];
        worker.Skills.SetLevel(SkillType.Farming, 10);
        worker.Skills.SetLevel(SkillType.Woodworking, 10);
        var farmXp = worker.Skills.GetExperience(SkillType.Farming);
        var woodXp = worker.Skills.GetExperience(SkillType.Woodworking);
        worker.Position = farm.AccessCell;
        host.Production.AssignWorkplace(worker, new WorkplaceId(farm.Id, 0));
        Assert.True(host.Production.TryEvaluate(farm, worker, out var evaluation));
        host.Production.BeginWork(farm, worker);
        worker.Activity.Start(ActionKind.Work, evaluation.DurationTicks, farm.AccessCell);
        for (var i = 0; i < evaluation.DurationTicks; i++)
            host.Characters.Actions.Advance(worker);

        Assert.True(worker.Skills.GetExperience(SkillType.Farming) > farmXp);
        Assert.Equal(woodXp, worker.Skills.GetExperience(SkillType.Woodworking));
    }

    [Fact]
    public void Worker_skill_changes_production_output()
    {
        var host = new SimulationHost(1, populationCount: 2, placeDevelopmentBuildings: false);
        var origin = PopulationSpawner.FindLandOrigin(host.World);
        var farm = host.Placement.TryPlace(BuildingTypeId.Farm, origin).Building!;
        var low = host.Population[0];
        var high = host.Population[1];
        low.Skills.SetLevel(SkillType.Farming, 5);
        high.Skills.SetLevel(SkillType.Farming, 80);
        Assert.True(host.Production.TryEvaluate(farm, low, out var lowEval));
        Assert.True(host.Production.TryEvaluate(farm, high, out var highEval));
        var lowFood = lowEval.Outputs.Single(s => s.Type == Cultures.Economy.ResourceType.Food).Quantity;
        var highFood = highEval.Outputs.Single(s => s.Type == Cultures.Economy.ResourceType.Food).Quantity;
        Assert.True(highFood > lowFood);
        Assert.True(highEval.DurationTicks <= lowEval.DurationTicks);
    }

    [Fact]
    public void Inheritance_is_small_deterministic_and_not_a_copy()
    {
        var host = new SimulationHost(2, populationCount: 2);
        host.Population[0].Skills.SetLevel(SkillType.Farming, 70);
        host.Population[1].Skills.SetLevel(SkillType.Farming, 20);
        Assert.True(host.Creation.TryCreateChild(host.Population[0].Id, host.Population[1].Id, out var child, out _));
        var inherited = child!.Skills.GetLevel(SkillType.Farming);
        Assert.InRange(inherited, 1, 20);
        Assert.NotEqual(70, inherited);
        var again = new SimulationHost(2, populationCount: 2);
        again.Population[0].Skills.SetLevel(SkillType.Farming, 70);
        again.Population[1].Skills.SetLevel(SkillType.Farming, 20);
        Assert.True(again.Creation.TryCreateChild(again.Population[0].Id, again.Population[1].Id, out var child2, out _));
        Assert.Equal(inherited, child2!.Skills.GetLevel(SkillType.Farming));
    }
}

public sealed class TeachingTests
{
    [Fact]
    public void Valid_teaching_increases_student_skill_over_time()
    {
        var host = SetupTeacherAndStudent(out var teacher, out var student);
        var before = student.Skills.GetLevel(SkillType.Farming);
        Assert.True(host.Teaching.TryBegin(teacher, student, SkillType.Farming, interrupt: true, out _));
        Assert.Equal(ActionKind.Teach, teacher.Activity.Kind);
        Assert.Equal(ActionKind.Learn, student.Activity.Kind);
        for (var i = 0; i < SkillRules.TeachingDurationTicks; i++)
        {
            host.Characters.Actions.Advance(teacher);
            host.Characters.Actions.Advance(student);
        }

        Assert.True(student.Skills.GetExperience(SkillType.Farming) > SkillRules.LevelToXp(before));
        Assert.Equal(ActionKind.None, teacher.Activity.Kind);
        Assert.Equal(ActionKind.None, student.Activity.Kind);
    }

    [Fact]
    public void Invalid_teacher_cannot_teach()
    {
        var host = new SimulationHost(1, populationCount: 2);
        var teacher = host.Population[0];
        var student = host.Population[1];
        teacher.Skills.SetLevel(SkillType.Farming, 0);
        student.Skills.SetLevel(SkillType.Farming, 0);
        Assert.False(host.Teaching.TryBegin(teacher, student, SkillType.Farming, interrupt: true, out _));
        Assert.False(host.Commands.Execute(new TeachCharacterCommand(teacher.Id, student.Id, SkillType.Farming)).Success);
    }

    [Fact]
    public void Teaching_command_uses_application_layer()
    {
        var host = SetupTeacherAndStudent(out var teacher, out var student);
        teacher.Position = student.Position;
        var result = host.Commands.Execute(new TeachCharacterCommand(teacher.Id, student.Id, SkillType.Farming));
        Assert.True(result.Success);
        Assert.Equal(ActionKind.Teach, teacher.Activity.Kind);
        Assert.Equal(ActionKind.Learn, student.Activity.Kind);
    }

    [Fact]
    public void Teaching_is_deterministic()
    {
        var a = SetupTeacherAndStudent(out var teacherA, out var studentA);
        var b = SetupTeacherAndStudent(out var teacherB, out var studentB);
        a.Teaching.TryBegin(teacherA, studentA, SkillType.Farming, interrupt: true, out _);
        b.Teaching.TryBegin(teacherB, studentB, SkillType.Farming, interrupt: true, out _);
        for (var i = 0; i < SkillRules.TeachingDurationTicks; i++)
        {
            a.Step(1);
            b.Step(1);
        }

        Assert.Equal(studentA.Skills.GetExperience(SkillType.Farming), studentB.Skills.GetExperience(SkillType.Farming));
    }

    [Fact]
    public void Autonomous_parent_teaching_can_start()
    {
        var host = SetupTeacherAndStudent(out var teacher, out var student);
        teacher.Position = student.Position;
        teacher.Activity.Cancel();
        student.Activity.Cancel();
        host.Characters.Decisions.AssignNext(teacher);
        Assert.True(
            teacher.Activity.Kind is ActionKind.Teach or ActionKind.Move
            || student.Activity.Kind is ActionKind.Learn);
    }

    private static SimulationHost SetupTeacherAndStudent(out CharacterState teacher, out CharacterState student)
    {
        var host = new SimulationHost(3, populationCount: 2, placeDevelopmentBuildings: false);
        teacher = host.Population[0];
        Assert.True(host.Creation.TryCreateChild(teacher.Id, host.Population[1].Id, out var child, out _));
        student = child!;
        student.AgeYears = 8;
        student.LifeStage = CharacterLifeStage.Child;
        teacher.Skills.SetLevel(SkillType.Farming, 40);
        student.Skills.SetLevel(SkillType.Farming, 2);
        student.Position = teacher.Position;
        return host;
    }
}

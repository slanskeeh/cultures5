using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Population;

public sealed record TeachCharacterCommand(CharacterId TeacherId, CharacterId StudentId, SkillType Skill) : ICommand;

public sealed record CreateChildCommand(CharacterId ParentA, CharacterId ParentB) : ICommand;

public sealed record AddSkillExperienceCommand(CharacterId CharacterId, SkillType Skill, int Experience) : ICommand;

public sealed class TeachCharacterHandler : ICommandHandler<TeachCharacterCommand>
{
    public TeachCharacterHandler(PopulationRoster population, TeachingSystem teaching, GridNavigator navigator)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Teaching = teaching ?? throw new ArgumentNullException(nameof(teaching));
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public PopulationRoster Population { get; }
    public TeachingSystem Teaching { get; }
    public GridNavigator Navigator { get; }

    public CommandResult Handle(TeachCharacterCommand command)
    {
        if (!Population.TryGet(command.TeacherId, out var teacher)
            || !Population.TryGet(command.StudentId, out var student))
            return CommandResult.Fail("Unknown teacher or student.");

        if (teacher.LodTier.IsAggregate() || student.LodTier.IsAggregate())
            return CommandResult.Fail("Character is in aggregate simulation.");

        teacher.IsPlayerCommanded = true;
        if (Teaching.TryBegin(teacher, student, command.Skill, interrupt: true, out var error))
            return CommandResult.Ok();

        if (!Teaching.TryValidate(teacher, student, command.Skill, out error))
            return CommandResult.Fail(error);

        var path = Navigator.FindPath(teacher.Position, student.Position);
        if (path is null)
            return CommandResult.Fail("Teacher cannot reach the student.");

        teacher.Activity.Start(ActionKind.Move, Math.Max(1, path.Count), student.Position, student.Id, command.Skill);
        foreach (var step in path)
            teacher.Activity.RemainingPath.Enqueue(step);
        return CommandResult.Ok();
    }
}

public sealed class CreateChildHandler : ICommandHandler<CreateChildCommand>
{
    public CreateChildHandler(CharacterCreation creation)
    {
        Creation = creation ?? throw new ArgumentNullException(nameof(creation));
    }

    public CharacterCreation Creation { get; }

    public CommandResult Handle(CreateChildCommand command)
    {
        var second = command.ParentB.IsAssigned ? command.ParentB : (CharacterId?)null;
        if (!Creation.TryCreateChild(command.ParentA, second, out _, out var error))
            return CommandResult.Fail(error);
        return CommandResult.Ok();
    }
}

public sealed class AddSkillExperienceHandler : ICommandHandler<AddSkillExperienceCommand>
{
    public AddSkillExperienceHandler(PopulationRoster population)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
    }

    public PopulationRoster Population { get; }

    public CommandResult Handle(AddSkillExperienceCommand command)
    {
        if (!Population.TryGet(command.CharacterId, out var character))
            return CommandResult.Fail("Unknown character.");
        if (!character.Skills.TryAddExperience(command.Skill, command.Experience, out _))
            return CommandResult.Fail("Could not add experience.");
        return CommandResult.Ok();
    }
}

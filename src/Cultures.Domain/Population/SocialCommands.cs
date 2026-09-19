using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.Buildings;

namespace Cultures.Population;

public sealed record AssignProfessionCommand(CharacterId Character, string Profession) : ICommand;

public sealed record MatchProfessionCommand(CharacterId Character) : ICommand;

public sealed record FormHouseholdCommand(CharacterId Founder, CharacterId Partner) : ICommand;

public sealed record SetHouseholdHomeCommand(HouseholdId Household, BuildingId Home) : ICommand;

public sealed record FormPartnershipCommand(CharacterId Left, CharacterId Right) : ICommand;

public sealed record LeaveHouseholdCommand(CharacterId Character) : ICommand;

public sealed class AssignProfessionHandler : ICommandHandler<AssignProfessionCommand>
{
    public AssignProfessionHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(AssignProfessionCommand command) =>
        Social.TryAssignProfession(command.Character, new ProfessionId(command.Profession), out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class MatchProfessionHandler : ICommandHandler<MatchProfessionCommand>
{
    public MatchProfessionHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(MatchProfessionCommand command) =>
        Social.TryMatchProfession(command.Character, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class FormHouseholdHandler : ICommandHandler<FormHouseholdCommand>
{
    public FormHouseholdHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(FormHouseholdCommand command)
    {
        var partner = command.Partner.IsAssigned ? command.Partner : (CharacterId?)null;
        return Social.TryFormHousehold(command.Founder, partner, out _, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
    }
}

public sealed class SetHouseholdHomeHandler : ICommandHandler<SetHouseholdHomeCommand>
{
    public SetHouseholdHomeHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(SetHouseholdHomeCommand command) =>
        Social.TrySetHome(command.Household, command.Home, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class FormPartnershipHandler : ICommandHandler<FormPartnershipCommand>
{
    public FormPartnershipHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(FormPartnershipCommand command) =>
        Social.TryFormPartnership(command.Left, command.Right, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

public sealed class LeaveHouseholdHandler : ICommandHandler<LeaveHouseholdCommand>
{
    public LeaveHouseholdHandler(SocialLifeSystem social)
    {
        Social = social ?? throw new ArgumentNullException(nameof(social));
    }

    public SocialLifeSystem Social { get; }

    public CommandResult Handle(LeaveHouseholdCommand command) =>
        Social.TryLeaveHousehold(command.Character, out var error)
            ? CommandResult.Ok()
            : CommandResult.Fail(error);
}

using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.Population;
using Cultures.World;

namespace Cultures.World;

public sealed record RefreshLodCommand : ICommand;

public sealed record ForceChunkLodCommand(ChunkCoordinate Chunk, SimulationLodTier Tier) : ICommand;

public sealed record ClearChunkLodOverrideCommand(ChunkCoordinate Chunk) : ICommand;

public sealed record ProtectCharacterCommand(CharacterId Character, bool Protect) : ICommand;

public sealed record SetChunkPresentationCommand(ChunkCoordinate Chunk, ChunkPresentationPresence Presence) : ICommand;

public sealed class RefreshLodHandler : ICommandHandler<RefreshLodCommand>
{
    public RefreshLodHandler(LodSystem lod)
    {
        Lod = lod ?? throw new ArgumentNullException(nameof(lod));
    }

    public LodSystem Lod { get; }

    public CommandResult Handle(RefreshLodCommand command)
    {
        Lod.Evaluate();
        return CommandResult.Ok();
    }
}

public sealed class ForceChunkLodHandler : ICommandHandler<ForceChunkLodCommand>
{
    public ForceChunkLodHandler(LodSystem lod)
    {
        Lod = lod ?? throw new ArgumentNullException(nameof(lod));
    }

    public LodSystem Lod { get; }

    public CommandResult Handle(ForceChunkLodCommand command)
    {
        Lod.ForceTier(command.Chunk, command.Tier);
        return CommandResult.Ok();
    }
}

public sealed class ClearChunkLodOverrideHandler : ICommandHandler<ClearChunkLodOverrideCommand>
{
    public ClearChunkLodOverrideHandler(LodSystem lod)
    {
        Lod = lod ?? throw new ArgumentNullException(nameof(lod));
    }

    public LodSystem Lod { get; }

    public CommandResult Handle(ClearChunkLodOverrideCommand command)
    {
        Lod.ClearForce(command.Chunk);
        return CommandResult.Ok();
    }
}

public sealed class ProtectCharacterHandler : ICommandHandler<ProtectCharacterCommand>
{
    public ProtectCharacterHandler(PopulationRoster population, LodSystem lod)
    {
        Population = population ?? throw new ArgumentNullException(nameof(population));
        Lod = lod ?? throw new ArgumentNullException(nameof(lod));
    }

    public PopulationRoster Population { get; }
    public LodSystem Lod { get; }

    public CommandResult Handle(ProtectCharacterCommand command)
    {
        if (!Population.TryGet(command.Character, out var character))
            return CommandResult.Fail("Unknown character.");
        if (!character.IsAlive)
            return CommandResult.Fail("Character is not alive.");

        character.IsPersistentIndividual = command.Protect;
        Lod.Evaluate();
        return CommandResult.Ok();
    }
}

public sealed class SetChunkPresentationHandler : ICommandHandler<SetChunkPresentationCommand>
{
    public SetChunkPresentationHandler(LodSystem lod)
    {
        Lod = lod ?? throw new ArgumentNullException(nameof(lod));
    }

    public LodSystem Lod { get; }

    public CommandResult Handle(SetChunkPresentationCommand command)
    {
        Lod.SetPresentation(command.Chunk, command.Presence);
        return CommandResult.Ok();
    }
}

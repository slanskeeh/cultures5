using Cultures.Core.Commands;
using Cultures.World;

namespace Cultures.Exploration;

public sealed record RumorChunkCommand(ChunkCoordinate Chunk) : ICommand;

public sealed record ScoutChunkCommand(ChunkCoordinate Chunk) : ICommand;

public sealed record MapChunkCommand(ChunkCoordinate Chunk) : ICommand;

public sealed record ConfirmChunkCommand(ChunkCoordinate Chunk) : ICommand;

public sealed record AnalyzeChunkCommand(ChunkCoordinate Chunk) : ICommand;

public sealed class RumorChunkHandler : ICommandHandler<RumorChunkCommand>
{
    public RumorChunkHandler(ExplorationSystem exploration)
    {
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
    }

    public ExplorationSystem Exploration { get; }

    public CommandResult Handle(RumorChunkCommand command) =>
        ExplorationCommandSupport.Advance(Exploration, command.Chunk, ExplorationKnowledgeLevel.Rumored);
}

public sealed class ScoutChunkHandler : ICommandHandler<ScoutChunkCommand>
{
    public ScoutChunkHandler(ExplorationSystem exploration)
    {
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
    }

    public ExplorationSystem Exploration { get; }

    public CommandResult Handle(ScoutChunkCommand command) =>
        ExplorationCommandSupport.Advance(Exploration, command.Chunk, ExplorationKnowledgeLevel.Scouted);
}

public sealed class MapChunkHandler : ICommandHandler<MapChunkCommand>
{
    public MapChunkHandler(ExplorationSystem exploration)
    {
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
    }

    public ExplorationSystem Exploration { get; }

    public CommandResult Handle(MapChunkCommand command) =>
        ExplorationCommandSupport.Advance(Exploration, command.Chunk, ExplorationKnowledgeLevel.Mapped);
}

public sealed class ConfirmChunkHandler : ICommandHandler<ConfirmChunkCommand>
{
    public ConfirmChunkHandler(ExplorationSystem exploration)
    {
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
    }

    public ExplorationSystem Exploration { get; }

    public CommandResult Handle(ConfirmChunkCommand command) =>
        ExplorationCommandSupport.Advance(Exploration, command.Chunk, ExplorationKnowledgeLevel.Confirmed);
}

public sealed class AnalyzeChunkHandler : ICommandHandler<AnalyzeChunkCommand>
{
    public AnalyzeChunkHandler(ExplorationSystem exploration)
    {
        Exploration = exploration ?? throw new ArgumentNullException(nameof(exploration));
    }

    public ExplorationSystem Exploration { get; }

    public CommandResult Handle(AnalyzeChunkCommand command) =>
        ExplorationCommandSupport.Advance(Exploration, command.Chunk, ExplorationKnowledgeLevel.Analyzed);
}

internal static class ExplorationCommandSupport
{
    public static CommandResult Advance(
        ExplorationSystem exploration,
        ChunkCoordinate chunk,
        ExplorationKnowledgeLevel target)
    {
        if (exploration.TryAdvance(chunk, target, out var error))
            return CommandResult.Ok();
        return CommandResult.Fail(error);
    }
}

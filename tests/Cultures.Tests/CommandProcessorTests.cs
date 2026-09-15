using Cultures.Core.Commands;

namespace Cultures.Tests;

public sealed class CommandProcessorTests
{
    [Fact]
    public void Ping_command_executes_and_records_token()
    {
        var handler = new PingCommandHandler();
        var processor = new CommandProcessor();
        processor.Register(handler);

        var result = processor.Execute(new PingCommand("phase-0"));

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Equal("phase-0", handler.LastToken);
        Assert.Equal(1, handler.ExecutionCount);
    }

    [Fact]
    public void Ping_command_fails_without_token()
    {
        var processor = new CommandProcessor();
        processor.Register(new PingCommandHandler());

        var result = processor.Execute(new PingCommand(" "));

        Assert.False(result.Success);
        Assert.Equal("Token is required.", result.Error);
    }

    [Fact]
    public void Unknown_command_fails()
    {
        var processor = new CommandProcessor();
        var result = processor.Execute(new PingCommand("x"));

        Assert.False(result.Success);
        Assert.Contains("No handler", result.Error);
    }
}

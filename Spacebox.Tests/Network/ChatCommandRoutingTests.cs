using Spacebox.Network.Admin;

namespace Spacebox.Tests.Network;

public class ChatCommandRoutingTests
{
    [Theory]
    [InlineData("gm 1", "gm")]
    [InlineData("  TP 1 2 3", "tp")]
    [InlineData("help", "help")]
    [InlineData("", "")]
    public void CommandName_IsTheFirstWordInLowerCase(string line, string name)
    {
        Assert.Equal(name, ChatCommands.CommandName(line));
    }

    [Fact]
    public void Teleport_ToCoordinatesStaysLocal_ForAPlayerGoesToTheServer()
    {
        Assert.False(ServerCommandForwarder.ShouldForwardTeleport(new[] { "10", "-5.5", "3" }));
        Assert.True(ServerCommandForwarder.ShouldForwardTeleport(new[] { "Anna", "10", "0", "3" }));
        Assert.False(ServerCommandForwarder.ShouldForwardTeleport(System.Array.Empty<string>()));
    }

    [Fact]
    public void TeleportToPlayer_WithTwoNamesGoesToTheServer()
    {
        Assert.False(ServerCommandForwarder.ShouldForwardTeleportToPlayer(new[] { "Anna" }));
        Assert.True(ServerCommandForwarder.ShouldForwardTeleportToPlayer(new[] { "Bob", "Anna" }));
    }

    [Fact]
    public void BuildLine_JoinsNameAndArguments()
    {
        Assert.Equal("gm 1 Anna", ServerCommandForwarder.BuildLine("gm", new[] { "1", "Anna" }));
        Assert.Equal("save", ServerCommandForwarder.BuildLine("save", System.Array.Empty<string>()));
    }
}

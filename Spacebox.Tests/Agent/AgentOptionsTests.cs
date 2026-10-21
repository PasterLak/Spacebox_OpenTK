using Spacebox.Agent;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Tests.Agent;

public class AgentOptionsTests
{
    [Fact]
    public void Parse_WithoutAgentFlags_IsDisabled()
    {
        var options = AgentOptions.Parse(["--connect", "host:1", "--hidden"]);

        Assert.False(options.Enabled);
        Assert.False(options.HasWorld);
        Assert.Equal(AgentOptions.DefaultPort, options.Port);
    }

    [Fact]
    public void Parse_ReadsPortWorldModeSeedAndFresh()
    {
        var options = AgentOptions.Parse(["--agent", "48000", "--AGENT-WORLD", "Test", "--agent-mode", "survival", "--agent-seed", "42", "--agent-fresh"]);

        Assert.True(options.Enabled);
        Assert.Equal(48000, options.Port);
        Assert.Equal("Test", options.WorldName);
        Assert.Equal(GameMode.Survival, options.GameMode);
        Assert.Equal("42", options.Seed);
        Assert.True(options.FreshWorld);
    }

    [Fact]
    public void Parse_AgentWorldWithoutName_UsesDefaultWorldAndKeepsNextFlag()
    {
        var options = AgentOptions.Parse(["--agent-world", "--agent", "--agent-mode", "unknown"]);

        Assert.True(options.Enabled);
        Assert.Equal(AgentOptions.DefaultWorld, options.WorldName);
        Assert.Equal(AgentOptions.DefaultPort, options.Port);
        Assert.Equal(GameMode.Creative, options.GameMode);
    }
}

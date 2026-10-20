using System.Text.Json;
using Spacebox.Game.Mobs;
using Spacebox.Network.Dedicated;

namespace Spacebox.Tests.Game;

public class MobRulesTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void FriendlyMobs_OnlyAttackWhenProvoked(bool friendly, bool provoked, bool expected)
    {
        Assert.Equal(expected, MobRules.IsHostile(friendly, provoked));
    }

    [Fact]
    public void ServerConfig_FriendlyMobs_IsOffByDefault()
    {
        Assert.False(new ServerConfig().FriendlyMobs);
        Assert.False(JsonSerializer.Deserialize<ServerConfig>("{ \"MaxPlayers\": 4 }")!.FriendlyMobs);
        Assert.True(JsonSerializer.Deserialize<ServerConfig>("{ \"FriendlyMobs\": true }")!.FriendlyMobs);
    }
}

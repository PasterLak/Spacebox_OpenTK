using Spacebox.Game.Commands;
using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Tests.Game;

public class PlayerCommandsTests
{
    [Theory]
    [InlineData("red", "Red")]
    [InlineData(" CYAN ", "Cyan")]
    [InlineData("Yellow", "Yellow")]
    public void Skins_NormalizeKnownColors(string input, string expected)
    {
        Assert.True(AstronautSkins.TryNormalize(input, out var name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("pink")]
    [InlineData("")]
    [InlineData(null)]
    public void Skins_RejectUnknownColors(string input)
    {
        Assert.False(AstronautSkins.TryNormalize(input, out _));
        Assert.Equal(AstronautSkins.Default, AstronautSkins.Normalize(input));
    }

    [Fact]
    public void Skins_IndexRoundTrips()
    {
        foreach (var name in AstronautSkins.Names)
            Assert.Equal(name, AstronautSkins.NameAt(AstronautSkins.IndexOf(name)));
        Assert.Equal(AstronautSkins.Default, AstronautSkins.NameAt(200));
    }

    [Fact]
    public void SkinStore_PrefersTheStoredSkin()
    {
        PlayerSkinStore.Reset();
        Assert.Equal("Blue", PlayerSkinStore.Resolve("Anna", "blue"));

        PlayerSkinStore.Set("Anna", "red");

        Assert.Equal("Red", PlayerSkinStore.Resolve("anna", "blue"));
        Assert.Equal("Green", PlayerSkinStore.Resolve("Bob", "green"));
        PlayerSkinStore.Reset();
    }

    [Theory]
    [InlineData("hp", PlayerStat.Health)]
    [InlineData("Health", PlayerStat.Health)]
    [InlineData("power", PlayerStat.Power)]
    [InlineData("air", PlayerStat.Air)]
    [InlineData("oxygen", PlayerStat.Air)]
    public void SetCommand_ParsesStatNames(string name, PlayerStat expected)
    {
        Assert.True(SetStatCommand.TryParseStat(name, out var stat));
        Assert.Equal(expected, stat);
    }

    [Fact]
    public void SetCommand_RejectsUnknownStats()
    {
        Assert.False(SetStatCommand.TryParseStat("mana", out _));
    }

    [Theory]
    [InlineData(50, 100, 50)]
    [InlineData(500, 100, 100)]
    [InlineData(0, 100, 0)]
    public void StatTarget_StaysWithinRange(int amount, int max, int expected)
    {
        Assert.Equal(expected, PlayerStatsAccess.Target(max, amount));
    }
}

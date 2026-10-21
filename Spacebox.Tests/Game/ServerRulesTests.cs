using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Commands;
using Spacebox.Game.Rules;
using Spacebox.Network;

namespace Spacebox.Tests.Game;

public class ServerRulesTests
{
    private const int Alice = 2;
    private const int Bob = 3;

    [Fact]
    public void Defaults_AllowEverything()
    {
        var rules = new ServerRules();
        Assert.True(rules.Pvp && rules.Explosions && rules.ExplosionBlockDamage && rules.ExplosionPlayerDamage && rules.ExplosionsRespectClaims);
    }

    [Fact]
    public void PvpOff_BlocksOtherPlayersButNotSelfOrUnknownSources()
    {
        var rules = new ServerRules { Pvp = false };

        Assert.False(GameplayRules.AllowsPlayerHit(rules, Alice, Bob));
        Assert.True(GameplayRules.AllowsPlayerHit(rules, Alice, Alice));
        Assert.True(GameplayRules.AllowsPlayerHit(rules, GameplayRules.NoPeer, Bob));
        Assert.True(GameplayRules.AllowsPlayerHit(new ServerRules(), Alice, Bob));
    }

    [Fact]
    public void ExplosionsOff_DoNothing()
    {
        var explosion = GameplayRules.ForExplosion(new ServerRules { Explosions = false }, Alice);

        Assert.False(explosion.Explode);
        Assert.False(explosion.BreakBlocks);
        Assert.False(explosion.DamagesPlayer(Bob));
        Assert.False(explosion.DamagesPlayer(Alice));
    }

    [Fact]
    public void BlockAndPlayerDamage_AreSeparateSwitches()
    {
        var noBlocks = GameplayRules.ForExplosion(new ServerRules { ExplosionBlockDamage = false }, Alice);
        Assert.False(noBlocks.BreakBlocks);
        Assert.True(noBlocks.DamagesPlayer(Bob));

        var noPlayers = GameplayRules.ForExplosion(new ServerRules { ExplosionPlayerDamage = false }, Alice);
        Assert.True(noPlayers.BreakBlocks);
        Assert.False(noPlayers.DamagesPlayer(Bob));
    }

    [Fact]
    public void PvpOff_ProtectsOthersFromAPlayersExplosion()
    {
        var explosion = GameplayRules.ForExplosion(new ServerRules { Pvp = false }, Alice);

        Assert.False(explosion.DamagesPlayer(Bob));
        Assert.True(explosion.DamagesPlayer(Alice));
        Assert.True(GameplayRules.ForExplosion(new ServerRules { Pvp = false }, GameplayRules.NoPeer).DamagesPlayer(Bob));
    }

    [Fact]
    public void ExplosionMessage_CarriesTheRulesToClients()
    {
        var rules = GameplayRules.ForExplosion(new ServerRules { Pvp = false, ExplosionsRespectClaims = false }, Alice);
        var message = new ExplosionMessage { Flags = ExplosionMessage.Pack(rules), CauserPeer = rules.CauserPeer };
        var received = message.Rules;

        Assert.Equal(rules.Explode, received.Explode);
        Assert.Equal(rules.DamagePlayers, received.DamagePlayers);
        Assert.Equal(rules.Pvp, received.Pvp);
        Assert.Equal(rules.RespectClaims, received.RespectClaims);
        Assert.Equal(Alice, received.CauserPeer);
        Assert.Equal(rules.DamagesPlayer(Bob), received.DamagesPlayer(Bob));
    }

    [Fact]
    public void ExplosionMessage_RoundTripsItsCells()
    {
        var cell = new Vector3i(10, -4, 7);
        var cells = new List<Vector3i> { cell, cell + new Vector3i(1, 0, 0), cell + new Vector3i(-2, 3, 1) };
        var message = new ExplosionMessage { EntityId = 42, Cell = cell, BombBlockId = 9, Cells = cells, Flags = 5, CauserPeer = Alice };

        var writer = new NetWriter();
        message.Serialize(writer);
        var copy = new ExplosionMessage();
        copy.Deserialize(new NetReader(writer.ToArray()));

        Assert.Equal(42, copy.EntityId);
        Assert.Equal(cell, copy.Cell);
        Assert.Equal(cells, copy.Cells);
        Assert.Equal(5, copy.Flags);
        Assert.Equal(Alice, copy.CauserPeer);
    }

    [Theory]
    [InlineData("on", true, true)]
    [InlineData("OFF", true, false)]
    [InlineData("1", true, true)]
    [InlineData("false", true, false)]
    [InlineData("maybe", false, false)]
    public void Command_ParsesSwitches(string text, bool parsed, bool value)
    {
        Assert.Equal(parsed, GameRuleCommand.TryParseSwitch(text, out bool result));
        Assert.Equal(value, result);
    }

    [Fact]
    public void RuleNames_AreUnique()
    {
        var names = ServerRulesService.Rules.Select(rule => rule.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(ServerRulesService.TryFind("PVP", out var pvp));
        Assert.Equal("pvp", pvp.Name);
    }
}

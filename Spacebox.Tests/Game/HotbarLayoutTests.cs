using System.Text.Json;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Tests.Game;

public class HotbarLayoutTests
{
    [Fact]
    public void NumberKeys_OnlyReachTheFirstTenActiveSlots()
    {
        Assert.Equal(0, HotbarLayout.SlotForNumberKey(0, 15));
        Assert.Equal(9, HotbarLayout.SlotForNumberKey(9, 15));
        Assert.Equal(-1, HotbarLayout.SlotForNumberKey(7, 5));
        Assert.Equal(-1, HotbarLayout.SlotForNumberKey(10, 15));
    }

    [Fact]
    public void Scroll_WrapsAroundTheActiveSlots()
    {
        Assert.Equal(14, HotbarLayout.Step(0, -1, 15));
        Assert.Equal(0, HotbarLayout.Step(14, 1, 15));
        Assert.Equal(11, HotbarLayout.Step(10, 1, 15));
    }

    [Fact]
    public void Layout_IsClamped()
    {
        Assert.Equal(3, HotbarLayout.ClampRows(9));
        Assert.Equal(1, HotbarLayout.ClampRows(0));
        Assert.Equal(20, HotbarLayout.ClampSlotsPerRow(99));
        Assert.Equal(0, HotbarLayout.Clamp(12, 10));
    }

    [Fact]
    public void Creative_HasOneRowOfFifteen()
    {
        var rules = GameModeRules.ForBase(GameMode.Creative);
        Assert.Equal(1, rules.HotbarRows);
        Assert.Equal(15, rules.HotbarSlots);
        Assert.Equal(10, HotbarLayout.ActiveSlots(GameModeRules.ForBase(GameMode.Survival)));
    }

    [Fact]
    public void GameModeJson_OverridesAndClampsTheHotbar()
    {
        var json = JsonSerializer.Deserialize<GameModeJSON>("{\"Id\":5,\"Key\":\"big\",\"Name\":\"Big\",\"Base\":\"survival\",\"Rules\":{\"HotbarSlots\":40,\"HotbarRows\":2}}");
        Assert.True(GameModeParser.TryConvert(json, out var mode, out var error), error);

        Assert.Equal(20, mode.Rules.HotbarSlots);
        Assert.Equal(2, mode.Rules.HotbarRows);
        Assert.Equal(40, HotbarLayout.ActiveSlots(mode.Rules));
    }

    [Fact]
    public void Bars_KeepTheWidthOfTenSlots()
    {
        float ten = Spacebox.Game.GUI.Canvas.HudView.BarsWidth(43f, 4f, 10, 12f);

        Assert.Equal(12f + 430f + 36f, ten);
        Assert.Equal(ten, Spacebox.Game.GUI.Canvas.HudView.BarsWidth(43f, 4f, 15, 12f));
        Assert.Equal(ten, Spacebox.Game.GUI.Canvas.HudView.BarsWidth(43f, 4f, 20, 12f));
        Assert.True(Spacebox.Game.GUI.Canvas.HudView.BarsWidth(43f, 4f, 5, 12f) < ten);
    }
}

using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Tests.GUI;

[Collection(Spacebox.Tests.GameAssetsCollection.Name)]
public class GameUiTests
{
    [Fact]
    public void NewContext_CreatesEveryControllerOnceWithoutGraphics()
    {
        var ui = new GameUi();

        Assert.NotEmpty(ui.Controllers);
        Assert.Equal(ui.Controllers.Count, ui.Controllers.Distinct().Count());
        Assert.Contains(ui.Panel, ui.Controllers);
        Assert.Contains(ui.Chat, ui.Controllers);
        Assert.Contains(ui.Pause, ui.Controllers);
        Assert.Contains(ui.Loading, ui.Controllers);
        Assert.Null(ui.Radar);
        Assert.NotSame(ui, GameUi.Current);
    }

    [Fact]
    public void Controllers_UpdateInTheOrderTheSceneUsedToCallThem()
    {
        var ui = new GameUi();
        int Index(IGameUiController controller) => ui.Controllers.ToList().IndexOf(controller);

        Assert.True(Index(ui.Chat) < Index(ui.Panel));
        Assert.True(Index(ui.Panel) < Index(ui.Tutorial));
        Assert.True(Index(ui.Tutorial) < Index(ui.Waypoints));
        Assert.True(Index(ui.Waypoints) < Index(ui.ShipLibrary));
        Assert.True(Index(ui.Processing) < Index(ui.Crafting));
        Assert.True(Index(ui.Inventory) < Index(ui.Storage));
        Assert.True(Index(ui.Pause) < Index(ui.Generator));
        Assert.True(Index(ui.ShipConsole) < Index(ui.DnaReconstructor));
    }

    [Fact]
    public void Facades_ForwardToTheCurrentContextAndIgnoreCallsWithoutOne()
    {
        var ui = new GameUi();
        ui.MakeCurrent();
        try
        {
            Chat.Write("hello", Color4.Yellow);
            Assert.Equal(1, ui.Chat.Log.Count);
            Assert.True(ui.Chat.IsVisible);

            PanelUI.AllowScroll = false;
            Assert.False(ui.Panel.AllowScroll);
            Assert.False(WorldLoadingScreen.IsActive);
        }
        finally
        {
            ui.Reset();
        }

        Assert.Null(GameUi.Current);
        Assert.Equal(0, ui.Chat.Log.Count);
        Chat.Write("lost");
        Assert.True(PanelUI.AllowScroll);
        Assert.Equal((short)0, PanelUI.SelectedSlotId);
        Assert.Null(PanelUI.CurrentSlot());
        Assert.False(WorldLoadingScreen.IsActive);
    }

    [Fact]
    public void Contexts_DoNotShareState()
    {
        var first = new GameUi();
        var second = new GameUi();

        first.Chat.Write("only here");
        first.HitMarker.Show(true);
        first.Statistics.Show(new PlayerStatistics());

        Assert.Equal(0, second.Chat.Log.Count);
        Assert.False(second.HitMarker.IsVisible);
        Assert.False(second.Statistics.IsVisible);
    }

    [Fact]
    public void HitMarker_HidesAfterItsDuration()
    {
        var marker = new HitMarkerUI();
        marker.Show(true);
        Assert.True(marker.IsVisible);
        Assert.True(marker.IsHeadshot);

        marker.Update(0.1f);
        Assert.True(marker.IsVisible);
        marker.Update(0.15f);
        Assert.False(marker.IsVisible);

        marker.Show(false);
        marker.Reset();
        Assert.False(marker.IsVisible);
    }

    [Fact]
    public void RepairTarget_ShowsRepairProgressUntilReset()
    {
        var target = new RepairTargetUI();
        target.Show("Hull", 50, 100, true);

        Assert.True(target.IsVisible);
        Assert.True(target.IsRepairing);
        Assert.Equal(0.5f, target.Fill);
        Assert.StartsWith("Repairing... ", target.Status);

        target.Reset();
        Assert.False(target.IsVisible);
        Assert.False(target.IsRepairing);
    }

    [Fact]
    public void Statistics_AreVisibleOnlyWithData()
    {
        var statistics = new StatisticsUI();
        statistics.IsVisible = true;
        Assert.False(statistics.IsVisible);

        var data = new PlayerStatistics();
        statistics.Show(data);
        Assert.True(statistics.IsVisible);
        Assert.Same(data, statistics.Statistics);

        statistics.Reset();
        Assert.False(statistics.IsVisible);
        Assert.Null(statistics.Statistics);
    }

    [Fact]
    public void ChatCommandInput_KeepsHistoryPerChat()
    {
        var first = new GameUi().Chat.Commands;
        var second = new GameUi().Chat.Commands;

        first.Sent("hello");

        Assert.Equal("hello", first.Navigate("", true));
        Assert.Equal("draft", second.Navigate("draft", true));
        Assert.Empty(first.Suggestions);
    }

    [Fact]
    public void Chat_ClearsItsLogOnReset()
    {
        var ui = new GameUi();
        ui.Chat.Write("one");
        ui.Chat.Write("two", Color4.Red);
        Assert.Equal(2, ui.Chat.Log.Count);

        ((IGameUiController)ui.Chat).Reset();
        Assert.Equal(0, ui.Chat.Log.Count);
    }
}

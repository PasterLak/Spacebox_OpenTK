using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Canvas;
using Spacebox.Game.GUI.Canvas.Screens;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Tests.GUI;

public class GameScreensLayoutTests
{
    private static readonly LayoutRules Rules = new LayoutRules { MinHitTargetPixels = 12f };
    private readonly GameUi _ui = new GameUi();

    public static IEnumerable<object[]> Designs() => new[] { new object[] { GameUiDesign.Classic }, new object[] { GameUiDesign.Modern } };

    private static CanvasRoot Root(GameUiDesign design, Control screen)
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(design, ShowcaseFixture.Assets.Font) };
        GameCanvas.Configure(root.Stretch);
        root.Add(screen, UiLayer.Popup);
        screen.Visible = true;
        return root;
    }

    private static PlayerStatistics SampleStatistics()
    {
        var statistics = new PlayerStatistics { PlayTimeSeconds = 7300, SessionsPlayed = 4, BlocksPlaced = 123456, DistanceTraveled = 12345 };
        statistics.DeathCauses["Explosion"] = 3;
        statistics.DeathCauses["Suffocation"] = 1;
        return statistics;
    }

    private static List<PlayerListEntry> SamplePlayers() => new List<PlayerListEntry>
    {
        new PlayerListEntry { Peer = 0, Name = "Host", IsHost = true },
        new PlayerListEntry { Peer = 1, Name = "Pilot", Ping = 45 },
        new PlayerListEntry { Peer = 2, Name = "VeryLongPlayerNameForTheList", Ping = 260, IsMuted = true }
    };

    [Theory]
    [MemberData(nameof(Designs))]
    public void PauseMenu_WithStatisticsAndPlayers_LaysOutCleanly(GameUiDesign design)
    {
        var pause = new PauseScreen(_ui.Pause, _ui.Statistics);
        pause.Statistics.Visible = true;
        pause.Statistics.Show(StatisticsRows.Build(SampleStatistics(), "Day 3, 12:00"));
        pause.Players.Visible = true;
        pause.Players.Show(SamplePlayers(), 0, true);

        var report = LayoutHarness.ValidateAll(Root(design, pause), ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void WelcomeScreen_LaysOutCleanly(GameUiDesign design)
    {
        var report = LayoutHarness.ValidateAll(Root(design, new WelcomeScreen(_ui.Welcome)), ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void DeathAndLoadingScreens_LayOutCleanly(GameUiDesign design)
    {
        Assert.True(LayoutHarness.ValidateAll(Root(design, new DeathScreenView()), ScreenProfiles.All, Rules) is var death && death.IsClean, death.Format());
        Assert.True(LayoutHarness.ValidateAll(Root(design, new LoadingScreenView(_ui.Loading)), ScreenProfiles.All, Rules) is var loading && loading.IsClean, loading.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void TutorialPrompt_LaysOutCleanly(GameUiDesign design)
    {
        var report = LayoutHarness.ValidateAll(Root(design, new TutorialPromptScreen(_ui.TutorialPrompt)), ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void TutorialMenu_LaysOutCleanly(GameUiDesign design)
    {
        var report = LayoutHarness.ValidateAll(Root(design, new TutorialMenuScreen(_ui.TutorialMenu, _ui.Pause)), ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }
}

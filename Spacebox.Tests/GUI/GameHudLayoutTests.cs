using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Spacebox.Game;
using Spacebox.Game.GUI.Canvas;

namespace Spacebox.Tests.GUI;

public class GameHudLayoutTests
{
    public static IEnumerable<object[]> Designs() => new[] { new object[] { GameUiDesign.Classic }, new object[] { GameUiDesign.Modern } };

    private static CanvasRoot Build(GameUiDesign design, float uiScale)
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(design, ShowcaseFixture.Assets.Font) };
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = GameCanvas.VirtualSize;
        root.Stretch.UiScale = uiScale;
        var hud = root.Add(new HudView(new Spacebox.Game.GUI.GameUi()), UiLayer.Hud);
        hud.SetAnchorsPreset(LayoutPreset.FullRect);
        hud.Present(new Storage(1, 10), 3, true);
        hud.SetBars((100, 100, null), (100, 100, "Infinite"), (37, 100, null));
        return root;
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void Hud_LaysOutCleanlyOnEveryScreen(GameUiDesign design)
    {
        var report = LayoutHarness.ValidateAll(Build(design, 1f), ScreenProfiles.All);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void Hud_FitsAtTheLargestUiScale(GameUiDesign design)
    {
        var report = LayoutHarness.ValidateAll(Build(design, 2f), ScreenProfiles.Quick);
        Assert.True(report.IsClean, report.Format());
    }
}

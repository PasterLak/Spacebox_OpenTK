using Engine.CanvasUI;
using Spacebox.Game;
using Spacebox.Game.GUI.Canvas;

namespace Spacebox.Tests.GUI;

public class GameThemeStyleTests
{
    private static StyleBox Style(Theme theme, string type, string name)
    {
        var control = new Panel { ThemeTypeVariation = type, Theme = theme };
        return control.GetThemeStyleBox(name);
    }

    [Fact]
    public void ClassicSlots_AreSunkenAndDarkerThanButtons()
    {
        var theme = GameCanvasThemes.Create(GameUiDesign.Classic, null);
        var slot = Assert.IsType<StyleBoxBevel>(Style(theme, "Slot", "normal"));
        var button = Assert.IsType<StyleBoxBevel>(Style(theme, "Button", "normal"));

        Assert.True(slot.Sunken);
        Assert.False(button.Sunken);
        Assert.True(slot.DarkColor.R < slot.LightColor.R);
        Assert.True(slot.BgColor.R < button.BgColor.R);
    }

    [Fact]
    public void ClassicTooltip_IsBevelledWithDarkText()
    {
        var theme = GameCanvasThemes.Create(GameUiDesign.Classic, null);
        Assert.IsType<StyleBoxBevel>(Style(theme, "TooltipPanel", "panel"));
        var label = new Label { ThemeTypeVariation = "TooltipLabel", Theme = theme };
        Assert.True(label.GetThemeColor("font_color", default).R < 0.5f);
    }

    [Fact]
    public void ModernTooltip_StaysDark()
    {
        var theme = GameCanvasThemes.Create(GameUiDesign.Modern, null);
        Assert.IsType<StyleBoxFlat>(Style(theme, "TooltipPanel", "panel"));
    }
}

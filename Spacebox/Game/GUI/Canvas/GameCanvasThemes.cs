using Engine.CanvasUI;
using OpenTK.Mathematics;
using CanvasTheme = Engine.CanvasUI.Theme;

namespace Spacebox.Game.GUI.Canvas;

public static class GameCanvasThemes
{
    public const string FontPath = "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf";
    public const float FontSize = 12f;

    public static readonly Color4 Gold = new Color4(1f, 0.77f, 0.16f, 1f);
    public static readonly Color4 Health = new Color4(0.86f, 0.12f, 0.12f, 1f);
    public static readonly Color4 Power = new Color4(0f, 0.85f, 0.95f, 1f);
    public static readonly Color4 Oxygen = new Color4(0.1f, 0.35f, 1f, 1f);

    public static CanvasTheme Create(GameUiDesign design, CanvasFont font)
    {
        var theme = new CanvasTheme { DefaultFont = font, DefaultFontSize = FontSize };
        if (design == GameUiDesign.Modern) ApplyModern(theme);
        else ApplyClassic(theme);
        return theme;
    }

    private static void ApplyClassic(CanvasTheme theme)
    {
        var background = ToColor(Theme_Background());
        var deep = ToColor(Theme_Deep());
        var light = new Color4(0.9f, 0.9f, 0.9f, 1f);
        var dark = new Color4(0.5f, 0.5f, 0.5f, 1f);

        theme.SetColor("Control", "font_color", Color4.White);
        theme.SetColor("Label", "font_color", Color4.White);
        theme.SetColor("Control", "font_shadow_color", new Color4(0f, 0f, 0f, 0.85f));
        theme.SetColor("Control", "slot_count_color", Color4.White);
        theme.SetColor("Control", "bar_back_color", new Color4(0.2f, 0.2f, 0.2f, 0.85f));
        theme.SetColor("Control", "font_header_color", Gold);

        theme.SetStyleBox("Slot", "normal", new StyleBoxBevel(deep, dark, light, 2f, true));
        theme.SetStyleBox("Slot", "hover", new StyleBoxBevel(new Color4(0.75f, 0.75f, 0.75f, 1f), dark, light, 2f, true));
        theme.SetStyleBox("Slot", "selected", new StyleBoxBevel(new Color4(0.78f, 0.78f, 0.78f, 1f), Gold, Gold, 3f));
        theme.SetStyleBox("HudPanel", "panel", new StyleBoxBevel(background, light, dark, 2f));
        theme.SetStyleBox("PanelContainer", "panel", new StyleBoxBevel(background, light, dark, 2f).SetContentMargins(8f));
        theme.SetConstant("Control", "slot_spacing", 0f);
    }

    private static void ApplyModern(CanvasTheme theme)
    {
        var panel = new Color4(0.06f, 0.07f, 0.1f, 0.82f);
        var border = new Color4(0.35f, 0.38f, 0.46f, 0.9f);

        theme.SetColor("Control", "font_color", new Color4(0.94f, 0.94f, 0.96f, 1f));
        theme.SetColor("Label", "font_color", new Color4(0.94f, 0.94f, 0.96f, 1f));
        theme.SetColor("Control", "font_shadow_color", new Color4(0f, 0f, 0f, 0.7f));
        theme.SetColor("Control", "slot_count_color", Color4.White);
        theme.SetColor("Control", "bar_back_color", new Color4(0.08f, 0.09f, 0.12f, 0.85f));
        theme.SetColor("Control", "font_header_color", Gold);

        theme.SetStyleBox("Slot", "normal", new StyleBoxFlat(new Color4(0.1f, 0.11f, 0.15f, 0.85f)).SetBorder(1f, border));
        theme.SetStyleBox("Slot", "hover", new StyleBoxFlat(new Color4(0.16f, 0.17f, 0.22f, 0.9f)).SetBorder(1f, new Color4(0.6f, 0.62f, 0.7f, 1f)));
        theme.SetStyleBox("Slot", "selected", new StyleBoxFlat(new Color4(0.2f, 0.17f, 0.08f, 0.9f)).SetBorder(2f, Gold));
        theme.SetStyleBox("HudPanel", "panel", new StyleBoxFlat(panel).SetBorder(1f, border));
        theme.SetStyleBox("PanelContainer", "panel", new StyleBoxFlat(panel, 8f).SetBorder(1f, border));
        theme.SetConstant("Control", "slot_spacing", 3f);
    }

    private static Engine.Color4Byte Theme_Background() => Spacebox.Game.GUI.Theme.Colors.Background;

    private static Engine.Color4Byte Theme_Deep() => Spacebox.Game.GUI.Theme.Colors.Deep;

    private static Color4 ToColor(Engine.Color4Byte color) => new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
}

using Engine.CanvasUI;
using OpenTK.Mathematics;
using CanvasTheme = Engine.CanvasUI.Theme;

namespace Spacebox.Game.GUI.Canvas;

public static class GameControlStyles
{
    public static void Apply(CanvasTheme theme, GamePalette p)
    {
        theme.SetColor("Control", "font_color", p.Text);
        theme.SetColor("Label", "font_color", p.Text);
        theme.SetColor("Control", "font_secondary_color", p.TextMuted);
        theme.SetColor("Control", "font_header_color", p.Bevel ? p.Text : p.Accent);
        theme.SetColor("Control", "placeholder_color", p.TextMuted);
        theme.SetColor("Control", "selection_color", WithAlpha(p.Selected, 0.45f));
        theme.SetColor("Control", "caret_color", p.FieldText);
        theme.SetColor("Control", "selected_color", p.Selected);
        theme.SetColor("Control", "selected_unfocused_color", WithAlpha(p.Selected, 0.7f));
        theme.SetColor("Control", "font_selected_color", p.SelectedText);
        theme.SetColor("Control", "hover_color", p.Hover);
        theme.SetColor("Control", "check_color", p.Bevel ? p.Text : p.Accent);
        theme.SetColor("Control", "scroll_track", WithAlpha(p.PanelDark, 0.35f));
        theme.SetColor("Control", "scroll_thumb", p.Bevel ? p.PanelDark : p.PanelLight);
        theme.SetColor("Control", "track_color", p.Bevel ? p.PanelDark : p.Field);
        theme.SetColor("Control", "fill_color", p.Accent);
        theme.SetColor("Control", "color", p.Bevel ? p.PanelLight : p.Accent);
        theme.SetColor("Control", "tab_line_color", p.Accent);

        theme.SetColor("Button", "font_color", p.ButtonText);
        theme.SetColor("Button", "font_hover_color", p.ButtonText);
        theme.SetColor("Button", "font_disabled_color", p.ButtonDisabledText);
        theme.SetColor("LineEdit", "font_color", p.FieldText);
        theme.SetColor("TextEdit", "font_color", p.FieldText);
        theme.SetColor("NumberEdit", "font_color", p.FieldText);
        theme.SetColor("ComboBox", "font_color", p.ButtonText);

        theme.SetStyleBox("Button", "normal", Raised(p, p.Button, 10f, 4f));
        theme.SetStyleBox("Button", "hover", Raised(p, p.ButtonHover, 10f, 4f, true));
        theme.SetStyleBox("Button", "pressed", Sunken(p, p.ButtonPressed, 10f, 4f));
        theme.SetStyleBox("Button", "disabled", Raised(p, WithAlpha(p.Button, 0.6f), 10f, 4f));
        theme.SetStyleBox("Button", "focus", Raised(p, p.ButtonHover, 10f, 4f, true));

        theme.SetTypeVariation("SmallButton", "Button");
        theme.SetStyleBox("SmallButton", "normal", Raised(p, p.Button, 4f, 1f));
        theme.SetStyleBox("SmallButton", "hover", Raised(p, p.ButtonHover, 4f, 1f, true));
        theme.SetStyleBox("SmallButton", "pressed", Sunken(p, p.ButtonPressed, 4f, 1f));
        theme.SetStyleBox("SmallButton", "disabled", Raised(p, WithAlpha(p.Button, 0.6f), 4f, 1f));

        foreach (var type in new[] { "LineEdit", "TextEdit", "NumberEdit" })
        {
            theme.SetStyleBox(type, "normal", Sunken(p, p.Field, 6f, 4f));
            theme.SetStyleBox(type, "focus", Focused(p, p.Field, 6f, 4f));
            theme.SetStyleBox(type, "read_only", Sunken(p, WithAlpha(p.Field, 0.7f), 6f, 4f));
        }

        theme.SetStyleBox("ComboBox", "normal", Raised(p, p.Button, 6f, 4f));
        theme.SetStyleBox("ComboBox", "hover", Raised(p, p.ButtonHover, 6f, 4f, true));
        theme.SetStyleBox("ComboBox", "arrow", Raised(p, p.Button, 0f));
        theme.SetStyleBox("ComboBox", "arrow_hover", Raised(p, p.ButtonHover, 0f, -1f, true));

        theme.SetStyleBox("CheckBox", "box", Sunken(p, p.Field, 0f));
        theme.SetStyleBox("CheckBox", "box_hover", Focused(p, p.Field, 0f, -1f));
        theme.SetStyleBox("ItemList", "panel", Sunken(p, p.List, 2f));
        theme.SetStyleBox("Panel", "panel", Raised(p, p.Panel, 0f));
        theme.SetStyleBox("PanelContainer", "panel", Raised(p, p.Panel, 12f));
        theme.SetStyleBox("PopupPanel", "panel", Raised(p, p.Panel, 6f));
        theme.SetStyleBox("PopupMenu", "panel", Raised(p, p.Panel, 3f));
        theme.SetTypeVariation("MenuItem", "Button");
        theme.SetStyleBox("MenuItem", "normal", new StyleBoxFlat(new Color4(0f, 0f, 0f, 0f)).SetContentMargins(8f, 3f));
        theme.SetStyleBox("MenuItem", "hover", new StyleBoxFlat(p.Selected).SetContentMargins(8f, 3f));
        theme.SetStyleBox("MenuItem", "pressed", new StyleBoxFlat(p.Selected).SetContentMargins(8f, 3f));
        theme.SetStyleBox("TooltipPanel", "panel", new StyleBoxFlat(new Color4(0.05f, 0.05f, 0.07f, 0.96f), 6f).SetBorder(1f, new Color4(0.4f, 0.42f, 0.5f, 1f)));
        theme.SetColor("TooltipPanel", "font_color", new Color4(0.95f, 0.95f, 0.97f, 1f));
        theme.SetColor("Label", "tooltip_font_color", new Color4(0.95f, 0.95f, 0.97f, 1f));
        theme.SetTypeVariation("TooltipLabel", "Label");
        theme.SetColor("TooltipLabel", "font_color", new Color4(0.95f, 0.95f, 0.97f, 1f));

        theme.SetStyleBox("TabContainer", "tab", Raised(p, p.Button, 10f, 4f));
        theme.SetStyleBox("TabContainer", "tab_hover", Raised(p, p.ButtonHover, 10f, 4f, true));
        theme.SetStyleBox("TabContainer", "tab_selected", Raised(p, p.Selected, 10f, 4f));
        theme.SetStyleBox("FoldableContainer", "header", new StyleBoxFlat(p.Hover, 4f));
        theme.SetStyleBox("FoldableContainer", "header_hover", new StyleBoxFlat(WithAlpha(p.Hover, p.Hover.A * 2f), 4f));

        theme.SetTypeVariation("MenuTitle", "Label");
        theme.SetColor("MenuTitle", "font_color", p.TitleText);
        theme.SetTypeVariation("SectionLabel", "Label");
        theme.SetColor("SectionLabel", "font_color", p.Heading);
        theme.SetTypeVariation("MutedLabel", "Label");
        theme.SetColor("MutedLabel", "font_color", p.TextMuted);
        theme.SetTypeVariation("ErrorLabel", "Label");
        theme.SetColor("ErrorLabel", "font_color", p.Error);
        theme.SetTypeVariation("WarningLabel", "Label");
        theme.SetColor("WarningLabel", "font_color", p.Warning);
        theme.SetTypeVariation("SuccessLabel", "Label");
        theme.SetColor("SuccessLabel", "font_color", p.Success);
        theme.SetTypeVariation("TitleBar", "PanelContainer");
        theme.SetStyleBox("TitleBar", "panel", new StyleBoxFlat(p.TitleBar).SetContentMargins(10f, 4f));
        theme.SetTypeVariation("InsetPanel", "PanelContainer");
        theme.SetStyleBox("InsetPanel", "panel", Sunken(p, p.List, 8f, 6f));

        theme.SetConstant("BoxContainer", "separation", 6f);
        theme.SetConstant("GridContainer", "h_separation", 8f);
        theme.SetConstant("GridContainer", "v_separation", 6f);
        theme.SetConstant("Control", "scrollbar_width", 8f);
        theme.SetConstant("Control", "box_size", 14f);
        theme.SetConstant("Control", "v_padding", 6f);
    }

    public static StyleBox Raised(GamePalette p, Color4 color, float horizontal, float vertical = -1f, bool highlight = false)
    {
        StyleBox box = p.Bevel
            ? new StyleBoxBevel(color, highlight ? p.Accent : p.PanelLight, highlight ? p.Accent : p.PanelDark, 2f)
            : new StyleBoxFlat(color).SetBorder(1f, highlight ? p.Accent : p.PanelLight);
        return box.SetContentMargins(horizontal, vertical < 0f ? horizontal : vertical);
    }

    public static StyleBox Sunken(GamePalette p, Color4 color, float horizontal, float vertical = -1f)
    {
        StyleBox box = p.Bevel
            ? new StyleBoxBevel(color, p.PanelLight, p.PanelDark, 2f, true)
            : new StyleBoxFlat(color).SetBorder(1f, p.PanelDark);
        return box.SetContentMargins(horizontal, vertical < 0f ? horizontal : vertical);
    }

    private static StyleBox Focused(GamePalette p, Color4 color, float horizontal, float vertical = -1f)
    {
        StyleBox box = p.Bevel
            ? new StyleBoxBevel(color, p.Accent, p.Accent, 2f, true)
            : new StyleBoxFlat(color).SetBorder(1f, p.Accent);
        return box.SetContentMargins(horizontal, vertical < 0f ? horizontal : vertical);
    }

    private static Color4 WithAlpha(Color4 color, float alpha) => new Color4(color.R, color.G, color.B, Math.Clamp(alpha, 0f, 1f));
}

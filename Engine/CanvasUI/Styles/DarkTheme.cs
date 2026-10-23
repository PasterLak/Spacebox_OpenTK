using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public static class DarkTheme
{
    public const string FontPath = "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf";
    public const float FontSize = 12f;

    public static readonly Color4 Background = new Color4(0.075f, 0.08f, 0.095f, 1f);
    public static readonly Color4 Panel = new Color4(0.11f, 0.12f, 0.14f, 1f);
    public static readonly Color4 Field = new Color4(0.16f, 0.17f, 0.2f, 1f);
    public static readonly Color4 FieldHover = new Color4(0.2f, 0.21f, 0.25f, 1f);
    public static readonly Color4 Border = new Color4(0.25f, 0.27f, 0.32f, 1f);
    public static readonly Color4 Accent = new Color4(1f, 0.69f, 0.2f, 1f);
    public static readonly Color4 AccentDark = new Color4(0.62f, 0.38f, 0.08f, 1f);
    public static readonly Color4 Text = new Color4(0.9f, 0.91f, 0.93f, 1f);
    public static readonly Color4 TextDim = new Color4(0.6f, 0.62f, 0.67f, 1f);
    public static readonly Color4 Error = new Color4(1f, 0.42f, 0.32f, 1f);
    public static readonly Color4 Success = new Color4(0.45f, 0.85f, 0.45f, 1f);
    public static readonly Color4 Warning = new Color4(1f, 0.75f, 0.3f, 1f);

    public static Theme Create(CanvasFont font)
    {
        var theme = new Theme { DefaultFont = font, DefaultFontSize = FontSize };

        theme.SetColor("Control", "font_color", Text);
        theme.SetColor("Label", "font_color", Text);
        theme.SetColor("Button", "font_color", Text);
        theme.SetColor("Button", "font_hover_color", Color4.White);
        theme.SetColor("Button", "font_disabled_color", new Color4(0.45f, 0.47f, 0.5f, 1f));
        theme.SetColor("Control", "font_secondary_color", TextDim);
        theme.SetColor("Control", "placeholder_color", new Color4(0.5f, 0.52f, 0.56f, 1f));
        theme.SetColor("Control", "selection_color", new Color4(0.85f, 0.55f, 0.12f, 0.45f));
        theme.SetColor("Control", "caret_color", Accent);
        theme.SetColor("Control", "selected_color", new Color4(0.75f, 0.47f, 0.1f, 0.85f));
        theme.SetColor("Control", "selected_unfocused_color", new Color4(0.55f, 0.35f, 0.1f, 0.6f));
        theme.SetColor("Control", "hover_color", new Color4(1f, 1f, 1f, 0.06f));
        theme.SetColor("Control", "font_header_color", Accent);
        theme.SetColor("Control", "check_color", Accent);
        theme.SetColor("Control", "tab_line_color", AccentDark);
        theme.SetColor("Control", "font_selected_color", Color4.White);
        theme.SetColor("Control", "handle_color", new Color4(1f, 1f, 1f, 0.06f));
        theme.SetColor("Control", "handle_hover_color", Accent);

        var field = Flat(Field, 4f, 3f).SetBorder(1f, Border);
        var focus = Flat(Field, 4f, 3f).SetBorder(1f, Accent);
        var readOnly = Flat(new Color4(0.12f, 0.13f, 0.15f, 1f), 4f, 3f).SetBorder(1f, Border);
        foreach (var type in new[] { "LineEdit", "TextEdit", "NumberEdit" })
        {
            theme.SetStyleBox(type, "normal", field);
            theme.SetStyleBox(type, "focus", focus);
            theme.SetStyleBox(type, "read_only", readOnly);
        }

        theme.SetStyleBox("Button", "normal", Flat(new Color4(0.2f, 0.21f, 0.25f, 1f), 8f, 3f).SetBorder(1f, Border));
        theme.SetStyleBox("Button", "hover", Flat(new Color4(0.26f, 0.27f, 0.32f, 1f), 8f, 3f).SetBorder(1f, new Color4(0.45f, 0.47f, 0.52f, 1f)));
        theme.SetStyleBox("Button", "pressed", Flat(AccentDark, 8f, 3f).SetBorder(1f, Accent));
        theme.SetStyleBox("Button", "disabled", Flat(new Color4(0.14f, 0.15f, 0.17f, 1f), 8f, 3f).SetBorder(1f, new Color4(0.2f, 0.21f, 0.24f, 1f)));

        theme.SetTypeVariation("MenuItem", "Button");
        theme.SetStyleBox("MenuItem", "normal", Flat(new Color4(0f, 0f, 0f, 0f), 8f, 3f));
        theme.SetStyleBox("MenuItem", "hover", Flat(AccentDark, 8f, 3f));
        theme.SetStyleBox("MenuItem", "pressed", Flat(Accent, 8f, 3f));
        theme.SetStyleBox("MenuItem", "disabled", Flat(new Color4(0f, 0f, 0f, 0f), 8f, 3f));

        theme.SetTypeVariation("NavButton", "Button");
        theme.SetStyleBox("NavButton", "normal", Flat(new Color4(0f, 0f, 0f, 0f), 10f, 4f));
        theme.SetStyleBox("NavButton", "hover", Flat(new Color4(1f, 1f, 1f, 0.06f), 10f, 4f));
        theme.SetStyleBox("NavButton", "pressed", Flat(AccentDark, 10f, 4f));

        theme.SetTypeVariation("SmallButton", "Button");
        theme.SetStyleBox("SmallButton", "normal", Flat(new Color4(0.2f, 0.21f, 0.25f, 1f), 4f, 1f));
        theme.SetStyleBox("SmallButton", "hover", Flat(new Color4(0.28f, 0.29f, 0.34f, 1f), 4f, 1f));
        theme.SetStyleBox("SmallButton", "pressed", Flat(AccentDark, 4f, 1f));
        theme.SetStyleBox("SmallButton", "disabled", Flat(new Color4(0.14f, 0.15f, 0.17f, 1f), 4f, 1f));

        theme.SetStyleBox("Panel", "panel", Flat(Panel, 0f).SetBorder(1f, Border));
        theme.SetStyleBox("PanelContainer", "panel", Flat(Panel, 8f).SetBorder(1f, Border));
        theme.SetStyleBox("PopupPanel", "panel", Flat(new Color4(0.1f, 0.11f, 0.13f, 0.98f), 4f).SetBorder(1f, Accent));
        theme.SetStyleBox("PopupMenu", "panel", Flat(new Color4(0.1f, 0.11f, 0.13f, 0.98f), 3f).SetBorder(1f, Accent));
        theme.SetStyleBox("TooltipPanel", "panel", Flat(new Color4(0.06f, 0.065f, 0.08f, 0.97f), 6f).SetBorder(1f, Border));
        theme.SetStyleBox("ItemList", "panel", Flat(new Color4(0.09f, 0.095f, 0.11f, 1f), 0f).SetBorder(1f, Border));

        theme.SetStyleBox("ComboBox", "arrow", Flat(new Color4(0.2f, 0.21f, 0.25f, 1f), 0f).SetBorder(1f, Border));
        theme.SetStyleBox("ComboBox", "arrow_hover", Flat(new Color4(0.28f, 0.29f, 0.34f, 1f), 0f).SetBorder(1f, Accent));

        theme.SetStyleBox("CheckBox", "box", Flat(Field, 0f).SetBorder(1f, Border));
        theme.SetStyleBox("CheckBox", "box_hover", Flat(FieldHover, 0f).SetBorder(1f, Accent));

        theme.SetStyleBox("TabContainer", "tab", Flat(new Color4(0.15f, 0.16f, 0.19f, 1f), 0f));
        theme.SetStyleBox("TabContainer", "tab_hover", Flat(new Color4(0.21f, 0.22f, 0.26f, 1f), 0f));
        theme.SetStyleBox("TabContainer", "tab_selected", Flat(AccentDark, 0f));

        theme.SetStyleBox("FoldableContainer", "header", Flat(new Color4(1f, 1f, 1f, 0.03f), 0f));
        theme.SetStyleBox("FoldableContainer", "header_hover", Flat(new Color4(1f, 1f, 1f, 0.07f), 0f));

        theme.SetConstant("BoxContainer", "separation", 4f);
        theme.SetConstant("GridContainer", "h_separation", 6f);
        theme.SetConstant("GridContainer", "v_separation", 3f);
        theme.SetConstant("Control", "scrollbar_width", 8f);
        theme.SetConstant("Control", "box_size", 14f);
        return theme;
    }

    private static StyleBoxFlat Flat(Color4 color, float horizontal, float vertical = -1f)
    {
        var style = new StyleBoxFlat(color);
        style.SetContentMargins(horizontal, vertical < 0f ? horizontal : vertical);
        return style;
    }
}

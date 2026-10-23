using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class MenuWindowFrame
{
    public PanelContainer Root { get; init; }
    public Label Title { get; init; }
    public VBoxContainer Body { get; init; }
    public HBoxContainer Footer { get; init; }
}

public static class MenuLayout
{
    public const float ButtonHeight = 30f;

    public static MenuWindowFrame Window(string title, float width, float minHeight = 0f)
    {
        var root = new PanelContainer { CustomMinimumSize = new Vector2(width, minHeight), SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10f);
        root.AddChild(column);

        Label titleLabel = null;
        if (!string.IsNullOrEmpty(title))
        {
            var bar = new PanelContainer { ThemeTypeVariation = "TitleBar" };
            titleLabel = new Label { Text = title, ThemeTypeVariation = "MenuTitle", HorizontalAlignment = HorizontalAlignment.Center };
            bar.AddChild(titleLabel);
            column.AddChild(bar);
        }

        var body = new VBoxContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
        body.AddThemeConstantOverride("separation", 8f);
        column.AddChild(body);

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 8f);
        column.AddChild(footer);

        return new MenuWindowFrame { Root = root, Title = titleLabel, Body = body, Footer = footer };
    }

    public static Button Button(GameMenu menu, string text, Action onClick, bool expand = true)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0f, ButtonHeight) };
        if (expand) button.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        button.Pressed += _ =>
        {
            menu?.Click1?.Play();
            onClick?.Invoke();
        };
        return button;
    }

    public static PanelContainer ScrollList(Control content, float height)
    {
        var box = new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(0f, height) };
        var scroll = new ScrollContainer();
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand }.SetMargins(0f, 0f, 14f, 0f);
        margin.AddChild(content);
        scroll.AddChild(margin);
        box.AddChild(scroll);
        return box;
    }

    public static Button IconButton(GameMenu menu, string iconPath, string tooltip, Action onClick)
    {
        var button = Button(menu, "", onClick, false);
        button.Icon = MenuTextures.Load(iconPath);
        button.IconSize = new Vector2(20f, 20f);
        button.TooltipText = tooltip;
        button.CustomMinimumSize = new Vector2(ButtonHeight + 6f, ButtonHeight);
        return button;
    }

    public static Button InlineIconButton(GameMenu menu, string iconPath, string tooltip, Action onClick)
    {
        var button = Button(menu, "", onClick, false);
        button.ThemeTypeVariation = "SmallButton";
        button.Icon = MenuTextures.Load(iconPath);
        button.IconSize = new Vector2(14f, 14f);
        button.TooltipText = tooltip;
        button.CustomMinimumSize = new Vector2(26f, 0f);
        button.SizeFlagsVertical = SizeFlags.Fill;
        return button;
    }

    public static Label Caption(string text) => new Label { Text = text, ThemeTypeVariation = "MutedLabel" };

    public static VBoxContainer Field(string caption, Control control)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 3f);
        box.AddChild(Caption(caption));
        control.SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand;
        box.AddChild(control);
        return box;
    }

    public static Control Spacer() => new Control { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore };
}

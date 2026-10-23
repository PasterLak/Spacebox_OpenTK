using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Windows;

public abstract class GameWindow : PanelContainer
{
    protected GameWindow(string title)
    {
        MouseFilter = MouseFilter.Stop;
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkEnd;
        Visible = false;

        var column = AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 6f);

        var bar = column.AddChild(new PanelContainer { ThemeTypeVariation = "TitleBar" });
        var row = bar.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        TitleLabel = row.AddChild(new Label { Text = title, ThemeTypeVariation = "MenuTitle", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
        Header = row.AddChild(new HBoxContainer { CustomMinimumSize = new Vector2(0f, HeaderButtonSize) });
        Header.AddThemeConstantOverride("separation", 4f);

        Body = column.AddChild(new VBoxContainer());
        Body.AddThemeConstantOverride("separation", 6f);
    }

    protected Label TitleLabel { get; }
    protected HBoxContainer Header { get; }
    protected VBoxContainer Body { get; }

    public abstract bool IsOpen { get; }

    public void Sync()
    {
        bool open = IsOpen;
        if (Visible != open) Visible = open;
        if (open) Refresh();
    }

    protected abstract void Refresh();

    public const float HeaderButtonSize = 32f;

    protected Button AddHeaderButton(string text, string tooltip, Action onPressed)
    {
        var button = Header.AddChild(new Button { Text = text, ThemeTypeVariation = "SmallButton", TooltipText = tooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(HeaderButtonSize, HeaderButtonSize) });
        button.Pressed += _ => onPressed();
        return button;
    }

    protected static Control Spacer(float width, float height) => new Control { CustomMinimumSize = new Vector2(width, height), MouseFilter = MouseFilter.Ignore };
}

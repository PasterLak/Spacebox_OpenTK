using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Windows;

public abstract class GameWindow : PanelContainer
{
    protected GameWindow(string title)
    {
        MouseFilter = MouseFilter.Stop;
        ThemeTypeVariation = "GameWindow";
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkEnd;
        Visible = false;

        var column = AddChild(new VBoxContainer());
        column.AddThemeConstantOverride("separation", 6f);

        var bar = column.AddChild(new PanelContainer { ThemeTypeVariation = "GameTitleBar" });
        var row = bar.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 6f);
        TitleLabel = row.AddChild(new Label { Text = title, ThemeTypeVariation = "MenuTitle", SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
        Header = row.AddChild(new HBoxContainer { CustomMinimumSize = new Vector2(0f, HeaderHeight) });
        Header.AddThemeConstantOverride("separation", 4f);
        _energy = row.AddChild(new Label { ThemeTypeVariation = "MenuTitle", VerticalAlignment = VerticalAlignment.Center, Visible = false, TooltipText = "Energy used by this block" });

        Body = column.AddChild(new VBoxContainer());
        Body.AddThemeConstantOverride("separation", 6f);
    }

    private readonly Label _energy;

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

    protected void ShowEnergy(int energy) => ShowEnergyText(energy > 0 ? energy + " EU" : null);

    protected void ShowEnergyText(string text)
    {
        bool visible = !string.IsNullOrEmpty(text);
        if (_energy.Visible != visible) _energy.Visible = visible;
        if (visible && _energy.Text != text) _energy.Text = text;
    }

    protected void MatchColumnWidth() => SizeFlagsHorizontal = SizeFlags.Fill;

    public const float HeaderHeight = 18f;
    public const float HeaderButtonSize = 18f;

    protected Button AddHeaderButton(string text, string tooltip, Action onPressed)
    {
        var button = Header.AddChild(new Button { Text = text, ThemeTypeVariation = "HeaderButton", TooltipText = tooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(HeaderButtonSize + 6f, HeaderButtonSize) });
        button.Pressed += _ => onPressed();
        return button;
    }

    protected static Control Spacer(float width, float height) => new Control { CustomMinimumSize = new Vector2(width, height), MouseFilter = MouseFilter.Ignore };
}

using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class ControlsOverlayView : GameScreen
{
    private static readonly Color4 TitleColor = new Color4(1f, 0.77f, 0.16f, 1f);
    private static readonly Color4 KeyColor = new Color4(0.45f, 0.85f, 0.95f, 1f);
    private static readonly Color4 HintColor = new Color4(0.6f, 0.62f, 0.68f, 1f);
    private static readonly Color4 TextColor = new Color4(0.92f, 0.92f, 0.92f, 1f);
    private static readonly Color4 Background = new Color4(0.06f, 0.08f, 0.11f, 0.9f);

    private readonly PanelContainer _panel = new PanelContainer { Name = "ControlsPanel", MouseFilter = MouseFilter.Ignore };
    private readonly Label _hint = new Label { MouseFilter = MouseFilter.Ignore };
    private readonly HBoxContainer _columns = new HBoxContainer { MouseFilter = MouseFilter.Ignore };
    private int _version = -1;

    public ControlsOverlayView()
    {
        Name = "Controls";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.AddChild(_panel);
        _panel.AddThemeStyleBoxOverride("panel", new StyleBoxFlat(Background).SetCornerRadius(6f).SetContentMargins(14f));

        var column = _panel.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore });
        column.AddThemeConstantOverride("separation", 6f);
        var header = column.AddChild(new HBoxContainer { MouseFilter = MouseFilter.Ignore });
        header.AddThemeConstantOverride("separation", 16f);
        header.AddChild(ColoredLabel("CONTROLS", TitleColor, true));
        header.AddChild(_hint);
        _hint.AddThemeColorOverride("font_color", HintColor);
        column.AddChild(new HSeparator { MouseFilter = MouseFilter.Ignore });
        column.AddChild(_columns);
        _columns.AddThemeConstantOverride("separation", 28f);
    }

    public override bool IsShown => InputOverlay.IsVisible && Settings.ShowInterface;

    protected override void Refresh(float deltaTime)
    {
        InputOverlay.EnsureBuilt();
        SetText(_hint, $"[{InputOverlay.ToggleKey}] close");
        if (_version == InputOverlay.Version) return;
        _version = InputOverlay.Version;
        Build();
    }

    private void Build()
    {
        _columns.ClearChildren();
        foreach (var groups in InputOverlay.Columns)
        {
            var grid = _columns.AddChild(new GridContainer { Columns = 2, MouseFilter = MouseFilter.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin });
            grid.AddThemeConstantOverride("h_separation", 12f);
            grid.AddThemeConstantOverride("v_separation", 2f);
            for (int g = 0; g < groups.Count; g++)
            {
                if (g > 0)
                {
                    grid.AddChild(Gap());
                    grid.AddChild(Gap());
                }
                grid.AddChild(ColoredLabel(groups[g].Title.ToUpperInvariant(), TitleColor, true));
                grid.AddChild(Gap());
                foreach (var (key, name) in groups[g].Entries)
                {
                    grid.AddChild(ColoredLabel($"[{key}]", KeyColor, false));
                    grid.AddChild(ColoredLabel(name, TextColor, false));
                }
            }
        }
    }

    private static Control Gap() => new Control { CustomMinimumSize = new Vector2(0f, 6f), MouseFilter = MouseFilter.Ignore };

    private static Label ColoredLabel(string text, Color4 color, bool bold)
    {
        var label = new Label { Text = text, Bold = bold, MouseFilter = MouseFilter.Ignore };
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}

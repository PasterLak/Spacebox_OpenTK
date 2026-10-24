using Engine.CanvasUI;
using Engine.Graphics.Debugging;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class RenderModeLabel : PanelContainer
{
    private readonly Label _label = new Label { FontColor = new Color4(1f, 0.88f, 0.4f, 1f) };
    private RenderDebugMode _shownMode = RenderDebugMode.Lit;

    public RenderModeLabel()
    {
        Name = "RenderMode";
        MouseFilter = MouseFilter.Ignore;
        var style = new StyleBoxFlat(new Color4(DarkTheme.Background.R, DarkTheme.Background.G, DarkTheme.Background.B, 0.85f)).SetBorder(1f, DarkTheme.Border);
        style.SetContentMargins(6f, 4f);
        AddThemeStyleBoxOverride("panel", style);
        AddChild(_label);
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, 12f, Vector2.Zero);
        Visible = false;
    }

    public void Show(RenderDebugMode mode)
    {
        Visible = mode != RenderDebugMode.Lit;
        if (mode == _shownMode && _label.Text.Length > 0) return;
        _shownMode = mode;
        _label.Text = $"F10  {RenderDebugView.Describe(mode)}";
    }
}

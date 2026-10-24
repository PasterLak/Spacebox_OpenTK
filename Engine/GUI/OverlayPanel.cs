using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public sealed class OverlayPanel : PanelContainer
{
    private const float Margin = 8f;

    public OverlayPanel(OverlayText text)
    {
        Name = "Overlay";
        MouseFilter = MouseFilter.Ignore;
        var style = new StyleBoxFlat(new Color4(DarkTheme.Background.R, DarkTheme.Background.G, DarkTheme.Background.B, 0.85f)).SetBorder(1f, DarkTheme.Border);
        style.SetContentMargins(8f, 6f);
        AddThemeStyleBoxOverride("panel", style);
        TextControl = new OverlayTextControl { Text = text };
        AddChild(TextControl);
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft, Margin, Vector2.Zero);
    }

    public OverlayTextControl TextControl { get; }
}

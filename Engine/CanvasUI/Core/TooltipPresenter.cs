using OpenTK.Mathematics;

namespace Engine.CanvasUI;

internal sealed class TooltipPresenter
{
    private static readonly Vector2 CursorSize = new Vector2(12f, 20f);

    private readonly Label _label = new Label { ThemeTypeVariation = "TooltipLabel" };

    public PopupPanel Panel { get; } = new PopupPanel { Name = "Tooltip", MouseFilter = MouseFilter.Ignore, ThemeTypeVariation = "TooltipPanel", Visible = false, Gap = 0f };

    public TooltipPresenter()
    {
        Panel.AddChild(_label);
    }

    public void Update(CanvasRoot root, string text, Vector2 pointer)
    {
        if (text == null)
        {
            Panel.Visible = false;
            return;
        }

        var layer = root.Layer(UiLayer.Tooltip);
        if (Panel.Parent != layer) layer.AddChild(Panel);

        _label.Text = text;
        Panel.AnchorRect = new Rect2(MathF.Floor(pointer.X), MathF.Floor(pointer.Y), CursorSize.X, CursorSize.Y);
        Panel.PreferredSide = PopupSide.Below;
        Panel.Visible = true;
    }
}

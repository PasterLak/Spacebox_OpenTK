using OpenTK.Mathematics;

namespace Engine.CanvasUI;

internal sealed class TooltipPresenter
{
    private static readonly Vector2 CursorSize = new Vector2(12f, 20f);
    private const float MaxWidth = 380f;

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

        if (_label.Text != text)
        {
            _label.Text = text;
            var font = _label.GetThemeFont();
            bool wrap = font != null && (font.MeasureWidth(text, _label.GetThemeFontSize()) > MaxWidth);
            _label.Autowrap = wrap;
            _label.CustomMinimumSize = new Vector2(wrap ? MaxWidth : 0f, 0f);
        }
        Panel.AnchorRect = new Rect2(MathF.Floor(pointer.X), MathF.Floor(pointer.Y), CursorSize.X, CursorSize.Y);
        Panel.PreferredSide = PopupSide.Below;
        Panel.Visible = true;
    }
}

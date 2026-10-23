using OpenTK.Mathematics;

namespace Engine.CanvasUI;

internal sealed class TooltipPresenter
{
    private static readonly Vector2 CursorSize = new Vector2(12f, 20f);
    private const float MaxWidth = 380f;

    private readonly VBoxContainer _column = new VBoxContainer { MouseFilter = MouseFilter.Ignore };
    private readonly Label _title = new Label { ThemeTypeVariation = "TooltipLabel", Bold = true, Visible = false };
    private readonly Label _label = new Label { ThemeTypeVariation = "TooltipLabel" };
    private readonly TextureRect _image = new TextureRect { StretchMode = TextureStretchMode.KeepAspectCentered, IgnoreTextureSize = true, MouseFilter = MouseFilter.Ignore, Visible = false };

    public PopupPanel Panel { get; } = new PopupPanel { Name = "Tooltip", MouseFilter = MouseFilter.Ignore, ThemeTypeVariation = "TooltipPanel", Visible = false, Gap = 0f };

    public TooltipPresenter()
    {
        _column.AddThemeConstantOverride("separation", 2f);
        _column.AddChild(_title);
        _column.AddChild(_label);
        _column.AddChild(_image);
        Panel.AddChild(_column);
    }

    public void Update(CanvasRoot root, Control source, Vector2 pointer)
    {
        if (source == null)
        {
            Panel.Visible = false;
            return;
        }

        var layer = root.Layer(UiLayer.Tooltip);
        if (Panel.Parent != layer) layer.AddChild(Panel);

        SetText(_title, source.TooltipTitle);
        SetText(_label, source.TooltipText);
        var texture = source.TooltipImage;
        if (!ReferenceEquals(_image.Texture, texture)) _image.Texture = texture;
        if (_image.FlipV != source.TooltipImageFlipV) _image.FlipV = source.TooltipImageFlipV;
        bool showImage = texture != null && source.TooltipImageSize.X > 0f && source.TooltipImageSize.Y > 0f;
        if (_image.Visible != showImage) _image.Visible = showImage;
        if (showImage && _image.CustomMinimumSize != source.TooltipImageSize) _image.CustomMinimumSize = source.TooltipImageSize;

        Panel.AnchorRect = new Rect2(MathF.Floor(pointer.X), MathF.Floor(pointer.Y), CursorSize.X, CursorSize.Y);
        Panel.PreferredSide = PopupSide.Below;
        Panel.Visible = true;
    }

    private static void SetText(Label label, string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        if (label.Visible != show) label.Visible = show;
        if (!show || label.Text == text) return;

        label.Text = text;
        var font = label.GetThemeFont();
        bool wrap = font != null && font.MeasureWidth(text, label.GetThemeFontSize()) > MaxWidth;
        label.Autowrap = wrap;
        label.CustomMinimumSize = new Vector2(wrap ? MaxWidth : 0f, 0f);
    }
}

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class FoldableContainer : Container
{
    private bool _expanded = true;
    private string _title = "";

    public FoldableContainer(string title = "", bool expanded = true)
    {
        _title = title ?? "";
        _expanded = expanded;
        MouseFilter = MouseFilter.Pass;
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? "";
            QueueLayout();
        }
    }

    public string Summary { get; set; } = "";
    public float Indent { get; set; } = 12f;
    public float HeaderHeight => (GetThemeFont()?.GetLineHeight(GetThemeFontSize()) ?? 12f) + GetThemeConstant("header_padding", 6f);

    public event Action<bool> Toggled;

    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            _expanded = value;
            QueueLayout();
            Toggled?.Invoke(value);
        }
    }

    public override Vector2 GetMinimumSize()
    {
        float header = HeaderHeight;
        if (!_expanded) return new Vector2(0f, header);

        float height = header;
        float width = 0f;
        foreach (var child in Children)
        {
            if (child is not Control control || !control.Visible) continue;
            var min = control.GetCombinedMinimumSize();
            height += min.Y;
            width = MathF.Max(width, min.X + Indent);
        }
        return new Vector2(width, height);
    }

    protected override void LayoutChildren()
    {
        float y = Rect.Y + HeaderHeight;
        foreach (var child in Children)
        {
            if (child is not Control control) continue;
            control.SetVisibleSilently(_expanded);
            if (!_expanded) continue;
            float height = control.GetCombinedMinimumSize().Y;
            FitChildInRect(control, new Rect2(Rect.X + Indent, y, MathF.Max(0f, Rect.Width - Indent), height));
            y += height;
        }
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || localPosition.Y > HeaderHeight) return false;
        Expanded = !Expanded;
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float header = HeaderHeight;
        var headerRect = new Rect2(0f, 0f, Size.X, header);
        bool hover = IsHovered && Root != null && Root.PointerPosition.Y - GlobalPosition.Y <= header;
        var style = GetThemeStyleBox(hover ? "header_hover" : "header");
        style?.Draw(renderer, headerRect);

        var color = GetThemeColor("font_color", Color4.White);
        float mid = MathF.Floor(header * 0.5f);
        if (_expanded)
            for (int i = 0; i < 4; i++) renderer.DrawRect(new Rect2(3f + i, mid - 2f + i, 7f - i * 2f, 1f), color);
        else
            for (int i = 0; i < 4; i++) renderer.DrawRect(new Rect2(4f + i, mid - 3f + i, 1f, 7f - i * 2f), color);

        var font = GetThemeFont();
        if (font == null) return;
        float size = GetThemeFontSize();
        float ty = MathF.Floor((header - font.GetLineHeight(size)) * 0.5f);
        CanvasDraw.Text(renderer, font, new Vector2(14f, ty), _title, size, color);
        if (Summary.Length > 0)
        {
            float x = 14f + font.MeasureWidth(_title, size) + 10f;
            CanvasDraw.Text(renderer, font, new Vector2(x, ty), TextFit.Ellipsize(font, Summary, size, MathF.Max(0f, Size.X - x)), size, GetThemeColor("font_secondary_color", new Color4(1f, 1f, 1f, 0.45f)));
        }
    }
}

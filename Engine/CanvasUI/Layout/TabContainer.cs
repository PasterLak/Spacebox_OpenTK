using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class TabContainer : Container
{
    private readonly Dictionary<Control, string> _titles = new Dictionary<Control, string>();
    private readonly List<float> _tabWidths = new List<float>();
    private int _current;
    private int _hoveredTab = -1;

    public int CurrentTab
    {
        get => _current;
        set
        {
            int count = Pages().Count;
            int clamped = count == 0 ? 0 : Math.Clamp(value, 0, count - 1);
            if (clamped == _current) return;
            _current = clamped;
            QueueLayout();
            TabChanged?.Invoke(_current);
        }
    }

    public Control CurrentPage
    {
        get
        {
            var pages = Pages();
            return pages.Count == 0 ? null : pages[Math.Clamp(_current, 0, pages.Count - 1)];
        }
    }

    public float TabHeight => (GetThemeFont()?.GetLineHeight(GetThemeFontSize()) ?? 12f) + GetThemeConstant("tab_v_padding", 8f);

    public event Action<int> TabChanged;

    public T AddTab<T>(T page, string title) where T : Control
    {
        AddChild(page);
        _titles[page] = title;
        return page;
    }

    public void SetTabTitle(Control page, string title)
    {
        _titles[page] = title;
        QueueLayout();
    }

    public string TabTitle(int index)
    {
        var pages = Pages();
        if (index < 0 || index >= pages.Count) return "";
        return _titles.TryGetValue(pages[index], out var title) ? title : pages[index].Name;
    }

    public override Vector2 GetMinimumSize()
    {
        var min = Vector2.Zero;
        foreach (var page in Pages()) min = Vector2.ComponentMax(min, page.GetCombinedMinimumSize());
        return new Vector2(min.X, min.Y + TabHeight);
    }

    protected override void LayoutChildren()
    {
        var pages = Pages();
        _current = pages.Count == 0 ? 0 : Math.Clamp(_current, 0, pages.Count - 1);
        float tabHeight = TabHeight;
        var content = new Rect2(Rect.X, Rect.Y + tabHeight, Rect.Width, MathF.Max(0f, Rect.Height - tabHeight));
        for (int i = 0; i < pages.Count; i++)
        {
            bool active = i == _current;
            pages[i].SetVisibleSilently(active);
            if (active) FitChildInRect(pages[i], content);
        }
        MeasureTabs(pages);
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || localPosition.Y > TabHeight) return false;
        int index = TabAt(localPosition.X);
        if (index < 0) return false;
        CurrentTab = index;
        return true;
    }

    protected internal override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        _hoveredTab = localPosition.Y <= TabHeight ? TabAt(localPosition.X) : -1;
    }

    protected internal override void OnMouseExit()
    {
        _hoveredTab = -1;
        base.OnMouseExit();
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        float height = TabHeight;
        var font = GetThemeFont();
        float size = GetThemeFontSize();
        renderer.DrawRect(new Rect2(0f, height - 1f, Size.X, 1f), GetThemeColor("tab_line_color", new Color4(1f, 0.77f, 0.16f, 0.6f)));

        float x = 0f;
        for (int i = 0; i < _tabWidths.Count; i++)
        {
            var rect = new Rect2(x, 0f, _tabWidths[i], height - 1f);
            string state = i == _current ? "tab_selected" : i == _hoveredTab ? "tab_hover" : "tab";
            var style = GetThemeStyleBox(state) ?? GetThemeStyleBox("tab");
            if (style != null) style.Draw(renderer, rect);
            else renderer.DrawRect(rect, i == _current ? new Color4(0.85f, 0.55f, 0.1f, 1f) : new Color4(1f, 1f, 1f, i == _hoveredTab ? 0.12f : 0.06f));

            if (font != null)
            {
                string title = TabTitle(i);
                float ty = MathF.Floor((height - font.GetLineHeight(size)) * 0.5f);
                var color = GetThemeColor(i == _current ? "font_selected_color" : "font_color", Color4.White);
                CanvasDraw.Text(renderer, font, new Vector2(x + 8f, ty), title, size, color);
            }
            x += _tabWidths[i] + 2f;
        }
    }

    private void MeasureTabs(IReadOnlyList<Control> pages)
    {
        _tabWidths.Clear();
        var font = GetThemeFont();
        float size = GetThemeFontSize();
        for (int i = 0; i < pages.Count; i++)
            _tabWidths.Add((font?.MeasureWidth(TabTitle(i), size) ?? 40f) + 16f);
    }

    private int TabAt(float x)
    {
        float cursor = 0f;
        for (int i = 0; i < _tabWidths.Count; i++)
        {
            if (x >= cursor && x < cursor + _tabWidths[i]) return i;
            cursor += _tabWidths[i] + 2f;
        }
        return -1;
    }

    private List<Control> Pages()
    {
        var pages = new List<Control>();
        foreach (var child in Children)
            if (child is Control control) pages.Add(control);
        return pages;
    }
}

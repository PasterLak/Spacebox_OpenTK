using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public sealed class ListItem
{
    public string Text { get; set; } = "";
    public string Secondary { get; set; } = "";
    public Texture2D Icon { get; set; }
    public Rect2? IconRegion { get; set; }
    public Color4? TextColor { get; set; }
    public string Tooltip { get; set; } = "";
    public bool IsHeader { get; set; }
    public bool Disabled { get; set; }
    public int Indent { get; set; }
    public object Metadata { get; set; }
}

public class ItemList : Control
{
    private readonly List<ListItem> _items = new List<ListItem>();
    private float _scroll;
    private int _pendingVisible = -1;
    private int _selected = -1;
    private int _hovered = -1;

    public ItemList()
    {
        ClipContents = true;
        FocusMode = FocusMode.All;
    }

    public IReadOnlyList<ListItem> Items => _items;
    public int SelectedIndex => _selected;
    public ListItem SelectedItem => _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;
    public float IconSize { get; set; } = 16f;
    public bool AllowReselect { get; set; }
    public override bool IsInteractive => true;
    public float RowHeight => MathF.Max(IconSize, GetThemeFont()?.GetLineHeight(GetThemeFontSize()) ?? 12f) + GetThemeConstant("v_padding", 4f) + GetThemeConstant("item_separation", 0f);
    public float ContentHeight => _items.Count * RowHeight;
    public float MaxScroll => MathF.Max(0f, ContentHeight - Size.Y);
    public bool IsScrolledToBottom => _scroll >= MaxScroll - 1f;

    public Rect2? RowRect(int index)
    {
        if (index < 0 || index >= _items.Count) return null;
        float top = index * RowHeight - _scroll;
        if (top + RowHeight <= 0f || top >= Size.Y) return null;
        return new Rect2(Rect.X, Rect.Y + top, Size.X, RowHeight);
    }

    public event Action<int> ItemSelected;
    public event Action<int> ItemActivated;
    public event Action<int> ItemRightClicked;

    public ListItem AddItem(string text, Texture2D icon = null, object metadata = null)
    {
        var item = new ListItem { Text = text ?? "", Icon = icon, Metadata = metadata };
        _items.Add(item);
        QueueLayout();
        return item;
    }

    public ListItem AddHeader(string text)
    {
        var item = new ListItem { Text = text ?? "", IsHeader = true, Disabled = true };
        _items.Add(item);
        return item;
    }

    public void Clear()
    {
        _items.Clear();
        _selected = -1;
        _hovered = -1;
        _scroll = 0f;
        QueueLayout();
    }

    public void Select(int index, bool notify = false)
    {
        if (index < -1 || index >= _items.Count) index = -1;
        if (index >= 0 && (_items[index].IsHeader || _items[index].Disabled)) return;
        bool changed = _selected != index;
        _selected = index;
        if (index >= 0) EnsureVisible(index);
        if (notify && (changed || AllowReselect) && index >= 0) ItemSelected?.Invoke(index);
    }

    public int IndexOf(Func<ListItem, bool> match)
    {
        for (int i = 0; i < _items.Count; i++)
            if (match(_items[i])) return i;
        return -1;
    }

    public void EnsureVisible(int index)
    {
        if (Size.Y <= 0f)
        {
            _pendingVisible = index;
            return;
        }

        _pendingVisible = -1;
        float top = index * RowHeight;
        if (top < _scroll) _scroll = top;
        if (top + RowHeight > _scroll + Size.Y) _scroll = top + RowHeight - Size.Y;
        _scroll = Math.Clamp(_scroll, 0f, MaxScroll);
    }

    public override Vector2 GetMinimumSize() => new Vector2(40f, RowHeight * 2f);

    protected internal override bool OnScroll(Vector2 localPosition, float delta)
    {
        if (MaxScroll <= 0f) return false;
        _scroll = Math.Clamp(_scroll - delta * RowHeight * 3f, 0f, MaxScroll);
        UpdateHovered(localPosition);
        return true;
    }

    protected internal override void OnMouseMove(Vector2 localPosition, bool pressed) => UpdateHovered(localPosition);

    protected internal override void OnMouseExit()
    {
        _hovered = -1;
        base.OnMouseExit();
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        int index = IndexAt(localPosition);
        if (index < 0 || _items[index].IsHeader || _items[index].Disabled) return true;

        if (button == MouseButton.Right)
        {
            Select(index, true);
            ItemRightClicked?.Invoke(index);
            return true;
        }

        if (button != MouseButton.Left) return false;
        Select(index, true);
        if ((Root?.ClickCount ?? 1) >= 2) ItemActivated?.Invoke(index);
        return true;
    }

    protected internal override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside) => true;

    protected internal override bool OnKeyPress(KeyPress press)
    {
        if (!HasFocus || _items.Count == 0) return false;

        switch (press.Key)
        {
            case Keys.Up: MoveSelection(-1); return true;
            case Keys.Down: MoveSelection(1); return true;
            case Keys.PageUp: MoveSelection(-Math.Max(1, (int)(Size.Y / RowHeight))); return true;
            case Keys.PageDown: MoveSelection(Math.Max(1, (int)(Size.Y / RowHeight))); return true;
            case Keys.Home: MoveTo(0, 1); return true;
            case Keys.End: MoveTo(_items.Count - 1, -1); return true;
            case Keys.Enter:
            case Keys.KeyPadEnter:
                if (_selected >= 0) ItemActivated?.Invoke(_selected);
                return true;
        }
        return false;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        GetThemeStyleBox("panel")?.Draw(renderer, LocalRect);
        if (_items.Count == 0) return;
        if (_pendingVisible >= 0) EnsureVisible(_pendingVisible);

        _scroll = Math.Clamp(_scroll, 0f, MaxScroll);
        var font = GetThemeFont();
        float size = GetThemeFontSize();
        float row = RowHeight;
        float lineHeight = font?.GetLineHeight(size) ?? 12f;
        float scrollbar = MaxScroll > 0f ? GetThemeConstant("scrollbar_width", 6f) : 0f;
        float width = Size.X - scrollbar;
        int first = Math.Max(0, (int)(_scroll / row));
        int last = Math.Min(_items.Count - 1, (int)((_scroll + Size.Y) / row));
        var textColor = GetThemeColor("font_color", Color4.White);
        var secondaryColor = GetThemeColor("font_secondary_color", new Color4(1f, 1f, 1f, 0.45f));
        var headerColor = GetThemeColor("font_header_color", new Color4(1f, 0.77f, 0.16f, 1f));
        float gap = GetThemeConstant("item_separation", 0f);
        var itemBox = GetThemeStyleBox("item");
        var hoverBox = GetThemeStyleBox("item_hover");
        var selectedBox = GetThemeStyleBox("item_selected");

        renderer.PushClip(LocalRect);
        for (int i = first; i <= last; i++)
        {
            var item = _items[i];
            float y = MathF.Floor(i * row - _scroll);
            var rowRect = new Rect2(0f, y, width, row - gap);
            bool hovered = i == _hovered && !item.IsHeader && !item.Disabled;
            if (i == _selected && selectedBox != null) selectedBox.Draw(renderer, rowRect);
            else if (i == _selected) renderer.DrawRect(rowRect, GetThemeColor(HasFocus ? "selected_color" : "selected_unfocused_color", new Color4(0.2f, 0.4f, 0.75f, HasFocus ? 0.75f : 0.45f)));
            else if (hovered && hoverBox != null) hoverBox.Draw(renderer, rowRect);
            else if (hovered) renderer.DrawRect(rowRect, GetThemeColor("hover_color", new Color4(1f, 1f, 1f, 0.07f)));
            else if (!item.IsHeader) itemBox?.Draw(renderer, rowRect);

            float x = 4f + item.Indent * 12f;
            if (item.Icon != null)
            {
                float iy = y + (row - gap - IconSize) * 0.5f;
                renderer.DrawTexture(item.Icon, new Rect2(x, MathF.Floor(iy), IconSize, IconSize), CanvasDraw.UvOf(item.Icon, item.IconRegion), Color4.White);
                x += IconSize + 4f;
            }

            if (font == null) continue;
            float ty = MathF.Floor(y + (row - gap - lineHeight) * 0.5f);
            var color = item.IsHeader ? headerColor : item.Disabled ? secondaryColor : item.TextColor ?? textColor;
            string text = TextFit.Ellipsize(font, item.Text, size, MathF.Max(0f, width - x - 4f));
            CanvasDraw.Text(renderer, font, new Vector2(x, ty), text, size, color);
            if (item.Secondary.Length > 0)
            {
                float used = font.MeasureWidth(text, size) + 8f;
                string secondary = TextFit.Ellipsize(font, item.Secondary, size, MathF.Max(0f, width - x - used - 4f));
                CanvasDraw.Text(renderer, font, new Vector2(x + used, ty), secondary, size, secondaryColor);
            }
        }
        renderer.PopClip();

        if (scrollbar > 0f)
        {
            var track = new Rect2(Size.X - scrollbar, 0f, scrollbar, Size.Y);
            renderer.DrawRect(track, GetThemeColor("scroll_track", new Color4(1f, 1f, 1f, 0.06f)));
            float thumb = MathF.Max(12f, Size.Y * Size.Y / ContentHeight);
            float thumbY = (Size.Y - thumb) * _scroll / MaxScroll;
            renderer.DrawRect(new Rect2(track.X, MathF.Floor(thumbY), scrollbar, MathF.Floor(thumb)), GetThemeColor("scroll_thumb", new Color4(1f, 1f, 1f, 0.35f)));
        }
    }

    public string TooltipAt(Vector2 localPosition)
    {
        int index = IndexAt(localPosition);
        return index >= 0 ? _items[index].Tooltip : "";
    }

    public int IndexAt(Vector2 localPosition)
    {
        if (localPosition.Y < 0f || localPosition.X < 0f || localPosition.X > Size.X) return -1;
        int index = (int)((localPosition.Y + _scroll) / RowHeight);
        return index >= 0 && index < _items.Count ? index : -1;
    }

    private void UpdateHovered(Vector2 localPosition)
    {
        _hovered = IndexAt(localPosition);
        TooltipText = _hovered >= 0 ? _items[_hovered].Tooltip : "";
    }

    private void MoveSelection(int delta)
    {
        int start = _selected < 0 ? (delta > 0 ? -1 : _items.Count) : _selected;
        MoveTo(start + delta, Math.Sign(delta));
    }

    private void MoveTo(int index, int direction)
    {
        index = Math.Clamp(index, 0, _items.Count - 1);
        while (index >= 0 && index < _items.Count && (_items[index].IsHeader || _items[index].Disabled)) index += direction == 0 ? 1 : direction;
        if (index < 0 || index >= _items.Count) return;
        Select(index, true);
    }
}

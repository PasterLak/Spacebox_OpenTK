using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class ComboBox : Control
{
    private const float ArrowWidth = 18f;

    private readonly LineEdit _edit = new LineEdit { Name = "ComboText" };
    private readonly List<ListItem> _options = new List<ListItem>();
    private readonly PopupPanel _popup = new PopupPanel { Name = "ComboPopup", Visible = false, AutoHide = true };
    private readonly ItemList _list = new ItemList { Name = "ComboList" };
    private bool _filtering;

    public ComboBox()
    {
        MouseFilter = MouseFilter.Pass;
        AddChild(_edit);
        _edit.SetAnchorsPreset(LayoutPreset.FullRect);
        _edit.OffsetRight = -ArrowWidth;
        _edit.TextChanged += OnTyped;
        _edit.TextSubmitted += _ => CommitTyped();
        _edit.EditingFinished += _ => CommitTyped();
        _edit.FocusEntered += _ => { if (!Editable) OpenPopup(); };
        _edit.KeyFilter = HandleKey;

        _popup.Owner = this;
        _popup.CustomMaximumSize = new Vector2(0f, 260f);
        _popup.AddChild(_list);
        _list.ItemActivated += Pick;
        _list.ItemSelected += index => { if (!_filtering) Pick(index); };
    }

    public LineEdit Edit => _edit;
    public IReadOnlyList<ListItem> Options => _options;
    public bool AllowCustomText { get; set; } = true;
    public bool IsOpen => _popup.Visible && _popup.Parent != null;
    public float PopupMinWidth { get; set; } = 160f;
    public bool ShowSelectedIcon { get; set; } = true;

    public event Action<string> ValueChanged;

    public bool Editable
    {
        get => _edit.Editable;
        set => _edit.Editable = value;
    }

    public string Value
    {
        get => _edit.Text;
        set => _edit.Text = value ?? "";
    }

    public string PlaceholderText
    {
        get => _edit.PlaceholderText;
        set => _edit.PlaceholderText = value;
    }

    public void SetOptions(IEnumerable<ListItem> options)
    {
        _options.Clear();
        if (options != null) _options.AddRange(options);
        if (IsOpen) Fill(_filtering ? _edit.Text : "");
    }

    public ListItem AddOption(string text, Texture2D icon = null, Rect2? region = null, object metadata = null)
    {
        var item = new ListItem { Text = text ?? "", Icon = icon, IconRegion = region, Metadata = metadata };
        _options.Add(item);
        return item;
    }

    public override void Process(float deltaTime)
    {
        base.Process(deltaTime);
        ListItem selected = null;
        if (ShowSelectedIcon)
        {
            for (int i = 0; i < _options.Count; i++)
                if (string.Equals(_options[i].Text, _edit.Text, StringComparison.OrdinalIgnoreCase)) { selected = _options[i]; break; }
        }
        _edit.Icon = selected?.Icon;
        _edit.IconRegion = selected?.IconRegion;
        _edit.Badges = selected?.Badges;
    }

    public override Vector2 GetMinimumSize() => _edit.GetCombinedMinimumSize() + new Vector2(ArrowWidth, 0f);

    protected override void LayoutChildren()
    {
        _edit.ApplyRect(new Rect2(Rect.X, Rect.Y, MathF.Max(0f, Rect.Width - ArrowWidth), Rect.Height));
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;
        if (IsOpen) ClosePopup();
        else OpenPopup();
        return true;
    }

    protected internal override bool OnKeyPress(KeyPress press) => (_edit.HasFocus || _list.HasFocus) && HandleKey(press);

    private bool HandleKey(KeyPress press)
    {
        switch (press.Key)
        {
            case Keys.Down:
                if (!IsOpen) OpenPopup();
                else MoveHighlight(1);
                return true;
            case Keys.Up:
                if (IsOpen) MoveHighlight(-1);
                return IsOpen;
            case Keys.Escape:
                if (!IsOpen) return false;
                ClosePopup();
                return true;
            case Keys.Enter:
            case Keys.KeyPadEnter:
                if (!IsOpen || _list.SelectedIndex < 0) return false;
                Pick(_list.SelectedIndex);
                return true;
        }
        return false;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var style = GetThemeStyleBox(IsHovered ? "arrow_hover" : "arrow");
        var rect = new Rect2(Size.X - ArrowWidth, 0f, ArrowWidth, Size.Y);
        if (style != null) style.Draw(renderer, rect);
        else renderer.DrawRect(rect, new Color4(1f, 1f, 1f, IsHovered ? 0.12f : 0.06f));

        var color = GetThemeColor("font_color", Color4.White);
        float cx = MathF.Floor(rect.X + ArrowWidth * 0.5f);
        float cy = MathF.Floor(Size.Y * 0.5f) - 2f;
        for (int i = 0; i < 4; i++) renderer.DrawRect(new Rect2(cx - 3f + i, cy + i, 7f - i * 2f, 1f), color);
    }

    public void OpenPopup()
    {
        if (Root == null) return;

        _filtering = false;
        Fill("");
        if (_popup.Parent == null) Root.Add(_popup, CanvasRoot.PopupLayerFor(this));
        float chrome = (_popup.GetThemeStyleBox("panel")?.MinimumSize.Y ?? 0f) + (_list.GetThemeStyleBox("panel")?.MinimumSize.Y ?? 0f);
        _popup.CustomMinimumSize = new Vector2(MathF.Max(PopupMinWidth, Size.X), MathF.Min(260f, _list.RowHeight * Math.Max(1, _list.Items.Count) + MathF.Max(8f, chrome + 2f)));
        _popup.Popup(Rect, PopupSide.Below);
        int current = _list.IndexOf(item => string.Equals(item.Text, _edit.Text, StringComparison.OrdinalIgnoreCase));
        _filtering = true;
        _list.Select(current);
        _filtering = false;
    }

    public void ClosePopup()
    {
        _popup.Hide();
        _popup.Parent?.RemoveChild(_popup);
    }

    private void OnTyped(string text)
    {
        if (!Editable) return;
        if (!IsOpen) OpenPopup();
        _filtering = true;
        Fill(text);
        _list.Select(-1);
        _filtering = false;
    }

    private void Fill(string filter)
    {
        _list.Clear();
        foreach (var option in _options)
        {
            if (!string.IsNullOrEmpty(filter) && option.Text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 && option.Secondary.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var copy = _list.AddItem(option.Text, option.Icon, option.Metadata);
            copy.IconRegion = option.IconRegion;
            copy.Badges = option.Badges;
            copy.Secondary = option.Secondary;
            copy.Tooltip = option.Tooltip;
            copy.IsHeader = option.IsHeader;
            copy.Disabled = option.Disabled;
        }
    }

    private void MoveHighlight(int delta)
    {
        _filtering = true;
        int index = Math.Clamp(_list.SelectedIndex + delta, 0, Math.Max(0, _list.Items.Count - 1));
        _list.Select(index);
        _filtering = false;
    }

    private void Pick(int index)
    {
        if (index < 0 || index >= _list.Items.Count) return;
        string value = _list.Items[index].Text;
        ClosePopup();
        bool changed = _edit.Text != value;
        _edit.Text = value;
        if (changed) ValueChanged?.Invoke(value);
    }

    private void CommitTyped()
    {
        if (!Editable || IsOpen && _list.HasFocus) return;
        string text = _edit.Text;
        if (!AllowCustomText && _options.FindIndex(o => string.Equals(o.Text, text, StringComparison.OrdinalIgnoreCase)) < 0) return;
        ValueChanged?.Invoke(text);
    }
}

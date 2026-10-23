using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class SelectableRow : PanelContainer
{
    private readonly HBoxContainer _row = new HBoxContainer { MouseFilter = MouseFilter.Ignore };
    private readonly VBoxContainer _lines = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand, MouseFilter = MouseFilter.Ignore };
    private readonly TextureRect _icon = new TextureRect { StretchMode = TextureStretchMode.KeepAspectCentered, MouseFilter = MouseFilter.Ignore };
    private bool _selected;

    public SelectableRow(float iconSize)
    {
        MouseFilter = MouseFilter.Stop;
        _row.AddThemeConstantOverride("separation", 8f);
        _lines.AddThemeConstantOverride("separation", 1f);
        _icon.CustomMinimumSize = new Vector2(iconSize, iconSize);
        _icon.Visible = iconSize > 0f;
        AddChild(_row);
        _row.AddChild(_icon);
        _row.AddChild(_lines);
        UpdateStyle();
    }

    public event Action<SelectableRow> Selected;
    public event Action<SelectableRow> Activated;
    public event Action<SelectableRow, Vector2> RightClicked;
    public object Metadata { get; set; }
    public HBoxContainer Row => _row;
    public override bool IsInteractive => true;

    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            UpdateStyle();
        }
    }

    public Texture2D Icon
    {
        get => _icon.Texture;
        set => _icon.Texture = value;
    }

    public bool FlipIcon
    {
        get => _icon.FlipV;
        set => _icon.FlipV = value;
    }

    public Label AddLine(string text, string variation = "Label", bool wrap = false)
    {
        var label = new Label { Text = text ?? "", ThemeTypeVariation = variation, Autowrap = wrap, Overrun = wrap ? TextOverrun.None : TextOverrun.Ellipsis, MouseFilter = MouseFilter.Ignore };
        _lines.AddChild(label);
        return label;
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button == MouseButton.Right && RightClicked != null)
        {
            Selected?.Invoke(this);
            RightClicked(this, Rect.Position + localPosition);
            return true;
        }
        if (button != MouseButton.Left) return false;
        Selected?.Invoke(this);
        if (Root != null && Root.ClickCount >= 2) Activated?.Invoke(this);
        return true;
    }

    protected override void OnMouseEnter() => UpdateStyle();

    protected override void OnMouseExit() => UpdateStyle();

    private void UpdateStyle()
    {
        var color = _selected ? GetThemeColor("selected_color", new Color4(1f, 0.72f, 0.1f, 1f)) : IsHovered ? GetThemeColor("hover_color", new Color4(0f, 0f, 0f, 0.08f)) : new Color4(0f, 0f, 0f, 0f);
        AddThemeStyleBoxOverride("panel", new StyleBoxFlat(color).SetContentMargins(6f, 4f));
        var text = _selected ? GetThemeColor("font_selected_color", Color4.Black) : (Color4?)null;
        foreach (var child in _lines.Children)
        {
            if (child is not Label label) continue;
            if (text.HasValue) label.AddThemeColorOverride("font_color", text.Value);
            else label.RemoveThemeColorOverride("font_color");
        }
    }
}

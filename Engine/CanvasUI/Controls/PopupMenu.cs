using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class PopupMenu : PopupPanel
{
    private readonly VBoxContainer _box = new VBoxContainer { Name = "MenuItems" };

    public PopupMenu()
    {
        Name = "PopupMenu";
        AutoHide = true;
        Visible = false;
        AddChild(_box);
        _box.AddThemeConstantOverride("separation", 1f);
    }

    public int ItemCount => _box.Children.Count;

    public void ClearItems() => _box.ClearChildren();

    public Button AddItem(string text, Action action, bool disabled = false)
    {
        var button = _box.AddChild(new Button(text) { Alignment = HorizontalAlignment.Left, Disabled = disabled, ThemeTypeVariation = "MenuItem" });
        button.Pressed += _ =>
        {
            Close();
            action?.Invoke();
        };
        return button;
    }

    public void AddSeparator() => _box.AddChild(new HSeparator());

    public void Open(CanvasRoot root, Vector2 position)
    {
        if (root == null || ItemCount == 0) return;
        if (Parent == null) root.Add(this, CanvasRoot.PopupLayerFor(Owner));
        Popup(new Rect2(position, Vector2.Zero), PopupSide.Below);
    }

    public void Close()
    {
        Hide();
        Parent?.RemoveChild(this);
    }
}

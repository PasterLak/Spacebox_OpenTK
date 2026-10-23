using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class MenuModal : Control
{
    private static readonly List<MenuModal> OpenModals = new List<MenuModal>();
    private readonly Control _content;

    public MenuModal(Control content)
    {
        _content = content;
        Name = "MenuModal";
        MouseFilter = MouseFilter.Stop;
        FocusMode = FocusMode.Click;
        SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(content);
    }

    public event Action Closed;

    public bool IsOpen => Parent != null;

    public void Open(CanvasRoot root)
    {
        if (root == null) return;
        if (Parent == null) root.Add(this, UiLayer.Modal);
        OpenModals.Remove(this);
        OpenModals.Add(this);
        MoveToFront();
        GrabFocus();
    }

    public void Close()
    {
        OpenModals.Remove(this);
        if (Parent == null) return;
        Parent.RemoveChild(this);
        Closed?.Invoke();
    }

    protected override void LayoutChildren()
    {
        var size = _content.GetCombinedMinimumSize();
        size.X = MathF.Min(size.X, Rect.Width - 20f);
        size.Y = MathF.Min(size.Y, Rect.Height - 20f);
        var position = Rect.Position + (Rect.Size - size) * 0.5f;
        Container.FitChildInRect(_content, new Rect2(MathF.Round(position.X), MathF.Round(position.Y), size.X, size.Y));
    }

    public static bool CloseTop()
    {
        if (OpenModals.Count == 0) return false;
        OpenModals[^1].Close();
        return true;
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button) => true;

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, GetThemeColor("dim_color", new Color4(0f, 0f, 0f, 0.55f)));
    }
}

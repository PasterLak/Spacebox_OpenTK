using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public class ConfirmDialog : Control
{
    private readonly PanelContainer _panel = new PanelContainer { Name = "DialogPanel" };
    private readonly Label _title = new Label { Name = "DialogTitle" };
    private readonly Label _message = new Label { Name = "DialogMessage", Autowrap = true };
    private readonly HBoxContainer _buttons = new HBoxContainer { Name = "DialogButtons", Alignment = BoxAlignment.End };
    private Action<int> _callback;

    public ConfirmDialog()
    {
        Name = "ConfirmDialog";
        MouseFilter = MouseFilter.Stop;
        FocusMode = FocusMode.Click;
        SetAnchorsPreset(LayoutPreset.FullRect);
        Visible = false;
        AddChild(_panel);
        var box = _panel.AddChild(new VBoxContainer());
        box.AddChild(_title);
        box.AddChild(new HSeparator());
        box.AddChild(_message);
        box.AddChild(_buttons);
        _panel.CustomMinimumSize = new Vector2(380f, 0f);
    }

    public bool IsOpen => Visible && Parent != null;

    public void Show(CanvasRoot root, string title, string message, string[] buttons, Action<int> callback)
    {
        _title.Text = title ?? "";
        _message.Text = message ?? "";
        _callback = callback;
        _buttons.ClearChildren();
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            var button = _buttons.AddChild(new Button(buttons[i]) { CustomMinimumSize = new Vector2(90f, 26f) });
            button.Pressed += _ => Close(index);
        }

        if (Parent == null) root.Add(this, UiLayer.Modal);
        Visible = true;
        MoveToFront();
        GrabFocus();
    }

    public void Close(int result)
    {
        Visible = false;
        Parent?.RemoveChild(this);
        var callback = _callback;
        _callback = null;
        callback?.Invoke(result);
    }

    protected override void LayoutChildren()
    {
        var size = _panel.GetCombinedMinimumSize();
        size.X = MathF.Min(MathF.Max(size.X, 380f), Rect.Width - 40f);
        var position = Rect.Position + (Rect.Size - size) * 0.5f;
        FitChildInRect(_panel, new Rect2(MathF.Round(position.X), MathF.Round(position.Y), size.X, size.Y));
    }

    protected internal override bool OnKeyPress(KeyPress press)
    {
        if (!IsOpen) return false;
        if (press.Key == Keys.Escape) { Close(_buttons.Children.Count - 1); return true; }
        if (press.Key == Keys.Enter || press.Key == Keys.KeyPadEnter) { Close(0); return true; }
        return false;
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button) => true;

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, GetThemeColor("dim_color", new Color4(0f, 0f, 0f, 0.55f)));
    }

    private static void FitChildInRect(Control child, Rect2 rect) => Container.FitChildInRect(child, rect);
}

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public abstract class TextEditBase : Control
{
    private const float BlinkPeriod = 1.06f;

    private float _blink;
    private string _committed = "";

    protected TextEditBase(bool multiline)
    {
        State = new TextEditState(multiline);
        FocusMode = FocusMode.All;
        MouseFilter = MouseFilter.Stop;
    }

    protected TextEditState State { get; }
    protected bool CaretVisible => HasFocus && Editable && _blink < BlinkPeriod * 0.5f;

    public bool Editable { get; set; } = true;
    public string PlaceholderText { get; set; } = "";
    public bool SelectAllOnFocus { get; set; }
    public Func<KeyPress, bool> KeyFilter { get; set; }
    public override bool IsInteractive => true;
    public int CaretPosition => State.Caret;
    public string SelectedText => State.SelectedText;

    public event Action<string> TextChanged;
    public event Action<string> TextSubmitted;
    public event Action<string> EditingFinished;

    public int MaxLength
    {
        get => State.MaxLength;
        set => State.MaxLength = value;
    }

    public string Text
    {
        get => State.Text;
        set
        {
            if (State.Text == (value ?? "")) return;
            State.Text = value;
            _committed = State.Text;
            State.SetCaret(Math.Min(State.Caret, State.Text.Length), false);
            OnTextReplaced();
            QueueLayout();
        }
    }

    public void Select(int start, int end)
    {
        State.SetCaret(start, false);
        State.SetCaret(end, true);
    }

    public void SelectAll() => State.SelectAll();

    public override void Process(float deltaTime)
    {
        _blink = HasFocus ? (_blink + deltaTime) % BlinkPeriod : 0f;
    }

    protected internal override bool OnKeyPress(KeyPress press)
    {
        if (!HasFocus) return false;
        if (KeyFilter != null && KeyFilter(press)) return true;

        var result = TextEditKeys.Handle(State, press, Root?.Clipboard, Editable, PageLines());
        switch (result)
        {
            case TextEditResult.Ignored:
                return false;
            case TextEditResult.Changed:
                NotifyChanged();
                break;
            case TextEditResult.Submitted:
                Commit();
                TextSubmitted?.Invoke(State.Text);
                break;
            case TextEditResult.Cancelled:
                ReleaseFocus();
                break;
        }

        _blink = 0f;
        EnsureCaretVisible();
        return true;
    }

    protected internal override void OnTextInput(string text)
    {
        if (!Editable || string.IsNullOrEmpty(text)) return;
        if (State.Insert(text)) NotifyChanged();
        _blink = 0f;
        EnsureCaretVisible();
    }

    protected internal override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left) return false;

        int index = IndexAtPoint(localPosition);
        int clicks = Root?.ClickCount ?? 1;
        if (clicks >= 3) State.SelectAll();
        else if (clicks == 2) State.SelectWordAt(index);
        else State.SetCaret(index, false);

        _blink = 0f;
        EnsureCaretVisible();
        return true;
    }

    protected internal override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        if (!pressed || !IsMouseDown) return;
        State.SetCaret(IndexAtPoint(localPosition), true);
        EnsureCaretVisible();
    }

    protected internal override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside) => button == MouseButton.Left;

    protected override void OnFocusEntered()
    {
        _committed = State.Text;
        if (SelectAllOnFocus) State.SelectAll();
    }

    protected override void OnFocusExited()
    {
        State.SetCaret(State.Caret, false);
        Commit();
    }

    protected abstract int IndexAtPoint(Vector2 localPosition);

    protected virtual void EnsureCaretVisible()
    {
    }

    protected virtual void OnTextReplaced()
    {
    }

    protected virtual int PageLines() => 10;

    protected StyleBox CurrentStyle()
    {
        string name = !Editable ? "read_only" : HasFocus ? "focus" : "normal";
        return GetThemeStyleBox(name) ?? GetThemeStyleBox("normal");
    }

    protected void DrawSelection(ICanvasRenderer renderer, Rect2 rect)
    {
        renderer.DrawRect(rect, GetThemeColor("selection_color", new Color4(0.25f, 0.45f, 0.8f, 0.55f)));
    }

    protected void DrawCaret(ICanvasRenderer renderer, Vector2 position, float height)
    {
        if (CaretVisible) renderer.DrawRect(new Rect2(MathF.Floor(position.X), position.Y, 1f, height), GetThemeColor("caret_color", Color4.White));
    }

    private void NotifyChanged()
    {
        QueueLayout();
        TextChanged?.Invoke(State.Text);
    }

    private void Commit()
    {
        if (_committed == State.Text) return;
        _committed = State.Text;
        EditingFinished?.Invoke(State.Text);
    }
}

using Engine.CanvasUI;

namespace Spacebox.Editor.Canvas;

public abstract class EditorPage : VBoxContainer
{
    private bool _built;

    protected EditorContext Editor { get; private set; }

    public abstract string Title { get; }
    public virtual string Description => "";

    public void Attach(EditorContext editor)
    {
        Editor = editor;
    }

    public void Show()
    {
        if (!_built)
        {
            _built = true;
            Build();
        }
        OnShown();
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void Hide()
    {
    }

    public virtual void Release()
    {
    }

    protected abstract void Build();

    protected virtual void OnShown()
    {
    }
}

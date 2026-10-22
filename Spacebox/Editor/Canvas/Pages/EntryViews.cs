using Engine.CanvasUI;
using Spacebox.Editor.Data;

namespace Spacebox.Editor.Canvas.Pages;

public interface IEntryView
{
    bool Applies(EditorContext editor, EditorEntry entry);
    Control Control { get; }
    void Show(EditorContext editor, EditorEntry entry);
    void Tick(EditorContext editor, float deltaTime);
    void Release();
}

public interface IEntryEditor : IEntryView
{
}

public abstract class EntryView : IEntryView
{
    private Control _control;

    public Control Control => _control ??= Create();

    public virtual bool Applies(EditorContext editor, EditorEntry entry) => entry?.Node != null;

    public abstract void Show(EditorContext editor, EditorEntry entry);

    public virtual void Tick(EditorContext editor, float deltaTime)
    {
    }

    public virtual void Release()
    {
    }

    protected abstract Control Create();
}

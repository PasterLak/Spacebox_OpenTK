namespace Spacebox.Game.GUI.Menu;

public interface IUnsavedChangesPrompt
{
    bool IsOpen { get; }
    void Open(Action onSave, Action onDiscard, Action onCancel);
}

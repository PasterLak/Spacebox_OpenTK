using Engine.CanvasUI;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class CanvasUnsavedChangesPrompt : IUnsavedChangesPrompt
{
    private readonly ConfirmDialog _dialog = new ConfirmDialog();

    public bool IsOpen => _dialog.IsOpen;

    public void Open(Action onSave, Action onDiscard, Action onCancel)
    {
        var root = MenuCanvas.Current?.Root;
        if (root == null)
        {
            onCancel?.Invoke();
            return;
        }

        _dialog.Show(root, "Unsaved changes", "Save changes?", new[] { "Save", "Don't Save", "Cancel" }, result =>
        {
            if (result == 0) onSave?.Invoke();
            else if (result == 1) onDiscard?.Invoke();
            else onCancel?.Invoke();
        });
    }

    public void Render()
    {
    }
}

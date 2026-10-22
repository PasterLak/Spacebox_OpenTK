using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.CanvasUI;

public enum TextEditResult
{
    Ignored,
    Handled,
    Changed,
    Submitted,
    Cancelled
}

public static class TextEditKeys
{
    public static TextEditResult Handle(TextEditState state, KeyPress press, ICanvasClipboard clipboard, bool editable, int pageLines = 10)
    {
        bool select = press.Shift;
        bool word = press.Control;

        switch (press.Key)
        {
            case Keys.Left:
                state.MoveLeft(word, select);
                return TextEditResult.Handled;
            case Keys.Right:
                state.MoveRight(word, select);
                return TextEditResult.Handled;
            case Keys.Up:
                if (!state.Multiline) return TextEditResult.Ignored;
                state.MoveLines(-1, select);
                return TextEditResult.Handled;
            case Keys.Down:
                if (!state.Multiline) return TextEditResult.Ignored;
                state.MoveLines(1, select);
                return TextEditResult.Handled;
            case Keys.PageUp:
                if (!state.Multiline) return TextEditResult.Ignored;
                state.MoveLines(-pageLines, select);
                return TextEditResult.Handled;
            case Keys.PageDown:
                if (!state.Multiline) return TextEditResult.Ignored;
                state.MoveLines(pageLines, select);
                return TextEditResult.Handled;
            case Keys.Home:
                if (press.Control) state.MoveDocumentStart(select);
                else state.MoveHome(select);
                return TextEditResult.Handled;
            case Keys.End:
                if (press.Control) state.MoveDocumentEnd(select);
                else state.MoveEnd(select);
                return TextEditResult.Handled;
            case Keys.Backspace:
                return editable && state.Backspace(word) ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.Delete:
                return editable && state.Delete(word) ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.Enter:
            case Keys.KeyPadEnter:
                if (!state.Multiline || press.Control) return TextEditResult.Submitted;
                return editable && state.Insert("\n") ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.Escape:
                return TextEditResult.Cancelled;
            case Keys.Tab:
                if (!state.Multiline || press.Control) return TextEditResult.Ignored;
                return editable && state.Insert("    ") ? TextEditResult.Changed : TextEditResult.Handled;
        }

        if (!press.Control) return TextEditResult.Ignored;

        switch (press.Key)
        {
            case Keys.A:
                state.SelectAll();
                return TextEditResult.Handled;
            case Keys.C:
                if (state.HasSelection) clipboard?.SetText(state.SelectedText);
                return TextEditResult.Handled;
            case Keys.X:
                if (!state.HasSelection) return TextEditResult.Handled;
                clipboard?.SetText(state.SelectedText);
                return editable && state.DeleteSelection() ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.V:
                return editable && state.Insert(clipboard?.GetText() ?? "") ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.Z:
                if (!editable) return TextEditResult.Handled;
                return (press.Shift ? state.Redo() : state.Undo()) ? TextEditResult.Changed : TextEditResult.Handled;
            case Keys.Y:
                return editable && state.Redo() ? TextEditResult.Changed : TextEditResult.Handled;
        }

        return TextEditResult.Ignored;
    }
}

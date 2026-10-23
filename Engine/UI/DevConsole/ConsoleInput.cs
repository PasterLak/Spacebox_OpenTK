using Engine.CanvasUI;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.UI;

public sealed class ConsoleInput : LineEdit
{
    public event Action<int> HistoryRequested;
    public event Action CompletionRequested;

    public void SetTextAtEnd(string text)
    {
        Text = text ?? "";
        State.SetCaret(Text.Length, false);
    }

    protected internal override bool OnKeyPress(KeyPress press)
    {
        switch (press.Key)
        {
            case Keys.Up:
                HistoryRequested?.Invoke(-1);
                return true;
            case Keys.Down:
                HistoryRequested?.Invoke(1);
                return true;
            case Keys.Tab:
                CompletionRequested?.Invoke();
                return true;
            default:
                return base.OnKeyPress(press);
        }
    }
}

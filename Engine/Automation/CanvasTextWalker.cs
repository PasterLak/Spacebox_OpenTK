using Engine.CanvasUI;

namespace Engine.Automation;

public static class CanvasTextWalker
{
    public static void Collect(CanvasItem item, List<CanvasTextRun> runs)
    {
        if (!item.Visible) return;

        if (item is Control control)
        {
            switch (control)
            {
                case ICanvasTextSource source:
                    source.CollectText(runs);
                    break;
                case Button button:
                    Add(runs, button.Text, control.Rect);
                    break;
                case CheckBox check:
                    Add(runs, check.Text, control.Rect);
                    break;
                case Label label:
                    Add(runs, label.Text, control.Rect);
                    break;
                case LineEdit edit:
                    Add(runs, edit.Text, control.Rect);
                    break;
                case ItemList list:
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        var row = list.RowRect(i);
                        if (row.HasValue) Add(runs, list.Items[i].Text, row.Value);
                    }
                    break;
            }
        }

        var children = item.Children;
        for (int i = 0; i < children.Count; i++) Collect(children[i], runs);
    }

    private static void Add(List<CanvasTextRun> runs, string text, Rect2 rect)
    {
        if (!string.IsNullOrEmpty(text)) runs.Add(new CanvasTextRun(text, rect));
    }
}

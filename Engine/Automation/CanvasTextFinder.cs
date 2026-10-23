using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Automation;

public readonly record struct CanvasTextMatch(string Layer, string Text, Vector2 ScreenCenter, bool Exact);

public static class CanvasTextFinder
{
    public static List<CanvasTextMatch> Find(string text, bool exactOnly)
    {
        var result = new List<CanvasTextMatch>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (var layer in CanvasLayer.Active)
        {
            if (!layer.InputEnabled) continue;
            Collect(layer.Name, layer.Root, layer.Root.Content, text, exactOnly, result);
        }

        result.Sort((a, b) => b.Exact.CompareTo(a.Exact));
        return result;
    }

    private static void Collect(string layer, CanvasRoot root, CanvasItem item, string text, bool exactOnly, List<CanvasTextMatch> result)
    {
        if (!item.Visible) return;

        if (item is Control control)
        {
            switch (control)
            {
                case Button button:
                    Add(layer, root, button.Text, control.Rect, text, exactOnly, result);
                    break;
                case CheckBox check:
                    Add(layer, root, check.Text, control.Rect, text, exactOnly, result);
                    break;
                case Label label:
                    Add(layer, root, label.Text, control.Rect, text, exactOnly, result);
                    break;
                case ItemList list:
                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        var row = list.RowRect(i);
                        if (row.HasValue) Add(layer, root, list.Items[i].Text, row.Value, text, exactOnly, result);
                    }
                    break;
            }
        }

        var children = item.Children;
        for (int i = 0; i < children.Count; i++) Collect(layer, root, children[i], text, exactOnly, result);
    }

    private static void Add(string layer, CanvasRoot root, string candidate, Rect2 rect, string text, bool exactOnly, List<CanvasTextMatch> result)
    {
        if (string.IsNullOrEmpty(candidate)) return;
        bool exact = string.Equals(candidate.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!exact && (exactOnly || candidate.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)) return;
        result.Add(new CanvasTextMatch(layer, candidate, root.CanvasToScreen(rect.Center), exact));
    }
}

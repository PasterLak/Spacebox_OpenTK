using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Automation;

public static class CanvasTextCapture
{
    public static UiTextSnapshot Capture(Vector2 display)
    {
        var windows = new List<UiWindowText>();
        var runs = new List<CanvasTextRun>();
        var layers = CanvasLayer.Active;
        int order = 0;
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            if (!layer.Visible) continue;
            var root = layer.Root;
            var groups = root.Content.Children;
            for (int g = groups.Count - 1; g >= 0; g--)
            {
                if (groups[g] is not Control group || !group.Visible) continue;
                if (group is not UiLayerRoot layerRoot)
                {
                    AddWindow(windows, runs, layer.Name, "Content", group, root, ref order);
                    continue;
                }

                var children = layerRoot.Children;
                for (int c = children.Count - 1; c >= 0; c--)
                {
                    if (children[c] is Control control && control.Visible) AddWindow(windows, runs, layer.Name, layerRoot.Name, control, root, ref order);
                }
            }
        }

        return new UiTextSnapshot(display.X, display.Y, windows);
    }

    private static void AddWindow(List<UiWindowText> windows, List<CanvasTextRun> runs, string layerName, string group, Control control, CanvasRoot root, ref int order)
    {
        runs.Clear();
        CanvasTextWalker.Collect(control, runs);
        windows.Add(Window(layerName, group, control, root, runs, order++));
    }

    private static UiWindowText Window(string layerName, string group, Control control, CanvasRoot root, List<CanvasTextRun> runs, int order)
    {
        string name = string.IsNullOrEmpty(control.Name) ? control.GetType().Name : control.Name;
        var texts = new List<UiTextRun>(runs.Count);
        foreach (var run in runs)
        {
            var rect = ToScreen(root, run.Rect);
            texts.Add(new UiTextRun(run.Text, rect.X, rect.Y, rect.Width, rect.Height));
        }

        var bounds = ToScreen(root, control.Rect);
        return new UiWindowText(name, $"{layerName}/{group}/{name}", order, bounds.X, bounds.Y, bounds.Width, bounds.Height, texts);
    }

    private static Rect2 ToScreen(CanvasRoot root, Rect2 rect)
    {
        var min = root.CanvasToScreen(rect.Position);
        var max = root.CanvasToScreen(rect.End);
        return new Rect2(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }
}

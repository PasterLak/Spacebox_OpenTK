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

        var runs = new List<CanvasTextRun>();
        foreach (var layer in CanvasLayer.Active)
        {
            if (!layer.InputEnabled || !layer.Visible) continue;
            runs.Clear();
            CanvasTextWalker.Collect(layer.Root.Content, runs);
            foreach (var run in runs) Add(layer.Name, layer.Root, run, text, exactOnly, result);
        }

        result.Sort((a, b) => b.Exact.CompareTo(a.Exact));
        return result;
    }

    private static void Add(string layer, CanvasRoot root, CanvasTextRun run, string text, bool exactOnly, List<CanvasTextMatch> result)
    {
        string candidate = run.Text;
        bool exact = string.Equals(candidate.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!exact && (exactOnly || candidate.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)) return;
        result.Add(new CanvasTextMatch(layer, candidate, root.CanvasToScreen(run.Rect.Center), exact));
    }
}

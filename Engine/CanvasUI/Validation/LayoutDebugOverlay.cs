using OpenTK.Mathematics;

namespace Engine.CanvasUI.Validation;

public static class LayoutDebugOverlay
{
    private static readonly Color4 SafeColor = new Color4(0.2f, 0.9f, 1f, 0.8f);
    private static readonly Color4 ErrorColor = new Color4(1f, 0.15f, 0.15f, 0.95f);
    private static readonly Color4 WarningColor = new Color4(1f, 0.8f, 0.1f, 0.9f);
    private static readonly Color4 ErrorFill = new Color4(1f, 0f, 0f, 0.12f);

    public static void Draw(ICanvasRenderer renderer, CanvasRoot root, LayoutReport report)
    {
        renderer.Transform = Transform2D.Identity;
        renderer.Modulate = Color4.White;
        CanvasDraw.Outline(renderer, root.SafeRect, SafeColor, 1f, 1f, 1f, 1f);
        if (report == null) return;

        var issues = report.Issues;
        for (int i = 0; i < issues.Count; i++)
        {
            var issue = issues[i];
            if (issue.Control == null) continue;
            var rect = issue.Control.Rect;
            bool error = issue.Severity == LayoutSeverity.Error;
            if (error) renderer.DrawRect(rect, ErrorFill);
            CanvasDraw.Outline(renderer, rect, error ? ErrorColor : WarningColor, 2f, 2f, 2f, 2f);
        }
    }
}

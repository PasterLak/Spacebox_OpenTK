namespace Engine.CanvasUI.Validation;

public sealed class BoundsCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        var rules = context.Rules;
        var viewport = context.Viewport;
        var safe = context.SafeRect;
        var reported = new ReportedSet();

        for (int i = 0; i < context.Entries.Count; i++)
        {
            var entry = context.Entries[i];
            var control = entry.Control;
            if (entry.IsClipped || !control.Rect.HasArea || context.IsTransparent(control) || rules.IsIgnored(control)) continue;
            if (reported.CoversAncestorOf(control)) continue;

            if (!viewport.Encloses(control.Rect, rules.Tolerance))
            {
                reported.Add(control);
                issues.Add(new LayoutIssue(LayoutIssueKind.OutsideViewport, LayoutSeverity.Error, control, $"leaves the viewport {viewport}"));
                continue;
            }

            if (!rules.RequireSafeArea || entry.Layer == UiLayer.Background || IsBackdrop(control.Rect, viewport, rules.Tolerance)) continue;
            if (safe.Encloses(control.Rect, rules.Tolerance)) continue;

            reported.Add(control);
            issues.Add(new LayoutIssue(LayoutIssueKind.OutsideSafeArea, LayoutSeverity.Error, control, $"leaves the safe area {safe}"));
        }
    }

    private static bool IsBackdrop(Rect2 rect, Rect2 viewport, float tolerance) => rect.Encloses(viewport, tolerance);
}

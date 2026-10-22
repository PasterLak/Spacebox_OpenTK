namespace Engine.CanvasUI.Validation;

public sealed class OverflowCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        var rules = context.Rules;
        float tolerance = rules.Tolerance;
        var reported = new ReportedSet();

        for (int i = 0; i < context.Entries.Count; i++)
        {
            var control = context.Entries[i].Control;
            if (context.IsTransparent(control) || rules.IsIgnored(control) || reported.CoversAncestorOf(control)) continue;

            var min = control.GetCombinedMinimumSize();
            if (control.Size.X < min.X - tolerance || control.Size.Y < min.Y - tolerance)
            {
                reported.Add(control);
                issues.Add(new LayoutIssue(LayoutIssueKind.Squeezed, LayoutSeverity.Error, control, $"smaller than its minimum size {min.X:0}x{min.Y:0}"));
                continue;
            }

            if (control.Parent is not Container parent || parent is ScrollContainer || !control.Rect.HasArea) continue;
            if (parent.Rect.Encloses(control.Rect, tolerance)) continue;

            reported.Add(control);
            issues.Add(new LayoutIssue(LayoutIssueKind.ContentOverflow, LayoutSeverity.Error, control, $"overflows its container {parent.Rect}", parent));
        }
    }
}

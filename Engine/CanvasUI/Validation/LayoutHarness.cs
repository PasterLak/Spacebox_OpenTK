using OpenTK.Mathematics;

namespace Engine.CanvasUI.Validation;

public static class LayoutHarness
{
    public static void Apply(CanvasRoot root, ScreenProfile profile)
    {
        root.Stretch.UiScale = profile.UiScale;
        root.Stretch.SafeMargins = profile.SafeMargins;
        root.Resize(profile.Size);
        root.EnsureLayout();
    }

    public static LayoutReport Validate(CanvasRoot root, ScreenProfile profile, LayoutRules rules = null)
    {
        Apply(root, profile);
        return LayoutValidator.Validate(root, rules, profile.ToString());
    }

    public static LayoutReport ValidateAll(CanvasRoot root, IEnumerable<ScreenProfile> profiles, LayoutRules rules = null)
    {
        var reports = new List<LayoutReport>();
        foreach (var profile in profiles) reports.Add(Validate(root, profile, rules));
        return LayoutReport.Merge(reports);
    }

    public static void Hover(CanvasRoot root, Vector2 canvasPoint, float seconds = 1f)
    {
        var pointer = new PointerState { Position = root.CanvasToScreen(canvasPoint) };
        root.Update(0f, pointer);
        root.Update(seconds, pointer);
        root.EnsureLayout();
    }

    public static void Click(CanvasRoot root, Vector2 canvasPoint, bool right = false)
    {
        var position = root.CanvasToScreen(canvasPoint);
        root.Update(0f, new PointerState { Position = position });
        root.Update(0f, new PointerState { Position = position, LeftDown = !right, RightDown = right });
        root.Update(0f, new PointerState { Position = position });
        root.EnsureLayout();
    }

    public static void MoveAway(CanvasRoot root)
    {
        root.Update(0f, new PointerState { Position = new Vector2(-10000f, -10000f) });
        root.EnsureLayout();
    }

    public static LayoutReport ValidateTooltips(CanvasRoot root, ScreenProfile profile, LayoutRules rules = null)
    {
        rules ??= new LayoutRules();
        Apply(root, profile);
        var targets = new List<Control>();
        CollectTooltipTargets(root.Content, targets);

        var issues = new List<LayoutIssue>();
        var tooltipRules = new LayoutRules { RequireSafeArea = rules.RequireSafeArea, Tolerance = rules.Tolerance, MinFontPixels = rules.MinFontPixels };
        tooltipRules.Checks.Clear();
        tooltipRules.Checks.Add(new BoundsCheck());
        tooltipRules.Checks.Add(new TextCheck());
        tooltipRules.Checks.Add(new PopupAnchorCheck());

        foreach (var target in targets)
        {
            if (!target.IsVisibleInTree || root.IsBlockedByModal(target)) continue;
            var point = target.Rect.Center;
            if (root.HitTest(point) != target) continue;

            Hover(root, point);
            var tooltip = root.TooltipPanel;
            if (!tooltip.IsVisibleInTree)
            {
                issues.Add(new LayoutIssue(LayoutIssueKind.UnreachableHitTarget, LayoutSeverity.Error, target, "tooltip did not appear"));
                continue;
            }

            var context = new LayoutContext(root, tooltipRules);
            var found = new List<LayoutIssue>();
            for (int i = 0; i < tooltipRules.Checks.Count; i++) tooltipRules.Checks[i].Check(context, found);
            foreach (var issue in found)
                if (IsInside(issue.Control, tooltip)) issues.Add(new LayoutIssue(issue.Kind, issue.Severity, issue.Control, $"tooltip of {ControlPath.Of(target)}: {issue.Message}", target));
        }

        MoveAway(root);
        for (int i = 0; i < issues.Count; i++) issues[i].Profile = profile.ToString();
        return new LayoutReport(issues);
    }

    private static bool IsInside(Control control, Control ancestor)
    {
        for (CanvasItem cursor = control; cursor != null; cursor = cursor.Parent)
            if (cursor == ancestor) return true;
        return false;
    }

    private static void CollectTooltipTargets(CanvasItem item, List<Control> targets)
    {
        if (!item.Visible) return;
        if (item is Control control && !string.IsNullOrEmpty(control.TooltipText) && control.MouseFilter != MouseFilter.Ignore) targets.Add(control);
        var children = item.Children;
        for (int i = 0; i < children.Count; i++) CollectTooltipTargets(children[i], targets);
    }
}

namespace Engine.CanvasUI.Validation;

public sealed class HitTargetCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        var rules = context.Rules;
        var root = context.Root;

        for (int i = 0; i < context.Entries.Count; i++)
        {
            var entry = context.Entries[i];
            var control = entry.Control;
            if (!control.IsInteractive || control.MouseFilter == MouseFilter.Ignore || rules.IsIgnored(control)) continue;

            var visible = entry.VisibleRect;
            if (!visible.HasArea) continue;

            float minimum = rules.MinHitTargetPixels;
            var screen = control.Size * root.Scale;
            if (screen.X + 0.01f < minimum || screen.Y + 0.01f < minimum)
                issues.Add(new LayoutIssue(LayoutIssueKind.SmallHitTarget, LayoutSeverity.Error, control, $"hit target is {screen.X:0}x{screen.Y:0} px on screen, minimum {minimum:0} px"));

            if (root.IsBlockedByModal(control)) continue;
            var hit = root.HitTest(visible.Center);
            if (!IsSelfOrDescendant(hit, control) && !IsCoveredByTransient(rules, hit, entry.Layer))
                issues.Add(new LayoutIssue(LayoutIssueKind.UnreachableHitTarget, LayoutSeverity.Error, control, "its centre is covered by another control", hit));
        }
    }

    private static bool IsCoveredByTransient(LayoutRules rules, CanvasItem hit, UiLayer? layer)
    {
        if (hit == null) return false;
        var hitLayer = UiLayerRoot.LayerOf(hit);
        return hitLayer != layer && rules.IsTransient(hitLayer);
    }

    private static bool IsSelfOrDescendant(CanvasItem hit, Control control)
    {
        for (var cursor = hit; cursor != null; cursor = cursor.Parent)
            if (cursor == control) return true;
        return false;
    }
}

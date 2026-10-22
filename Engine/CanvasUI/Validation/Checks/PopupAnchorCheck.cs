namespace Engine.CanvasUI.Validation;

public sealed class PopupAnchorCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        for (int i = 0; i < context.Entries.Count; i++)
        {
            if (context.Entries[i].Control is not PopupPanel popup || context.Rules.IsIgnored(popup)) continue;
            if (popup.Rect.Intersects(popup.AnchorRect, context.Rules.Tolerance))
                issues.Add(new LayoutIssue(LayoutIssueKind.PopupCoversAnchor, LayoutSeverity.Error, popup, $"covers its anchor {popup.AnchorRect}"));
        }
    }
}

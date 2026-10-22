namespace Engine.CanvasUI.Validation;

public sealed class RegionOverlapCheck : ILayoutCheck
{
    public void Check(LayoutContext context, List<LayoutIssue> issues)
    {
        var rules = context.Rules;
        foreach (var pair in context.Regions)
        {
            var regions = pair.Value;
            for (int a = 0; a < regions.Count; a++)
                for (int b = a + 1; b < regions.Count; b++)
                    if (Overlaps(rules, regions[a], regions[b]))
                        issues.Add(new LayoutIssue(LayoutIssueKind.Overlap, LayoutSeverity.Error, regions[b], $"overlaps a sibling region in layer {pair.Key}", regions[a]));
        }

        foreach (var (upper, lower) in rules.ForbiddenCovers)
        {
            if (!context.Regions.TryGetValue(upper, out var uppers) || !context.Regions.TryGetValue(lower, out var lowers)) continue;
            for (int a = 0; a < uppers.Count; a++)
                for (int b = 0; b < lowers.Count; b++)
                    if (Overlaps(rules, uppers[a], lowers[b]))
                        issues.Add(new LayoutIssue(LayoutIssueKind.CoveredByLayer, LayoutSeverity.Error, uppers[a], $"{upper} covers {lower}", lowers[b]));
        }
    }

    private static bool Overlaps(LayoutRules rules, Control a, Control b)
    {
        return a.Rect.Intersects(b.Rect, rules.Tolerance) && !rules.IsOverlapAllowed(a, b);
    }
}

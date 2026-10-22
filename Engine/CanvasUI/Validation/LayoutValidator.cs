namespace Engine.CanvasUI.Validation;

public interface ILayoutCheck
{
    void Check(LayoutContext context, List<LayoutIssue> issues);
}

public static class LayoutValidator
{
    public static LayoutReport Validate(CanvasRoot root, LayoutRules rules = null, string profile = null)
    {
        rules ??= new LayoutRules();
        var context = new LayoutContext(root, rules);
        var issues = new List<LayoutIssue>();
        for (int i = 0; i < rules.Checks.Count; i++) rules.Checks[i].Check(context, issues);
        for (int i = 0; i < issues.Count; i++) issues[i].Profile = profile;
        return new LayoutReport(issues);
    }
}

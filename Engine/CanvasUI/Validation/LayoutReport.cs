using System.Text;

namespace Engine.CanvasUI.Validation;

public sealed class LayoutReport
{
    private readonly List<LayoutIssue> _issues;

    public IReadOnlyList<LayoutIssue> Issues => _issues;
    public int ErrorCount { get; }
    public int WarningCount => _issues.Count - ErrorCount;
    public bool IsClean => ErrorCount == 0;
    public int CheckedProfiles { get; }

    public LayoutReport(IEnumerable<LayoutIssue> issues, int checkedProfiles = 1)
    {
        _issues = new List<LayoutIssue>(issues);
        CheckedProfiles = checkedProfiles;
        for (int i = 0; i < _issues.Count; i++)
            if (_issues[i].Severity == LayoutSeverity.Error) ErrorCount++;
    }

    public static LayoutReport Merge(IEnumerable<LayoutReport> reports)
    {
        var issues = new List<LayoutIssue>();
        int profiles = 0;
        foreach (var report in reports)
        {
            issues.AddRange(report.Issues);
            profiles += report.CheckedProfiles;
        }
        return new LayoutReport(issues, profiles);
    }

    public IEnumerable<LayoutIssue> OfKind(LayoutIssueKind kind)
    {
        for (int i = 0; i < _issues.Count; i++)
            if (_issues[i].Kind == kind) yield return _issues[i];
    }

    public void ThrowIfErrors()
    {
        if (!IsClean) throw new LayoutValidationException(this);
    }

    public string Format(int maxIssues = 40)
    {
        if (_issues.Count == 0) return $"Layout clean ({CheckedProfiles} profile(s))";
        var builder = new StringBuilder();
        builder.Append(ErrorCount).Append(" error(s), ").Append(WarningCount).Append(" warning(s) in ").Append(CheckedProfiles).Append(" profile(s)");
        for (int i = 0; i < _issues.Count && i < maxIssues; i++) builder.Append('\n').Append(_issues[i]);
        if (_issues.Count > maxIssues) builder.Append("\n… ").Append(_issues.Count - maxIssues).Append(" more");
        return builder.ToString();
    }

    public override string ToString() => Format();
}

public sealed class LayoutValidationException : Exception
{
    public LayoutReport Report { get; }

    public LayoutValidationException(LayoutReport report) : base(report.Format())
    {
        Report = report;
    }
}

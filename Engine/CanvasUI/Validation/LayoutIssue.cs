namespace Engine.CanvasUI.Validation;

public enum LayoutIssueKind
{
    OutsideViewport,
    OutsideSafeArea,
    ContentOverflow,
    Squeezed,
    Overlap,
    CoveredByLayer,
    TextOverflow,
    SmallText,
    SmallHitTarget,
    UnreachableHitTarget,
    PopupCoversAnchor
}

public enum LayoutSeverity
{
    Warning,
    Error
}

public sealed class LayoutIssue
{
    public LayoutIssueKind Kind { get; }
    public LayoutSeverity Severity { get; }
    public Control Control { get; }
    public Control Other { get; }
    public Rect2 Rect { get; }
    public string Path { get; }
    public string Message { get; }
    public string Profile { get; internal set; }

    public LayoutIssue(LayoutIssueKind kind, LayoutSeverity severity, Control control, string message, Control other = null)
    {
        Kind = kind;
        Severity = severity;
        Control = control;
        Other = other;
        Rect = control?.Rect ?? default;
        Path = control == null ? "" : ControlPath.Of(control);
        Message = message;
    }

    public override string ToString()
    {
        string profile = string.IsNullOrEmpty(Profile) ? "" : $"[{Profile}] ";
        string other = Other == null ? "" : $" <-> {ControlPath.Of(Other)} {Other.Rect}";
        return $"{profile}{Severity} {Kind}: {Path} {Rect}{other} - {Message}";
    }
}

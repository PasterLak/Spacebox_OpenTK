namespace Engine.CanvasUI.Validation;

internal sealed class ReportedSet
{
    private readonly HashSet<Control> _reported = new HashSet<Control>();

    public void Add(Control control) => _reported.Add(control);

    public bool CoversAncestorOf(Control control)
    {
        for (var cursor = control.Parent; cursor != null; cursor = cursor.Parent)
            if (cursor is Control parent && _reported.Contains(parent)) return true;
        return false;
    }
}

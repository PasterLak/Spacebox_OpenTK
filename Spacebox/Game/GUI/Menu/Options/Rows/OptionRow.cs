namespace Spacebox.Game.GUI.Menu;

public abstract class OptionRow
{
    protected OptionRow(string label, string tooltip)
    {
        Label = label;
        Tooltip = tooltip;
    }

    public string Label { get; }
    public string Tooltip { get; }
    public Func<bool> VisibleWhen { get; init; }
    public Func<bool> EnabledWhen { get; init; }

    public bool IsVisible => VisibleWhen?.Invoke() ?? true;
    public bool IsEnabled => EnabledWhen?.Invoke() ?? true;

    public abstract void Accept(IOptionRowVisitor visitor);
}

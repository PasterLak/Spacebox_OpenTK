namespace Spacebox.Game.GUI.Menu;

public sealed class ButtonRow : OptionRow
{
    public ButtonRow(string label, string tooltip, string text, Action click) : base(label, tooltip)
    {
        Text = text;
        Click = click;
    }

    public string Text { get; }
    public Action Click { get; }

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

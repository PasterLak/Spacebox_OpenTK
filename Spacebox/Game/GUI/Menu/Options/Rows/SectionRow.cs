namespace Spacebox.Game.GUI.Menu;

public sealed class SectionRow : OptionRow
{
    public SectionRow(string title) : base(title, null)
    {
    }

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

namespace Spacebox.Game.GUI.Menu;

public sealed class TextRow : OptionRow
{
    public TextRow(string label, string tooltip, int maxLength, Func<string> get, Action<string> set) : base(label, tooltip)
    {
        MaxLength = maxLength;
        Get = get;
        Set = set;
    }

    public int MaxLength { get; }
    public Func<string> Get { get; }
    public Action<string> Set { get; }

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

namespace Spacebox.Game.GUI.Menu;

public sealed class CheckRow : OptionRow
{
    public CheckRow(string label, string tooltip, Func<bool> get, Action<bool> set) : base(label, tooltip)
    {
        Get = get;
        Set = set;
    }

    public Func<bool> Get { get; }
    public Action<bool> Set { get; }

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

namespace Spacebox.Game.GUI.Menu;

public sealed class ChoiceRow : OptionRow
{
    public ChoiceRow(string label, string tooltip, string[] items, Func<int> get, Action<int> set) : base(label, tooltip)
    {
        Items = items;
        Get = get;
        Set = set;
    }

    public string[] Items { get; }
    public Func<int> Get { get; }
    public Action<int> Set { get; }

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

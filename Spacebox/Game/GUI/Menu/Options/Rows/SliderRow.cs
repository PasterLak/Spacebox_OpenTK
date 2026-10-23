namespace Spacebox.Game.GUI.Menu;

public sealed class SliderRow : OptionRow
{
    public SliderRow(string label, string tooltip, int min, int max, Func<int> get, Action<int> set, Func<int, string> format) : base(label, tooltip)
    {
        Min = min;
        Max = max;
        Get = get;
        Set = set;
        Format = format;
    }

    public int Min { get; }
    public int Max { get; }
    public Func<int> Get { get; }
    public Action<int> Set { get; }
    public Func<int, string> Format { get; }
    public bool ApplyOnRelease { get; init; }

    public static Func<int, string> Suffix(string suffix) => value => value + suffix;

    public override void Accept(IOptionRowVisitor visitor) => visitor.Visit(this);
}

namespace Spacebox.Game.Rules;

public abstract class ServerRuleDefinition
{
    protected ServerRuleDefinition(string name, string label)
    {
        Name = name;
        Label = label;
    }

    public string Name { get; }
    public string Label { get; }
    public string Description { get; init; } = "";

    public abstract string Kind { get; }
    public abstract object Value { get; }
    public abstract string Text { get; }
    public abstract string Usage { get; }

    public abstract bool TrySet(string text);

    public bool Matches(string name) => Normalize(Name) == Normalize(name);

    private static string Normalize(string name) => (name ?? "").Trim().Replace("_", "").ToLowerInvariant();
}

public sealed class SwitchRuleDefinition : ServerRuleDefinition
{
    private readonly Func<bool> _get;
    private readonly Action<bool> _set;

    public SwitchRuleDefinition(string name, string label, Func<bool> get, Action<bool> set) : base(name, label)
    {
        _get = get;
        _set = set;
    }

    public override string Kind => "switch";
    public override object Value => _get();
    public override string Text => _get() ? "on" : "off";
    public override string Usage => "on|off";

    public override bool TrySet(string text)
    {
        if (!TryParse(text, out bool value)) return false;
        _set(value);
        return true;
    }

    public static bool TryParse(string text, out bool value)
    {
        value = false;
        switch (text?.Trim().ToLowerInvariant())
        {
            case "on":
            case "true":
            case "1":
                value = true;
                return true;
            case "off":
            case "false":
            case "0":
                return true;
            default:
                return false;
        }
    }
}

public sealed class NumberRuleDefinition : ServerRuleDefinition
{
    private readonly Func<int> _get;
    private readonly Action<int> _set;
    private readonly int _min;
    private readonly int _max;
    private readonly string _unit;

    public NumberRuleDefinition(string name, string label, Func<int> get, Action<int> set, int min, int max, string unit) : base(name, label)
    {
        _get = get;
        _set = set;
        _min = min;
        _max = max;
        _unit = unit;
    }

    public int Min => _min;
    public int Max => _max;
    public string Unit => _unit;

    public override string Kind => "number";
    public override object Value => _get();
    public override string Text => $"{_get()} {_unit}";
    public override string Usage => $"{_min}-{_max} ({_unit})";

    public override bool TrySet(string text)
    {
        if (!int.TryParse(text?.Trim(), out int value) || value < _min || value > _max) return false;
        _set(value);
        return true;
    }
}

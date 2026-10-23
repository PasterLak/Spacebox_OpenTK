namespace Spacebox.Game.GUI;

public sealed class RepairTargetUI : IGameUiController
{
    private bool _visible;
    private string _name = "";
    private int _current;
    private int _max;
    private bool _repairing;

    public void Show(string blockName, int current, int max, bool repairing)
    {
        _visible = true;
        _name = blockName ?? "";
        _current = current;
        _max = max;
        _repairing = repairing;
    }

    public bool IsVisible => _visible;
    public string Name => _name;
    public bool IsRepairing => _repairing && _current < _max;
    public float Fill => _max <= 0 ? 1f : Math.Clamp(_current / (float)_max, 0f, 1f);
    public string Status => IsRepairing ? "Repairing... " + Describe(_current, _max) : Describe(_current, _max);

    public void Hide() => _visible = false;

    public void Reset()
    {
        _visible = false;
        _repairing = false;
    }

    public static string Describe(int current, int max)
    {
        if (max <= 0) return "Cannot be repaired";
        if (current >= max) return "No repair needed";
        return $"Durability {Percent(current, max)}%  ({current}/{max})";
    }

    public static int Percent(int current, int max) => max <= 0 ? 100 : (int)Math.Floor(Math.Clamp(current, 0, max) * 100f / max);
}

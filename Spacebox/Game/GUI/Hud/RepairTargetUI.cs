namespace Spacebox.Game.GUI;

public static class RepairTargetUI
{
    private static bool _visible;
    private static string _name = "";
    private static int _current;
    private static int _max;
    private static bool _repairing;

    public static void Show(string blockName, int current, int max, bool repairing)
    {
        _visible = true;
        _name = blockName ?? "";
        _current = current;
        _max = max;
        _repairing = repairing;
    }

    public static bool IsVisible => _visible;
    public static string Name => _name;
    public static bool IsRepairing => _repairing && _current < _max;
    public static float Fill => _max <= 0 ? 1f : Math.Clamp(_current / (float)_max, 0f, 1f);
    public static string Status => IsRepairing ? "Repairing... " + Describe(_current, _max) : Describe(_current, _max);

    public static void Hide() => _visible = false;

    public static void Reset()
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

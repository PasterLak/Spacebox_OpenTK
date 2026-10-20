using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI;

public static class RepairTargetUI
{
    private static readonly Vector4 FullColor = new Vector4(0.43f, 0.86f, 0.43f, 1f);
    private static readonly Vector4 TextColor = new Vector4(0.94f, 0.94f, 0.94f, 1f);
    private const uint BarBackground = 0xC83E322D;
    private const uint BarBorder = 0xFF000000;

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

    public static void OnGUI()
    {
        if (!_visible) return;

        var io = ImGui.GetIO();
        var draw = ImGui.GetForegroundDrawList();
        float unit = io.DisplaySize.Y / 100f;
        var center = new Vector2(io.DisplaySize.X / 2f, io.DisplaySize.Y / 2f + unit * 5f);

        string status = Describe(_current, _max);
        var nameSize = ImGui.CalcTextSize(_name);
        var statusSize = ImGui.CalcTextSize(status);
        draw.AddText(center - new Vector2(nameSize.X / 2f, 0f), ImGui.ColorConvertFloat4ToU32(TextColor), _name);

        float barWidth = unit * 18f;
        float barHeight = unit * 1.2f;
        var barMin = center + new Vector2(-barWidth / 2f, nameSize.Y + unit * 0.6f);
        var barMax = barMin + new Vector2(barWidth, barHeight);
        float fill = _max <= 0 ? 1f : Math.Clamp(_current / (float)_max, 0f, 1f);
        var color = BarColor(fill);

        draw.AddRectFilled(barMin, barMax, BarBackground);
        draw.AddRectFilled(barMin, new Vector2(barMin.X + barWidth * fill, barMax.Y), ImGui.ColorConvertFloat4ToU32(color));
        draw.AddRect(barMin, barMax, BarBorder);

        var statusPos = new Vector2(center.X - statusSize.X / 2f, barMax.Y + unit * 0.5f);
        draw.AddText(statusPos, ImGui.ColorConvertFloat4ToU32(_repairing && _current < _max ? color : TextColor), _repairing && _current < _max ? "Repairing... " + status : status);
    }

    private static Vector4 BarColor(float fill)
    {
        if (fill >= 1f) return FullColor;
        if (fill > 0.5f) return new Vector4(1f, 0.77f, 0.16f, 1f);
        return new Vector4(1f, 0.42f, 0.35f, 1f);
    }
}

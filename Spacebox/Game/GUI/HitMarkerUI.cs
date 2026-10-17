using System.Numerics;
using Engine;
using ImGuiNET;

namespace Spacebox.Game.GUI;

public static class HitMarkerUI
{
    private const float Duration = 0.2f;
    private const float SizeRatio = 0.012f;
    private const float GapRatio = 0.005f;

    private static readonly uint HitColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
    private static readonly uint HeadshotColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.2f, 0.2f, 1f));

    private static float _timeLeft;
    private static bool _isHeadshot;

    public static void Show(bool headshot)
    {
        _timeLeft = Duration;
        _isHeadshot = headshot;
    }

    public static void OnGUI()
    {
        if (_timeLeft <= 0f) return;

        _timeLeft -= Time.Delta;

        var size = ImGui.GetIO().DisplaySize;
        var center = size * 0.5f;
        float length = size.Y * SizeRatio;
        float gap = size.Y * GapRatio;
        float thickness = _isHeadshot ? 3f : 2f;
        uint color = _isHeadshot ? HeadshotColor : HitColor;
        var drawList = ImGui.GetForegroundDrawList();

        DrawArm(drawList, center, new Vector2(-1f, -1f), gap, length, color, thickness);
        DrawArm(drawList, center, new Vector2(1f, -1f), gap, length, color, thickness);
        DrawArm(drawList, center, new Vector2(-1f, 1f), gap, length, color, thickness);
        DrawArm(drawList, center, new Vector2(1f, 1f), gap, length, color, thickness);
    }

    public static void Reset()
    {
        _timeLeft = 0f;
    }

    private static void DrawArm(ImDrawListPtr drawList, Vector2 center, Vector2 direction, float gap, float length, uint color, float thickness)
    {
        var normalized = Vector2.Normalize(direction);
        drawList.AddLine(center + normalized * gap, center + normalized * (gap + length), color, thickness);
    }
}

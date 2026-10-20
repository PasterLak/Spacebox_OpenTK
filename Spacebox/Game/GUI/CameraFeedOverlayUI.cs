using System.Numerics;
using ImGuiNET;
using Spacebox.Game.Rendering;

namespace Spacebox.Game.GUI;

public static class CameraFeedOverlayUI
{
    private const string RecText = "REC";
    private const string CameraText = "CAM 01";
    private const float BlinkPeriod = 1f;

    private static readonly uint White = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.85f));
    private static readonly uint Shadow = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.6f));
    private static readonly uint Red = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.15f, 0.1f, 0.95f));

    private static int _shownSeconds = -1;
    private static string _timer = string.Empty;

    public static void OnGUI()
    {
        if (!CameraFeedController.IsVisible || !Settings.ShowInterface) return;

        var drawList = ImGui.GetBackgroundDrawList();
        var screen = ImGui.GetIO().DisplaySize;
        float margin = MathF.Round(MathF.Min(screen.X, screen.Y) * 0.06f);
        float corner = margin * 0.9f;
        float thickness = MathF.Max(2f, MathF.Round(screen.Y / 400f));

        DrawCorners(drawList, new Vector2(margin), screen - new Vector2(margin), corner, thickness);

        float lineHeight = ImGui.GetTextLineHeight();
        var textOrigin = new Vector2(margin + thickness * 4f, margin + thickness * 3f);

        float seconds = CameraFeedController.ActiveSeconds;
        if (seconds % BlinkPeriod < BlinkPeriod * 0.6f)
            drawList.AddCircleFilled(textOrigin + new Vector2(lineHeight * 0.45f, lineHeight * 0.5f), lineHeight * 0.38f, Red);
        DrawText(drawList, textOrigin + new Vector2(lineHeight * 1.1f, 0f), RecText);

        var cameraSize = ImGui.CalcTextSize(CameraText);
        DrawText(drawList, new Vector2(screen.X - margin - thickness * 4f - cameraSize.X, textOrigin.Y), CameraText);

        UpdateTimer((int)seconds);
        var timerSize = ImGui.CalcTextSize(_timer);
        DrawText(drawList, new Vector2(screen.X - margin - thickness * 4f - timerSize.X, screen.Y - margin - thickness * 3f - lineHeight), _timer);
    }

    private static void UpdateTimer(int seconds)
    {
        if (seconds == _shownSeconds) return;
        _shownSeconds = seconds;
        _timer = $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }

    private static void DrawText(ImDrawListPtr drawList, Vector2 position, string text)
    {
        drawList.AddText(position + Vector2.One, Shadow, text);
        drawList.AddText(position, White, text);
    }

    private static void DrawCorners(ImDrawListPtr drawList, Vector2 min, Vector2 max, float length, float thickness)
    {
        DrawCorner(drawList, new Vector2(min.X, min.Y), new Vector2(1f, 1f), length, thickness);
        DrawCorner(drawList, new Vector2(max.X, min.Y), new Vector2(-1f, 1f), length, thickness);
        DrawCorner(drawList, new Vector2(min.X, max.Y), new Vector2(1f, -1f), length, thickness);
        DrawCorner(drawList, new Vector2(max.X, max.Y), new Vector2(-1f, -1f), length, thickness);
    }

    private static void DrawCorner(ImDrawListPtr drawList, Vector2 point, Vector2 direction, float length, float thickness)
    {
        drawList.AddLine(point, point + new Vector2(direction.X * length, 0f), White, thickness);
        drawList.AddLine(point, point + new Vector2(0f, direction.Y * length), White, thickness);
    }
}

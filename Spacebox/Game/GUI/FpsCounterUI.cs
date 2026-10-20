using System.Numerics;
using Engine;
using ImGuiNET;

namespace Spacebox.Game.GUI;

public static class FpsCounterUI
{
    private const float Margin = 8f;
    private static readonly uint TextColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.9f));
    private static readonly uint ShadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));

    private static int _shownFps = -1;
    private static string _text = string.Empty;

    public static void OnGUI()
    {
        if (!Settings.Graphics.ShowFps) return;

        int fps = Time.FPS;
        if (fps != _shownFps)
        {
            _shownFps = fps;
            _text = fps > 0 ? $"{fps} FPS  {1000f / fps:0.0} ms" : "-- FPS";
        }

        var drawList = ImGui.GetForegroundDrawList();
        var size = ImGui.CalcTextSize(_text);
        var position = new Vector2(ImGui.GetIO().DisplaySize.X - size.X - Margin, Margin);
        drawList.AddText(position + Vector2.One, ShadowColor, _text);
        drawList.AddText(position, TextColor, _text);
    }
}

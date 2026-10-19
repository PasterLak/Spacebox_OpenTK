using System.Numerics;
using ImGuiNET;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public static class ServerSignalBars
{
    public const int BarCount = 4;

    private static readonly int[] PingThresholds = { 300, 150, 80 };

    public static int GetLevel(ServerListItem item)
    {
        if (item.State != ServerOnlineState.Online) return 0;

        int level = 1;
        for (int i = 0; i < PingThresholds.Length; i++)
        {
            if (item.PingMs < PingThresholds[i]) level++;
        }
        return level;
    }

    public static float Width(float height)
    {
        return BarWidth(height) * BarCount + Gap(height) * (BarCount - 1);
    }

    public static void Draw(ImDrawListPtr draw, Vector2 topLeft, float height, ServerListItem item)
    {
        int level = GetLevel(item);
        float barWidth = BarWidth(height);
        float gap = Gap(height);
        uint lit = ImGui.GetColorU32(ServerText.StateColor(item));
        uint dim = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.2f));

        for (int i = 0; i < BarCount; i++)
        {
            float barHeight = height * (i + 1) / BarCount;
            var min = new Vector2(topLeft.X + i * (barWidth + gap), topLeft.Y + height - barHeight);
            var max = new Vector2(min.X + barWidth, topLeft.Y + height);
            draw.AddRectFilled(min, max, i < level ? lit : dim);
        }
    }

    private static float BarWidth(float height) => MathF.Max(2f, height * 0.2f);

    private static float Gap(float height) => MathF.Max(1f, height * 0.08f);
}

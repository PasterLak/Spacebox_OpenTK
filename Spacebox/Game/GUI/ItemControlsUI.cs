using ImGuiNET;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public static class ItemControlsUI
    {
        public static bool IsVisible { get; set; } = true;
        public static string Text { get; set; } = "";
        public static Vector4 TextColor { get; set; } = new Vector4(1f, 1f, 1f, 0.5f);

        public static void OnGUI()
        {
            if (!IsVisible) return;
            if (!Settings.ShowInterface) return;
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;
            string displayText = Text ?? "";
            var textSize = ImGui.CalcTextSize(displayText);
            var padding = new Vector2(10, 10);
            var windowSize = textSize + padding * 2;
            var windowPos = new Vector2(10, displaySize.Y - windowSize.Y - 10);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
            bool visible = IsVisible;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0));
            ImGui.Begin("SimpleTextDrawer", ref visible, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings);
            ImGui.TextColored(TextColor, displayText);
            ImGui.End();
            ImGui.PopStyleColor();
            IsVisible = visible;
        }
    }
}

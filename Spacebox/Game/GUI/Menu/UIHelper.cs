using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public static class UIHelper
    {
        public static void ShowTooltip(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 32f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }
    }
}

using ImGuiNET;


namespace Spacebox.Game.GUI.Menu
{
    public static class UIHelper
    {
        public static void ShowTooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(text);
                ImGui.EndTooltip();
            }
        }
    }
}

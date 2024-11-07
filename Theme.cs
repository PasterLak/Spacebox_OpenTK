using ImGuiNET;
using System.Numerics;


namespace Spacebox
{
    public static class Theme
    {
        public static void ApplyDarkTheme()
        {
            var style = ImGui.GetStyle();

            // Customize style parameters
            style.WindowPadding = new Vector2(20, 20);
            style.WindowRounding = 12f;
            style.FramePadding = new Vector2(0, 0);
            style.FrameRounding = 0f;
            style.ItemSpacing = new Vector2(12, 8);
            style.ItemInnerSpacing = new Vector2(8, 6);
            style.IndentSpacing = 25.0f;
            style.ScrollbarSize = 15.0f;
            style.ScrollbarRounding = 9.0f;
            style.GrabMinSize = 10.0f;
            style.GrabRounding = 3.0f;
            style.WindowTitleAlign = new Vector2(0.5f,0);

            style.CellPadding = new Vector2(0,0);

            // Customize colors
            var colors = ImGui.GetStyle().Colors;
            colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1f, 0.105f, 0.11f, 1.0f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.15f, 0.1505f, 0.151f, 1.0f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.1f, 0.105f, 0.11f, 1.0f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.2f, 0.205f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.2f, 0.205f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.3f, 0.305f, 0.31f, 1.0f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.4f, 0.405f, 0.41f, 1.0f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.1f, 0.105f, 0.11f, 1.0f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.15f, 0.1505f, 0.151f, 1.0f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.1f, 0.105f, 0.11f, 1.0f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.15f, 0.1505f, 0.151f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 0.53f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.41f, 0.41f, 0.41f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.51f, 0.51f, 0.51f, 1.0f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.28f, 0.63f, 0.28f, 1.0f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.28f, 0.63f, 0.28f, 1.0f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.38f, 0.73f, 0.38f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.28f, 0.63f, 0.28f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.38f, 0.73f, 0.38f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.48f, 0.83f, 0.48f, 1.0f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.28f, 0.63f, 0.28f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.38f, 0.73f, 0.38f, 1.0f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.48f, 0.83f, 0.48f, 1.0f);
            // Add more color customizations as needed
        }
    }
}

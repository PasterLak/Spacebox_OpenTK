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


        public static void ApplySpaceboxTheme()
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
            style.WindowTitleAlign = new Vector2(0.5f, 0);

            style.CellPadding = new Vector2(0, 0);



            var colors = style.Colors;

            colors[(int)ImGuiCol.Text] = new Vector4(0.90f, 0.90f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.78f, 0.78f, 0.78f, 1f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.54f, 0.54f, 0.54f, 1.0f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.59f, 0.59f, 0.59f, 0.92f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.50f, 0.50f, 0.50f, 0.50f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.43f, 0.43f, 0.43f, 0.39f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.86f, 0.72f, 0.13f, 0.40f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.86f, 0.70f, 0.10f, 0.69f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.78f, 0.78f, 0.78f, 1f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.78f, 0.78f, 0.78f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.40f, 0.40f, 0.80f, 0.20f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.40f, 0.40f, 0.55f, 0.80f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.20f, 0.25f, 0.30f, 0.60f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.60f, 0.60f, 0.60f, 0.30f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.54f, 0.54f, 0.54f, 0.40f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.50f, 0.50f, 0.50f, 0.60f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.90f, 0.90f, 0.90f, 0.50f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(1.00f, 1.00f, 1.00f, 0.30f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.84f, 0.70f, 0.10f, 0.60f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.75f, 0.75f, 0.75f, 1f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.6f, 0.6f, 0.6f, 1f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.89f, 0.90f, 0.40f, 0.45f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.90f, 0.86f, 0.45f, 0.80f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.86f, 0.84f, 0.15f, 0.80f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0.50f, 0.50f, 0.50f, 0.60f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.60f, 0.60f, 0.70f, 1.00f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.70f, 0.70f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(1.00f, 1.00f, 1.00f, 0.10f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.78f, 0.82f, 1.00f, 0.60f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.78f, 0.82f, 1.00f, 0.90f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.90f, 0.87f, 0.45f, 0.80f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.34f, 0.34f, 0.68f, 0.79f);
            colors[(int)ImGuiCol.TabSelected] = new Vector4(0.40f, 0.40f, 0.73f, 0.84f);
            colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.28f, 0.28f, 0.57f, 0.82f);
            colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(0.35f, 0.35f, 0.65f, 0.84f);
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.40f, 0.40f, 0.90f, 0.31f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.45f, 0.45f, 0.90f, 0.80f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.20f, 0.20f, 0.20f, 0.35f);
            colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.5f,0.5f,0.5f,1f);
        }

    }
}

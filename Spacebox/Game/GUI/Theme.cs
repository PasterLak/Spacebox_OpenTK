using ImGuiNET;
using System;
using System.Numerics;


namespace Spacebox.Game.GUI
{
    public static class Theme
    {

        public static class Colors
        {
            public static readonly Vector4 Background = new Vector4(0.75f, 0.75f, 0.75f, 1f);
            public static readonly Vector4 Deep = new Vector4(0.65f, 0.65f, 0.65f, 1f);
            public static readonly Vector4 Deep2 = new Vector4(0.45f, 0.45f, 0.45f, 1f);
            public static readonly Vector4 BorderLight = new Vector4(0.9f, 0.9f, 0.9f, 1f);
            public static readonly Vector4 BorderDark = new Vector4(0.5f, 0.5f, 0.5f, 1f);

            public static uint BackgroundUint => ImGui.GetColorU32(Background);
            public static uint DeepUint => ImGui.GetColorU32(Deep);
            public static uint Deep2Uint => ImGui.GetColorU32(Deep2);
            public static uint BorderLightUint => ImGui.GetColorU32(BorderLight);
            public static uint BorderDarkUint => ImGui.GetColorU32(BorderDark);



        }

        private static bool activated = false;
        public static void ApplySpaceboxTheme()
        {
            if (activated) return;

            activated = true;
            var style = ImGui.GetStyle();

            // Customize style parameters
            style.WindowPadding = new Vector2(0, 0);
            style.WindowRounding = 0f;
            style.WindowBorderSize = 0f;
            style.FramePadding = new Vector2(0, 0);
            style.FrameBorderSize = 0f;
            style.FrameRounding = 0f;
            style.ItemSpacing = new Vector2(12, 8);
            style.ItemInnerSpacing = new Vector2(8, 6);
            style.IndentSpacing = 25.0f;
            style.ScrollbarSize = 10.0f;
            style.ScrollbarRounding = 0f;
            style.GrabMinSize = 10.0f;
            style.GrabRounding = 3.0f;
            style.WindowTitleAlign = new Vector2(0.5f, 0);

            style.CellPadding = new Vector2(0, 0);



            var colors = style.Colors;

            colors[(int)ImGuiCol.Text] = new Vector4(0.90f, 0.90f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = Colors.Background;
            colors[(int)ImGuiCol.ChildBg] = Colors.Background;
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
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.2f, 0.2f, 0.2f, 0.60f);
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
            colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(0.65f, 0.65f, 0.65f, 0f);
        }
    }
}

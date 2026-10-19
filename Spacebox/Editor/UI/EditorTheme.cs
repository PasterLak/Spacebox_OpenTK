using System.Numerics;
using ImGuiNET;

namespace Spacebox.Editor.UI;

public static class EditorTheme
{
    private static readonly Vector4 Accent = new Vector4(1f, 0.62f, 0.1f, 1f);
    private static readonly Vector4 AccentDim = new Vector4(0.75f, 0.45f, 0.08f, 1f);

    public static void Apply()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        float scale = EditorWidgets.Scale;

        style.WindowRounding = 0f;
        style.ChildRounding = 3f;
        style.FrameRounding = 3f;
        style.GrabRounding = 3f;
        style.PopupRounding = 3f;
        style.TabRounding = 3f;
        style.WindowPadding = new Vector2(8f, 8f) * scale;
        style.FramePadding = new Vector2(6f, 3f) * scale;
        style.ItemSpacing = new Vector2(6f, 4f) * scale;
        style.ItemInnerSpacing = new Vector2(4f, 4f) * scale;
        style.IndentSpacing = 14f * scale;
        style.ScrollbarSize = 14f * scale;
        style.GrabMinSize = 10f * scale;

        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.09f, 0.09f, 0.11f, 1f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.11f, 0.11f, 0.13f, 1f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.12f, 0.12f, 0.15f, 0.98f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.25f, 0.25f, 0.3f, 1f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.18f, 0.18f, 0.22f, 1f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.25f, 0.25f, 0.3f, 1f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.3f, 0.3f, 0.36f, 1f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.22f, 0.22f, 0.27f, 1f);
        colors[(int)ImGuiCol.ButtonHovered] = AccentDim;
        colors[(int)ImGuiCol.ButtonActive] = Accent;
        colors[(int)ImGuiCol.Header] = new Vector4(0.45f, 0.28f, 0.06f, 1f);
        colors[(int)ImGuiCol.HeaderHovered] = AccentDim;
        colors[(int)ImGuiCol.HeaderActive] = Accent;
        colors[(int)ImGuiCol.Tab] = new Vector4(0.18f, 0.18f, 0.22f, 1f);
        colors[(int)ImGuiCol.TabHovered] = AccentDim;
        colors[(int)ImGuiCol.TabSelected] = new Vector4(0.55f, 0.34f, 0.06f, 1f);
        colors[(int)ImGuiCol.CheckMark] = Accent;
        colors[(int)ImGuiCol.SliderGrab] = AccentDim;
        colors[(int)ImGuiCol.SliderGrabActive] = Accent;
        colors[(int)ImGuiCol.TableRowBg] = new Vector4(0f, 0f, 0f, 0f);
        colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1f, 1f, 1f, 0.03f);
        colors[(int)ImGuiCol.Text] = new Vector4(0.92f, 0.92f, 0.94f, 1f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.55f, 0.55f, 0.6f, 1f);
    }
}

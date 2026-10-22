using System.Numerics;
using ImGuiNET;

namespace Spacebox.Editor.Preview;

public static class PreviewViewportImGui
{
    private const float ClickDragTolerance = 4f;

    public static void Draw(this PreviewViewport viewport, Vector2 size)
    {
        size = Vector2.Max(size, new Vector2(32f, 32f));
        viewport.Present((int)size.X, (int)size.Y);

        var start = ImGui.GetCursorScreenPos();
        ImGui.Image(viewport.TextureHandle, size, new Vector2(0, 1), new Vector2(1, 0));
        ImGui.SetCursorScreenPos(start);
        ImGui.InvisibleButton("##previewInput", size);

        bool hovered = ImGui.IsItemHovered();
        var clickTarget = viewport.ClickTarget;
        if (clickTarget != null && hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
            ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0f).Length() < ClickDragTolerance)
        {
            var point = (ImGui.GetMousePos() - start) / size;
            viewport.ClickAt(point.X, point.Y);
        }
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 0f))
        {
            var delta = ImGui.GetIO().MouseDelta;
            viewport.Orbit(delta.X, delta.Y);
        }

        if (hovered)
        {
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0f) viewport.Zoom(wheel);
            if (clickTarget == null && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) viewport.ResetView();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) viewport.ToggleAutoRotate();
        }

        ImGui.GetWindowDrawList().AddText(start + new Vector2(6f, 4f), 0xB0FFFFFF, viewport.Hint);
    }
}

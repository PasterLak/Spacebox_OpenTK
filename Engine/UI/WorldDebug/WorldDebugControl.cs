using Engine.CanvasUI;
using Engine.GUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class WorldDebugControl : ScreenOverlay
{
    private const float AxesLengthRatio = 0.04f;
    private const float LabelOffset = 1.2f;

    public WorldDebugControl()
    {
        Name = "WorldDebug";
        MouseFilter = MouseFilter.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        var camera = Camera.Main;
        var font = GetThemeFont();
        if (camera == null || Root == null) return;

        float fontSize = GetThemeFontSize();
        var projection = new WorldToCanvas(camera, Root, GlobalPosition);
        DrawIcons(renderer, font, fontSize, projection);
        WorldTextDebug.Draw(renderer, font, fontSize, projection);
        AxesGizmo.Draw(renderer, font, fontSize, camera, Size * 0.5f, Size.Y * AxesLengthRatio);
    }

    private static void DrawIcons(ICanvasRenderer renderer, CanvasFont font, float fontSize, WorldToCanvas projection)
    {
        var entries = GizmoIcons.Entries;
        var cameraPosition = projection.Camera.Position;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!projection.TryProject(entry.World, out var center)) continue;

            float size = GizmoIcons.SizeAt((entry.World - cameraPosition).Length);
            GizmoIconPainter.Draw(renderer, center, size, entry.Icon, entry.Color);
            if (font != null && !string.IsNullOrEmpty(entry.Label))
                CanvasDraw.Text(renderer, font, new Vector2(center.X + size * LabelOffset, center.Y - size * 0.5f), entry.Label, fontSize, entry.Color);
        }
    }
}

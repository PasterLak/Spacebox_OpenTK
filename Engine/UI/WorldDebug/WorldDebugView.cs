using Engine.CanvasUI;
using Engine.Graphics.Debugging;
using Engine.GUI;

namespace Engine.UI;

public static class WorldDebugView
{
    private static DebugCanvas _canvas;
    private static WorldDebugControl _world;
    private static RenderModeLabel _renderMode;
    private static bool _showWorld;
    private static bool _showRenderMode;

    public static void DrawWorld()
    {
        _showWorld = VisualDebug.Enabled && Camera.Main != null;
        _showRenderMode = RenderDebugView.Mode != RenderDebugMode.Lit;
        if (Application.IsHeadless || (!_showWorld && !_showRenderMode))
        {
            GizmoIcons.Clear();
            return;
        }

        EnsureCanvas();
        _world.Visible = _showWorld;
        _renderMode.Show(RenderDebugView.Mode);
        _canvas.Layer.Update(Time.Delta);
        _renderMode.Visible = false;
        if (_showWorld) _canvas.Layer.Render();
        GizmoIcons.Clear();
    }

    public static void DrawLabels()
    {
        if (_canvas == null || !_showRenderMode || Application.IsHeadless) return;
        _world.Visible = false;
        _renderMode.Visible = true;
        _canvas.Layer.Render();
        _showRenderMode = false;
    }

    public static void Shutdown()
    {
        _canvas?.Dispose();
        _canvas = null;
    }

    private static void EnsureCanvas()
    {
        if (_canvas != null) return;
        _canvas = new DebugCanvas("World debug") { Layer = { InputEnabled = false } };
        _world = _canvas.Root.Add(new WorldDebugControl(), UiLayer.Background);
        _renderMode = DebugCanvas.Area(_canvas.Root, UiLayer.Debug).AddChild(new RenderModeLabel());
    }
}

using Engine.Light;
using Engine.Graphics.Rhi;

namespace Spacebox.Game.ShipViewer;

public static class ShipViewerBackdrop
{
    public static void Draw()
    {
        var skybox = Lighting.Skybox;
        if (skybox == null) return;

        skybox.Render();
        Gpu.Device.Clear(ClearTargets.Depth);
    }
}

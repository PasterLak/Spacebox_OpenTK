using Engine.Light;
using OpenTK.Graphics.OpenGL4;

namespace Spacebox.Game.ShipViewer;

public static class ShipViewerBackdrop
{
    public static void Draw()
    {
        var skybox = Lighting.Skybox;
        if (skybox == null) return;

        skybox.Render();
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }
}

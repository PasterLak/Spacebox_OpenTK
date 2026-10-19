using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class ShipViewCamera : Camera360Base
{
    public ShipViewCamera() : base(Vector3.Zero, false)
    {
        Name = "ShipViewCamera";
    }

    public void Follow(Camera source, Vector3 position)
    {
        Position = position;
        FOV = source.FOV;
        AspectRatio = source.AspectRatio;
        DepthNear = source.DepthNear;
        DepthFar = source.DepthFar;
        SetRenderSpace(source.CameraRelativeRender);
        Update();
    }
}

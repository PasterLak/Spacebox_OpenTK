using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class ShipViewCamera : Camera360Base
{
    public const float FovMultiplier = 1.2f;
    public const float MaxFov = 150f;

    public ShipViewCamera() : base(Vector3.Zero, false)
    {
        Name = "ShipViewCamera";
    }

    public void Follow(Camera source, Vector3 position)
    {
        Position = position;
        FOV = GetViewFov(source.FOV);
        AspectRatio = source.AspectRatio;
        DepthNear = source.DepthNear;
        DepthFar = source.DepthFar;
        SetRenderSpace(source.CameraRelativeRender);
        Update();
    }

    public static float GetViewFov(float playerFov)
    {
        return MathF.Min(playerFov * FovMultiplier, MaxFov);
    }
}

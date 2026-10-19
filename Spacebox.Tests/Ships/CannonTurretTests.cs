using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Tests.Ships;

public class CannonTurretTests
{
    private static BlockData CreateCannon(byte x, byte y, byte z)
    {
        return new BlockData("cannon", "cannon")
        {
            Size = new Vector3Byte(x, y, z),
            BaseFrontDirection = Direction.Up
        };
    }

    [Fact]
    public void Pivot_SingleCell_IsBottomFaceCenter()
    {
        Assert.Equal(new Vector3(0f, -0.5f, 0f), CannonTurretRenderer.GetPivotModel(CreateCannon(1, 1, 1)));
    }

    [Fact]
    public void Pivot_LargeCannon_IsBaseCenter()
    {
        Assert.Equal(new Vector3(0.5f, -0.5f, 0.5f), CannonTurretRenderer.GetPivotModel(CreateCannon(2, 4, 2)));
    }

    [Fact]
    public void Pivot_FollowsFrontDirection()
    {
        var data = CreateCannon(1, 1, 3);
        data.BaseFrontDirection = Direction.Forward;

        Assert.Equal(new Vector3(0f, 0f, -0.5f), CannonTurretRenderer.GetPivotModel(data));
    }

    [Fact]
    public void AimRotation_MapsRestToAim()
    {
        var aim = new Vector3(1f, 1f, 0f).Normalized();
        var rotation = CannonTurretRenderer.GetAimRotation(Vector3.UnitY, aim);

        var result = Vector3.TransformVector(Vector3.UnitY, rotation);

        Assert.True((result - aim).Length < 1e-5f);
    }

    [Fact]
    public void AimRotation_AtRest_IsIdentity()
    {
        Assert.Equal(Matrix4.Identity, CannonTurretRenderer.GetAimRotation(Vector3.UnitY, Vector3.UnitY));
    }
}
